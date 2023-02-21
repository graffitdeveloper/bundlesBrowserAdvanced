using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using AssetBundleBrowser.AssetBundleDataSource;

namespace AssetBundleBrowser {
	[System.Serializable]
	internal class AssetBundleAdvancedBuildTab {
		const string k_BuildPrefPrefix = "ABBBuild:";

		private string m_streamingPath = "Assets/StreamingAssets";

		[SerializeField] private bool m_AdvancedSettings;

		[SerializeField] private Vector2 m_ScrollPosition;


		class ToggleData {
			internal ToggleData(bool s,
				string title,
				string tooltip,
				List<string> onToggles,
				BuildAssetBundleOptions opt = BuildAssetBundleOptions.None) {
				if (onToggles.Contains(title))
					state = true;
				else
					state = s;
				content = new GUIContent(title, tooltip);
				option = opt;
			}

			//internal string prefsKey
			//{ get { return k_BuildPrefPrefix + content.text; } }
			internal bool                    state;
			internal GUIContent              content;
			internal BuildAssetBundleOptions option;
		}

		private AssetBundleInspectTab m_InspectTab;

		[SerializeField] private BuildTabData m_UserData;

		List<ToggleData>                                 m_ToggleData;
		ToggleData                                       m_ForceRebuild;
		ToggleData                                       m_AskConfirmation;
		ToggleData                                       m_CopyToStreaming;
		private Dictionary<ValidBuildTarget, ToggleData> m_EnabledBuildTargetsToggleDatas;
		private Dictionary<ValidBuildTarget, ToggleData> m_RebuildBuildTargetsToggleDatas;
		GUIContent                                       m_TargetContent;
		GUIContent                                       m_CompressionContent;

		internal enum CompressOptions {
			Uncompressed = 0,
			StandardCompression,
			ChunkBasedCompression,
		}

		GUIContent[] m_CompressionOptions = {
			new GUIContent("No Compression"),
			new GUIContent("Standard Compression (LZMA)"),
			new GUIContent("Chunk Based Compression (LZ4)")
		};

		int[]        m_CompressionValues = { 0, 1, 2 };
		private bool m_EnabledBuildTargets;


		internal AssetBundleAdvancedBuildTab() {
			m_AdvancedSettings = false;
			m_UserData = new BuildTabData();
			m_UserData.m_OnToggles = new List<string>();
			m_UserData.m_UseDefaultPath = true;
		}

		internal void OnDisable() {
			var dataPath = System.IO.Path.GetFullPath(".");
			dataPath = dataPath.Replace("\\", "/");
			dataPath += "/Library/AssetBundleBrowserBuildAdvanced.dat";

			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Create(dataPath);

			bf.Serialize(file, m_UserData);
			file.Close();
		}

		internal void OnEnable(EditorWindow parent) {
			m_InspectTab = (parent as AssetBundleBrowserMain).m_InspectTab;

			//LoadData...
			var dataPath = System.IO.Path.GetFullPath(".");
			dataPath = dataPath.Replace("\\", "/");
			dataPath += "/Library/AssetBundleBrowserBuildAdvanced.dat";

			if (File.Exists(dataPath)) {
				BinaryFormatter bf = new BinaryFormatter();
				FileStream file = File.Open(dataPath, FileMode.Open);
				var data = bf.Deserialize(file) as BuildTabData;
				if (data != null)
					m_UserData = data;
				file.Close();
			}

			m_ToggleData = new List<ToggleData>();
			m_ToggleData.Add(new ToggleData(
				false,
				"Exclude Type Information",
				"Do not include type information within the asset bundle (don't write type tree).",
				m_UserData.m_OnToggles,
				BuildAssetBundleOptions.DisableWriteTypeTree));
			m_ToggleData.Add(new ToggleData(
				false,
				"Force Rebuild",
				"Force rebuild the asset bundles",
				m_UserData.m_OnToggles,
				BuildAssetBundleOptions.ForceRebuildAssetBundle));
			m_ToggleData.Add(new ToggleData(
				false,
				"Ignore Type Tree Changes",
				"Ignore the type tree changes when doing the incremental build check.",
				m_UserData.m_OnToggles,
				BuildAssetBundleOptions.IgnoreTypeTreeChanges));
			m_ToggleData.Add(new ToggleData(
				false,
				"Append Hash",
				"Append the hash to the assetBundle name.",
				m_UserData.m_OnToggles,
				BuildAssetBundleOptions.AppendHashToAssetBundleName));
			m_ToggleData.Add(new ToggleData(
				false,
				"Strict Mode",
				"Do not allow the build to succeed if any errors are reporting during it.",
				m_UserData.m_OnToggles,
				BuildAssetBundleOptions.StrictMode));
			m_ToggleData.Add(new ToggleData(
				false,
				"Dry Run Build",
				"Do a dry run build.",
				m_UserData.m_OnToggles,
				BuildAssetBundleOptions.DryRunBuild));


			m_ForceRebuild = new ToggleData(
				false,
				"Clear Folders",
				"Will wipe out all contents of build directory as well as StreamingAssets/AssetBundles if you are choosing to copy build there.",
				m_UserData.m_OnToggles);

			m_AskConfirmation = new ToggleData(
				false,
				"Ask Clear Folders confirmation",
				"Is Clear folders confirmation window required?",
				m_UserData.m_OnToggles);

			m_CopyToStreaming = new ToggleData(
				false,
				"Copy to StreamingAssets",
				"After build completes, will copy all build content to " + m_streamingPath + " for use in stand-alone player.",
				m_UserData.m_OnToggles);

			m_EnabledBuildTargetsToggleDatas = new Dictionary<ValidBuildTarget, ToggleData>();
			var values = Enum.GetValues(typeof(ValidBuildTarget));

			foreach (ValidBuildTarget value in values) {
				m_EnabledBuildTargetsToggleDatas.Add(value, new ToggleData(
					false,
					value.ToString(),
					$"Toggle {value} support",
					m_UserData.m_OnToggles));
			}

			m_RebuildBuildTargetsToggleDatas = new Dictionary<ValidBuildTarget, ToggleData>();

			foreach (ValidBuildTarget value in values) {
				m_RebuildBuildTargetsToggleDatas.Add(value, new ToggleData(
					false,
					$"Rebuild {value.ToString()}",
					$"Rebuild {value} bundles",
					m_UserData.m_OnToggles));
			}

			m_TargetContent = new GUIContent("Build Target", "Choose build target to build for.");
			m_CompressionContent = new GUIContent("Compression", "Choose no compress, standard (LZMA), or chunk based (LZ4)");

			if (m_UserData.m_UseDefaultPath) {
				ResetPathToDefault();
			}
		}

		internal void OnGUI() {
			m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
			bool newState = false;
			var centeredStyle = new GUIStyle(GUI.skin.GetStyle("Label"));
			centeredStyle.alignment = TextAnchor.UpperCenter;
			GUILayout.Label(new GUIContent("Build setup"), centeredStyle);
			//basic options
			GUILayout.BeginVertical();

			////output path
			using (new EditorGUI.DisabledScope(!AssetBundleModel.Model.DataSource.CanSpecifyBuildOutputDirectory)) {
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				var newPath = EditorGUILayout.TextField("Output Path", m_UserData.m_OutputPath);
				if (!System.String.IsNullOrEmpty(newPath) && newPath != m_UserData.m_OutputPath) {
					m_UserData.m_UseDefaultPath = false;
					m_UserData.m_OutputPath = newPath;
					//EditorUserBuildSettings.SetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath", m_OutputPath);
				}

				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Browse", GUILayout.MaxWidth(75f)))
					BrowseForFolder();
				if (GUILayout.Button("Reset", GUILayout.MaxWidth(75f)))
					ResetPathToDefault();
				//if (string.IsNullOrEmpty(m_OutputPath))
				//    m_OutputPath = EditorUserBuildSettings.GetPlatformSettings(EditorUserBuildSettings.activeBuildTarget.ToString(), "AssetBundleOutputPath");
				GUILayout.EndHorizontal();

				var options = new GUILayoutOption[1];
				options[0] = GUILayout.Width(300);
				var style = new GUIStyle();
				style.alignment = TextAnchor.MiddleLeft;
				EditorGUILayout.BeginHorizontal(style, options);
				var atLeastOneBuildTargetEnabled = false;
				var values = Enum.GetValues(typeof(ValidBuildTarget));
				foreach (ValidBuildTarget value in values) {
					if (m_EnabledBuildTargetsToggleDatas[value].state) {
						atLeastOneBuildTargetEnabled = true;
						newState = GUILayout.Toggle(
							m_RebuildBuildTargetsToggleDatas[value].state,
							m_RebuildBuildTargetsToggleDatas[value].content);
						if (newState != m_RebuildBuildTargetsToggleDatas[value].state) {
							if (newState) {
								m_UserData.m_OnToggles.Add(m_RebuildBuildTargetsToggleDatas[value].content.text);
							} else {
								m_UserData.m_OnToggles.Remove(m_RebuildBuildTargetsToggleDatas[value].content.text);
							}

							m_RebuildBuildTargetsToggleDatas[value].state = newState;
						}

						EditorGUILayout.Space();
					} else {
						m_RebuildBuildTargetsToggleDatas[value].state = false;
						m_UserData.m_OnToggles.Remove(m_RebuildBuildTargetsToggleDatas[value].content.text);
					}
				}

				if (!atLeastOneBuildTargetEnabled) {
					GUILayout.Label(EditorGUIUtility.FindTexture("console.infoicon.inactive.sml"), GUILayout.MaxWidth(20f));
					GUILayout.Label("All build targets are disabled");
				}

				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				m_EnabledBuildTargets = EditorGUILayout.Foldout(m_EnabledBuildTargets, "Enabled build targets");
				if (m_EnabledBuildTargets) {
					foreach (ValidBuildTarget value in values) {
						newState = GUILayout.Toggle(
							m_EnabledBuildTargetsToggleDatas[value].state,
							m_EnabledBuildTargetsToggleDatas[value].content);
						if (newState != m_EnabledBuildTargetsToggleDatas[value].state) {
							if (newState)
								m_UserData.m_OnToggles.Add(m_EnabledBuildTargetsToggleDatas[value].content.text);
							else
								m_UserData.m_OnToggles.Remove(m_EnabledBuildTargetsToggleDatas[value].content.text);
							m_EnabledBuildTargetsToggleDatas[value].state = newState;
						}
					}
				}

				EditorGUILayout.Space();

				var buildTargets = new List<string>();

				foreach (var data in m_RebuildBuildTargetsToggleDatas) {
					if (data.Value.state) {
						buildTargets.Add(data.Key.ToString());
					}
				}

				var sb = new StringBuilder();
				for (var i = 0; i < buildTargets.Count; i++) {
					sb.Append(buildTargets[i]);
					if (i < buildTargets.Count - 1) {
						sb.Append(", ");
					}
				}

				if (buildTargets.Count == 0) {
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button("Choose at least one build target to rebuild");
					EditorGUI.EndDisabledGroup();
				} else {
					if (GUILayout.Button($"Build {sb} bundles")) {
						EditorApplication.delayCall += () => ExecuteBuildAllBuildTargets();
					}
				}

				EditorGUILayout.Space();

				EditorGUILayout.BeginHorizontal();
				GUILayout.Label(EditorGUIUtility.FindTexture("console.infoicon.inactive.sml"), GUILayout.MaxWidth(20f));
				GUILayout.Label("Use #if !BUNDLES_BUILD preprocessor directive to ignore platform-specific lines while bundles are build");
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				newState = GUILayout.Toggle(
					m_ForceRebuild.state,
					m_ForceRebuild.content);
				if (newState != m_ForceRebuild.state) {
					if (newState)
						m_UserData.m_OnToggles.Add(m_ForceRebuild.content.text);
					else
						m_UserData.m_OnToggles.Remove(m_ForceRebuild.content.text);
					m_ForceRebuild.state = newState;
				}

				newState = GUILayout.Toggle(
					m_AskConfirmation.state,
					m_AskConfirmation.content);
				if (newState != m_AskConfirmation.state) {
					if (newState)
						m_UserData.m_OnToggles.Add(m_AskConfirmation.content.text);
					else
						m_UserData.m_OnToggles.Remove(m_AskConfirmation.content.text);
					m_AskConfirmation.state = newState;
				}
			}

			// advanced options
			using (new EditorGUI.DisabledScope(!AssetBundleModel.Model.DataSource.CanSpecifyBuildOptions)) {
				EditorGUILayout.Space();
				m_AdvancedSettings = EditorGUILayout.Foldout(m_AdvancedSettings, "Advanced Settings");
				if (m_AdvancedSettings) {
					var indent = EditorGUI.indentLevel;
					EditorGUI.indentLevel = 1;

					newState = GUILayout.Toggle(
						m_CopyToStreaming.state,
						m_CopyToStreaming.content);
					if (newState != m_CopyToStreaming.state) {
						if (newState)
							m_UserData.m_OnToggles.Add(m_CopyToStreaming.content.text);
						else
							m_UserData.m_OnToggles.Remove(m_CopyToStreaming.content.text);
						m_CopyToStreaming.state = newState;
					}

					CompressOptions cmp = (CompressOptions) EditorGUILayout.IntPopup(
						m_CompressionContent,
						(int) m_UserData.m_Compression,
						m_CompressionOptions,
						m_CompressionValues);

					if (cmp != m_UserData.m_Compression) {
						m_UserData.m_Compression = cmp;
					}

					foreach (var tog in m_ToggleData) {
						newState = EditorGUILayout.ToggleLeft(
							tog.content,
							tog.state);
						if (newState != tog.state) {
							if (newState)
								m_UserData.m_OnToggles.Add(tog.content.text);
							else
								m_UserData.m_OnToggles.Remove(tog.content.text);
							tog.state = newState;
						}
					}

					EditorGUILayout.Space();
					EditorGUI.indentLevel = indent;
				}
			}

			GUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}

		public void ExecuteBuildAllBuildTargets(List<AssetBundleBuild> assetBundleBuilds = null) {
			var oldOutputPath = m_UserData.m_OutputPath;
			var oldBuildTarget = m_UserData.m_BuildTarget;
			var targets = new List<ValidBuildTarget>();

			// для экономии времени текущую включенную платформу добавляем в список первой, если её нужно ребилдить
			var currentEditorValidTarget = BuildTargetToValidBuildTarget(EditorUserBuildSettings.activeBuildTarget);
			if (m_RebuildBuildTargetsToggleDatas[currentEditorValidTarget].state) {
				targets.Add(currentEditorValidTarget);
			}

			// добавляем остальные отмеченные платформы
			foreach (var data in m_RebuildBuildTargetsToggleDatas) {
				if (data.Value.state && !targets.Exists(target => target == data.Key)) {
					targets.Add(data.Key);
				}
			}

			var sb = new StringBuilder();
			for (var i = 0; i < targets.Count; i++) {
				sb.Append(targets[i].ToString());
				if (i < targets.Count - 1) {
					sb.Append(", ");
				}
			}

			Debug.Log($"<color=#00FFAA>Started bundles rebuild for build targets: {sb}...</color>");

			// билдим все платформы, которые нужно
			for (var i = 0; i < targets.Count; i++) {
				var target = targets[i];

				var targetGroup = ValidBuildTargetToGroup(target);
				var sdSymbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
				PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, "BUNDLES_BUILD");

				Debug.Log($"<color=#00FFAA>Building {target} bundles...</color>");

				m_UserData.m_BuildTarget = target;

				if (oldOutputPath[oldOutputPath.Length - 1] == '/') {
					m_UserData.m_OutputPath = oldOutputPath + $"{target}/";
				} else {
					m_UserData.m_OutputPath = oldOutputPath + $"/{target}/";
				}

				ExecuteBuild(assetBundleBuilds);

				PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, sdSymbols);

				Debug.Log($"<color=#00FFAA>Finished build {target} bundles</color>");
			}

			m_UserData.m_OutputPath = oldOutputPath;
			m_UserData.m_BuildTarget = oldBuildTarget;
			AssetDatabase.SaveAssets();
			Debug.Log("<color=#00FFAA>Rebuild bundles for all selected build targets finished!</color>");
		}

		private void ExecuteBuild(List<AssetBundleBuild> assetBundleBuilds) {
			if (AssetBundleModel.Model.DataSource.CanSpecifyBuildOutputDirectory) {
				if (string.IsNullOrEmpty(m_UserData.m_OutputPath))
					BrowseForFolder();

				if (string.IsNullOrEmpty(m_UserData.m_OutputPath)) //in case they hit "cancel" on the open browser
				{
					Debug.LogError("AssetBundle Build: No valid output path for build.");
					return;
				}

				if (m_ForceRebuild.state) {
					string message = "Do you want to delete all files in the directory " + m_UserData.m_OutputPath;
					if (m_CopyToStreaming.state)
						message += " and " + m_streamingPath;
					message += "?";
					if (!m_AskConfirmation.state ||
					    EditorUtility.DisplayDialog("File delete confirmation", message, "Yes", "No")) {
						try {
							if (Directory.Exists(m_UserData.m_OutputPath))
								Directory.Delete(m_UserData.m_OutputPath, true);

							if (m_CopyToStreaming.state)
								if (Directory.Exists(m_streamingPath))
									Directory.Delete(m_streamingPath, true);
						}
						catch (System.Exception e) {
							Debug.LogException(e);
						}
					}
				}

				if (!Directory.Exists(m_UserData.m_OutputPath))
					Directory.CreateDirectory(m_UserData.m_OutputPath);
			}

			BuildAssetBundleOptions opt = BuildAssetBundleOptions.None;

			if (AssetBundleModel.Model.DataSource.CanSpecifyBuildOptions) {
				if (m_UserData.m_Compression == CompressOptions.Uncompressed)
					opt |= BuildAssetBundleOptions.UncompressedAssetBundle;
				else if (m_UserData.m_Compression == CompressOptions.ChunkBasedCompression)
					opt |= BuildAssetBundleOptions.ChunkBasedCompression;
				foreach (var tog in m_ToggleData) {
					if (tog.state)
						opt |= tog.option;
				}
			}

			ABBuildInfo buildInfo = new ABBuildInfo();

			buildInfo.outputDirectory = m_UserData.m_OutputPath;
			buildInfo.options = opt;
			buildInfo.buildTarget = (BuildTarget) m_UserData.m_BuildTarget;
			buildInfo.onBuild = (assetBundleName) => {
				if (m_InspectTab == null)
					return;
				m_InspectTab.AddBundleFolder(buildInfo.outputDirectory);
				m_InspectTab.RefreshBundles();
			};

			AssetBundleModel.Model.DataSource.BuildAssetBundles(buildInfo, assetBundleBuilds);

			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

			if (m_CopyToStreaming.state)
				DirectoryCopy(m_UserData.m_OutputPath, m_streamingPath);
		}

		private static void DirectoryCopy(string sourceDirName, string destDirName) {
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName)) {
				Directory.CreateDirectory(destDirName);
			}

			foreach (string folderPath in Directory.GetDirectories(sourceDirName, "*", SearchOption.AllDirectories)) {
				if (!Directory.Exists(folderPath.Replace(sourceDirName, destDirName)))
					Directory.CreateDirectory(folderPath.Replace(sourceDirName, destDirName));
			}

			foreach (string filePath in Directory.GetFiles(sourceDirName, "*.*", SearchOption.AllDirectories)) {
				var fileDirName = Path.GetDirectoryName(filePath).Replace("\\", "/");
				var fileName = Path.GetFileName(filePath);
				string newFilePath = Path.Combine(fileDirName.Replace(sourceDirName, destDirName), fileName);

				File.Copy(filePath, newFilePath, true);
			}
		}

		private void BrowseForFolder() {
			m_UserData.m_UseDefaultPath = false;
			var newPath = EditorUtility.OpenFolderPanel("Bundle Folder", m_UserData.m_OutputPath, string.Empty);
			if (!string.IsNullOrEmpty(newPath)) {
				var gamePath = System.IO.Path.GetFullPath(".");
				gamePath = gamePath.Replace("\\", "/");
				if (newPath.StartsWith(gamePath) && newPath.Length > gamePath.Length)
					newPath = newPath.Remove(0, gamePath.Length + 1);
				m_UserData.m_OutputPath = newPath;
			}
		}

		private void ResetPathToDefault() {
			m_UserData.m_UseDefaultPath = true;
			m_UserData.m_OutputPath = "../AssetBundles";
		}

		internal BuildTargetGroup ValidBuildTargetToGroup(ValidBuildTarget target) {
			BuildTargetGroup result;
			switch (target) {
				case ValidBuildTarget.StandaloneOSXUniversal:
				case ValidBuildTarget.StandaloneOSXIntel:
				case ValidBuildTarget.StandaloneWindows:
				case ValidBuildTarget.StandaloneLinux:
				case ValidBuildTarget.StandaloneWindows64:
				case ValidBuildTarget.StandaloneLinux64:
				case ValidBuildTarget.StandaloneLinuxUniversal:
				case ValidBuildTarget.StandaloneOSXIntel64:
					result = BuildTargetGroup.Standalone;
					break;
				case ValidBuildTarget.WebPlayer:
				case ValidBuildTarget.WebPlayerStreamed:
				case ValidBuildTarget.WebGL:
					result = BuildTargetGroup.WebGL;
					break;
				case ValidBuildTarget.iOS:
					result = BuildTargetGroup.iOS;
					break;
				case ValidBuildTarget.PS3:
					result = BuildTargetGroup.PS3;
					break;
				case ValidBuildTarget.XBOX360:
					result = BuildTargetGroup.XBOX360;
					break;
				case ValidBuildTarget.Android:
					result = BuildTargetGroup.Android;
					break;
				case ValidBuildTarget.BlackBerry:
					result = BuildTargetGroup.BlackBerry;
					break;
				case ValidBuildTarget.WSAPlayer:
					result = BuildTargetGroup.WSA;
					break;
				case ValidBuildTarget.WP8Player:
					result = BuildTargetGroup.WP8;
					break;
				case ValidBuildTarget.Tizen:
					result = BuildTargetGroup.Tizen;
					break;
				case ValidBuildTarget.PSP2:
					result = BuildTargetGroup.PSP2;
					break;
				case ValidBuildTarget.PS4:
					result = BuildTargetGroup.PS4;
					break;
				case ValidBuildTarget.PSM:
					result = BuildTargetGroup.PSM;
					break;
				case ValidBuildTarget.XboxOne:
					result = BuildTargetGroup.XboxOne;
					break;
				case ValidBuildTarget.SamsungTV:
					result = BuildTargetGroup.SamsungTV;
					break;
				case ValidBuildTarget.N3DS:
					result = BuildTargetGroup.N3DS;
					break;
				case ValidBuildTarget.WiiU:
					result = BuildTargetGroup.WiiU;
					break;
				case ValidBuildTarget.tvOS:
					result = BuildTargetGroup.tvOS;
					break;
				case ValidBuildTarget.Switch:
					result = BuildTargetGroup.Switch;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(target), target, null);
			}

			return result;
		}

		internal ValidBuildTarget BuildTargetToValidBuildTarget(BuildTarget target) {
			ValidBuildTarget result;
			switch (target) {
				case BuildTarget.StandaloneOSX:
					result = ValidBuildTarget.StandaloneOSXUniversal;
					break;
				case BuildTarget.StandaloneOSXIntel:
					result = ValidBuildTarget.StandaloneOSXIntel;
					break;
				case BuildTarget.StandaloneWindows:
					result = ValidBuildTarget.StandaloneWindows;
					break;
				case BuildTarget.iOS:
					result = ValidBuildTarget.iOS;
					break;
				case BuildTarget.PS3:
					result = ValidBuildTarget.PS3;
					break;
				case BuildTarget.XBOX360:
					result = ValidBuildTarget.XBOX360;
					break;
				case BuildTarget.Android:
					result = ValidBuildTarget.Android;
					break;
				case BuildTarget.StandaloneLinux:
					result = ValidBuildTarget.StandaloneLinux;
					break;
				case BuildTarget.StandaloneWindows64:
					result = ValidBuildTarget.StandaloneWindows64;
					break;
				case BuildTarget.WebGL:
					result = ValidBuildTarget.WebGL;
					break;
				case BuildTarget.WSAPlayer:
					result = ValidBuildTarget.WSAPlayer;
					break;
				case BuildTarget.StandaloneLinux64:
					result = ValidBuildTarget.StandaloneLinux64;
					break;
				case BuildTarget.StandaloneLinuxUniversal:
					result = ValidBuildTarget.StandaloneLinuxUniversal;
					break;
				case BuildTarget.WP8Player:
					result = ValidBuildTarget.WP8Player;
					break;
				case BuildTarget.StandaloneOSXIntel64:
					result = ValidBuildTarget.StandaloneOSXIntel64;
					break;
				case BuildTarget.BlackBerry:
					result = ValidBuildTarget.BlackBerry;
					break;
				case BuildTarget.Tizen:
					result = ValidBuildTarget.Tizen;
					break;
				case BuildTarget.PSP2:
					result = ValidBuildTarget.PSP2;
					break;
				case BuildTarget.PS4:
					result = ValidBuildTarget.PS4;
					break;
				case BuildTarget.PSM:
					result = ValidBuildTarget.PSM;
					break;
				case BuildTarget.XboxOne:
					result = ValidBuildTarget.XboxOne;
					break;
				case BuildTarget.SamsungTV:
					result = ValidBuildTarget.SamsungTV;
					break;
				case BuildTarget.N3DS:
					result = ValidBuildTarget.N3DS;
					break;
				case BuildTarget.WiiU:
					result = ValidBuildTarget.WiiU;
					break;
				case BuildTarget.tvOS:
					result = ValidBuildTarget.tvOS;
					break;
				case BuildTarget.Switch:
					result = ValidBuildTarget.Switch;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(target), target, null);
			}

			return result;
		}

		//Note: this is the provided BuildTarget enum with some entries removed as they are invalid in the dropdown
		internal enum ValidBuildTarget {
			//NoTarget = -2,        --doesn't make sense
			//iPhone = -1,          --deprecated
			//BB10 = -1,            --deprecated
			//MetroPlayer = -1,     --deprecated
			StandaloneOSXUniversal   = 2,
			StandaloneOSXIntel       = 4,
			StandaloneWindows        = 5,
			WebPlayer                = 6,
			WebPlayerStreamed        = 7,
			iOS                      = 9,
			PS3                      = 10,
			XBOX360                  = 11,
			Android                  = 13,
			StandaloneLinux          = 17,
			StandaloneWindows64      = 19,
			WebGL                    = 20,
			WSAPlayer                = 21,
			StandaloneLinux64        = 24,
			StandaloneLinuxUniversal = 25,
			WP8Player                = 26,
			StandaloneOSXIntel64     = 27,
			BlackBerry               = 28,
			Tizen                    = 29,
			PSP2                     = 30,
			PS4                      = 31,
			PSM                      = 32,
			XboxOne                  = 33,
			SamsungTV                = 34,
			N3DS                     = 35,
			WiiU                     = 36,
			tvOS                     = 37,
			Switch                   = 38
		}

		[System.Serializable]
		internal class BuildTabData {
			internal List<string>     m_OnToggles;
			internal ValidBuildTarget m_BuildTarget    = ValidBuildTarget.StandaloneWindows;
			internal CompressOptions  m_Compression    = CompressOptions.StandardCompression;
			internal string           m_OutputPath     = string.Empty;
			internal bool             m_UseDefaultPath = true;
		}
	}
}