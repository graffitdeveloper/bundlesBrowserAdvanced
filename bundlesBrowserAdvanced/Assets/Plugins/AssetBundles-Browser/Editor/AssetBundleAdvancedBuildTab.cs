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

		List<ToggleData> m_ToggleData;
		ToggleData       m_ForceRebuild;
		ToggleData       m_AskConfirmation;
		ToggleData       m_CopyToStreaming;
		GUIContent       m_TargetContent;
		GUIContent       m_CompressionContent;

		ToggleData m_rebuildWebGL;
		ToggleData m_rebuildAndroid;
		ToggleData m_rebuildiOS;
		ToggleData m_rebuildStandaloneWindows;

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

		int[] m_CompressionValues = {0, 1, 2};


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


			m_rebuildWebGL = new ToggleData(
				false,
				"Rebuild WebGL",
				"Rebuild WebGL bundles",
				m_UserData.m_OnToggles);

			m_rebuildAndroid = new ToggleData(
				false,
				"Rebuild Android",
				"Rebuild Android bundles",
				m_UserData.m_OnToggles);

			m_rebuildiOS = new ToggleData(
				false,
				"Rebuild iOS",
				"Rebuild iOS bundles",
				m_UserData.m_OnToggles);

			m_rebuildStandaloneWindows = new ToggleData(
				false,
				"Rebuild StandaloneWindows",
				"Rebuild StandaloneWindows bundles",
				m_UserData.m_OnToggles);

			m_TargetContent = new GUIContent("Build Target", "Choose target platform to build for.");
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
				EditorGUILayout.BeginHorizontal(options);

				EditorGUILayout.Space();

				newState = GUILayout.Toggle(
					m_rebuildWebGL.state,
					m_rebuildWebGL.content);
				if (newState != m_rebuildWebGL.state) {
					if (newState)
						m_UserData.m_OnToggles.Add(m_rebuildWebGL.content.text);
					else
						m_UserData.m_OnToggles.Remove(m_rebuildWebGL.content.text);
					m_rebuildWebGL.state = newState;
				}

				EditorGUILayout.Space();

				newState = GUILayout.Toggle(
					m_rebuildAndroid.state,
					m_rebuildAndroid.content);
				if (newState != m_rebuildAndroid.state) {
					if (newState)
						m_UserData.m_OnToggles.Add(m_rebuildAndroid.content.text);
					else
						m_UserData.m_OnToggles.Remove(m_rebuildAndroid.content.text);
					m_rebuildAndroid.state = newState;
				}

				EditorGUILayout.Space();

				newState = GUILayout.Toggle(
					m_rebuildiOS.state,
					m_rebuildiOS.content);
				if (newState != m_rebuildiOS.state) {
					if (newState)
						m_UserData.m_OnToggles.Add(m_rebuildiOS.content.text);
					else
						m_UserData.m_OnToggles.Remove(m_rebuildiOS.content.text);
					m_rebuildiOS.state = newState;
				}

				EditorGUILayout.Space();

				newState = GUILayout.Toggle(
					m_rebuildStandaloneWindows.state,
					m_rebuildStandaloneWindows.content);
				if (newState != m_rebuildStandaloneWindows.state) {
					if (newState)
						m_UserData.m_OnToggles.Add(m_rebuildStandaloneWindows.content.text);
					else
						m_UserData.m_OnToggles.Remove(m_rebuildStandaloneWindows.content.text);
					m_rebuildStandaloneWindows.state = newState;
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				var platforms = new List<string>();
				if (m_rebuildWebGL.state) {
					platforms.Add("WebGL");
				}
				if (m_rebuildAndroid.state) {
					platforms.Add("Android");
				}
				if (m_rebuildiOS.state) {
					platforms.Add("iOS");
				}
				if (m_rebuildStandaloneWindows.state) {
					platforms.Add("StandaloneWindows");
				}


				var sb = new StringBuilder();
				for (var i = 0; i < platforms.Count; i++) {
					sb.Append(platforms[i]);
					if (i < platforms.Count - 1) {
						sb.Append(", ");
					}
				}

				if (platforms.Count == 0) {
					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button("Choose at least one platform");
					EditorGUI.EndDisabledGroup();
				} else {
					if (GUILayout.Button($"Build {sb} bundles")) {
						EditorApplication.delayCall += ExecuteBuildAllPlatforms;
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

		private void ExecuteBuildAllPlatforms() {
			var oldOutputPath = m_UserData.m_OutputPath;
			var oldBuildTarget = m_UserData.m_BuildTarget;
			var targets = new List<KeyValuePair<string, ValidBuildTarget>>();

			// для экономии времени текущую включенную платформу добавляем в список первой, если её нужно ребилдить
#if UNITY_WEBGL
            if (m_rebuildWebGL.state) {
                targets.Add(new KeyValuePair<string, ValidBuildTarget>("Web", ValidBuildTarget.WebGL));
            }
#elif UNITY_ANDROID
            if (m_rebuildAndroid.state) {
                targets.Add(new KeyValuePair<string, ValidBuildTarget>("Android", ValidBuildTarget.Android));
            }
#elif UNITY_IOS
			if (m_rebuildiOS.state) {
				targets.Add(new KeyValuePair<string, ValidBuildTarget>("iOS", ValidBuildTarget.iOS));
			}
#elif UNITY_STANDALONE
			if (m_rebuildStandaloneWindows.state) {
				targets.Add(new KeyValuePair<string, ValidBuildTarget>("StandaloneWindows", ValidBuildTarget.StandaloneWindows));
			}
#endif
			// добавляем остальные отмеченные платформы
			if (m_rebuildWebGL.state && !targets.Exists(target => target.Value == ValidBuildTarget.WebGL)) {
				targets.Add(new KeyValuePair<string, ValidBuildTarget>("Web", ValidBuildTarget.WebGL));
			}

			if (m_rebuildAndroid.state && !targets.Exists(target => target.Value == ValidBuildTarget.Android)) {
				targets.Add(new KeyValuePair<string, ValidBuildTarget>("Android", ValidBuildTarget.Android));
			}

			if (m_rebuildiOS.state && !targets.Exists(target => target.Value == ValidBuildTarget.iOS)) {
				targets.Add(new KeyValuePair<string, ValidBuildTarget>("iOS", ValidBuildTarget.iOS));
			}

			if (m_rebuildStandaloneWindows.state && !targets.Exists(target => target.Value == ValidBuildTarget.StandaloneWindows)) {
				targets.Add(new KeyValuePair<string, ValidBuildTarget>("StandaloneWindows", ValidBuildTarget.StandaloneWindows));
			}

			var sb = new StringBuilder();
			for (var i = 0; i < targets.Count; i++) {
				sb.Append(targets[i].Value);
				if (i < targets.Count - 1) {
					sb.Append(", ");
				}
			}
			Debug.Log($"<color=#00FFAA>Started bundles rebuild for platforms: {sb}...</color>");

			// билдим все платформы, которые нужно
			for (var i = 0; i < targets.Count; i++) {
				var target = targets[i];

				var symbols = string.Empty;
				switch (target.Value) {
					case ValidBuildTarget.WebGL:
						symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL);
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, "BUNDLES_BUILD");
						break;

					case ValidBuildTarget.Android:
						symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android);
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, "BUNDLES_BUILD");
						break;

					case ValidBuildTarget.iOS:
						symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS);
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, "BUNDLES_BUILD");
						break;

					case ValidBuildTarget.StandaloneWindows:
						symbols = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone);
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, "BUNDLES_BUILD");
						break;
				}

				Debug.Log($"<color=#00FFAA>Building {target.Value} bundles...</color>");

				m_UserData.m_BuildTarget = target.Value;

				if (oldOutputPath[oldOutputPath.Length - 1] == '/') {
					m_UserData.m_OutputPath = oldOutputPath + $"{target.Key}/";
				} else {
					m_UserData.m_OutputPath = oldOutputPath + $"/{target.Key}/";
				}

				ExecuteBuild();

				switch (target.Value) {
					case ValidBuildTarget.WebGL:
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.WebGL, symbols);
						break;

					case ValidBuildTarget.Android:
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, symbols);
						break;

					case ValidBuildTarget.iOS:
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.iOS, symbols);
						break;

					case ValidBuildTarget.StandaloneWindows:
						PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Standalone, symbols);
						break;
				}

				Debug.Log($"<color=#00FFAA>Finished build {target.Value} bundles</color>");
			}

			m_UserData.m_OutputPath = oldOutputPath;
			m_UserData.m_BuildTarget = oldBuildTarget;
			Debug.Log("<color=#00FFAA>Rebuild bundles for all selected platforms finished!</color>");
		}

		private void ExecuteBuild() {
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
						} catch (System.Exception e) {
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

			AssetBundleModel.Model.DataSource.BuildAssetBundles(buildInfo);

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