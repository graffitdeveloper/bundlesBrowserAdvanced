# Asset Bundle Browser advanced
This is advanced version of official Unity Asset Bundle Browser tool
(https://github.com/Unity-Technologies/AssetBundles-Browser)

This enhance adds brand new tab "Advanced Build".
Features:
- Ability to build Asset Bundles for multiple build targets by one click. It improves your productivity by not having to switch the target platform and build bundles for it manually each time. Supports all common platforms, you can choose your own set of build targets in "Enabled build targets" foldout
- Ability to hide platform specific code in such way:
```c#
#if !BUNDLES_BUILD
// Platform-specific code here
#endif
```
It allows you not to get build errors when you rebuild bundles with platform specific code;
- Improved speed of Bundle Assets list refresh in "Configure" tab;
- Ability to build specific Asset Bundle, instead of build all of them, when it's required;
- Fixed bug when bundle name and bundle folder are conflicting, for instance after switch branch. In this case you will see dialog window that offers you to recache Asset Bundles List.

For some unique cases - classic Build tab wasn't removed, so you can start to use Asset Bundle Browser advanced without any doubts.
