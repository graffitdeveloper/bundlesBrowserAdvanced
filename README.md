# Asset Bundle Browser advanced
This is advanced version of official Unity Asset Bundle Browser tool
(https://github.com/Unity-Technologies/AssetBundles-Browser)

This enhance adds brand new tab "Advanced Build".
Features:
- Ability to build Asset Bundles on multiples platforms with only one click. This improves your productivity by not having to manually switch the target platform and build bundles for it each time.
Current supported platforms list: Standalone Windows, Android, iOS, WebGL;
- Ability to hide platform specific lines by 
```c#
#if !BUNDLES_BUILD
// Platform-specific code here
#endif
```
directive. It allows you to not get error builds when you rebuild bundles without platform specific lines remove\comment;
- Improved speed of Bundle Assets list refresh in "Configure" tab;
- Ability to build specific Asset Bundle, instead to build all of them, when it's required;
- Fixed bug when bundle name and bundle folder conflicts, for instance after switch branch. In this case you will see dialog window that offers you to recache Asset Bundles List.

For some unique cases or not supported platforms build - original build tab wasn't removed.
