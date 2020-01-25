Open Sesame Compiler For Unity
===

A custom Roslyn compiler to access internals/privates for Unity.  
In other words, you can access to **any internals/privates** in other assemblies, **without reflection**.

Let's say, **"Open sesame!"**

![](https://user-images.githubusercontent.com/12690315/71837979-255d0880-30fb-11ea-99bc-3bb96b77cfa6.gif)

![](https://user-images.githubusercontent.com/12690315/70616819-a804bc00-1c52-11ea-8ea3-e24f94f6467d.gif)

[![](https://img.shields.io/github/release/mob-sakai/OpenSesameCompilerForUnity.svg?label=latest%20version)](https://github.com/mob-sakai/OpenSesameCompilerForUnity/releases)
[![](https://img.shields.io/github/release-date/mob-sakai/OpenSesameCompilerForUnity.svg)](https://github.com/mob-sakai/OpenSesameCompilerForUnity/releases)
![](https://img.shields.io/badge/unity-2018.3%20or%20later-green.svg)
[![](https://img.shields.io/github/license/mob-sakai/OpenSesameCompilerForUnity.svg)](https://github.com/mob-sakai/OpenSesameCompilerForUnity/blob/upm/LICENSE.txt)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-orange.svg)](http://makeapullrequest.com)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)

<< [Description](#description) | [Install](#install) | [Usage](#usage) >>

### What's new? [See changelog ![](https://img.shields.io/github/release-date/mob-sakai/OpenSesameCompilerForUnity.svg?label=last%20updated)](https://github.com/mob-sakai/OpenSesameCompilerForUnity/blob/upm/CHANGELOG.md)
### Do you want to receive notifications for new releases? [Watch this repo ![](https://img.shields.io/github/watchers/mob-sakai/OpenSesameCompilerForUnity.svg?style=social&label=Watch)](https://github.com/mob-sakai/OpenSesameCompilerForUnity/subscription)
### Support me on GitHub!  
[![become_a_sponsor_on_github](https://user-images.githubusercontent.com/12690315/66942881-03686280-f085-11e9-9586-fc0b6011029f.png)](https://github.com/users/mob-sakai/sponsorship)


<br><br><br><br>
## Description

> _Then Ali Baba climbed down and went to the door concealed among the bushes, and said, "Open, Sesame!" and it flew open._

This package allows to access to **any internals/privates** in other assemblies, **without reflection**.  

For details about `IgnoresAccessChecksToAttribute`, see 
[No InternalsVisibleTo, no problem – bypassing C# visibility rules with Roslyn](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/).


#### Features

* Easy to use: this package is out of the box
* Allow to access to any internal/private elements (types/members) in other assemblies, **without reflection**
  * Create instance
  * Get/set fields or properties
  * Call method
  * Create extension method that contains private access
* Processes only `AssemblyDefinitionFile` you configured
* Add/remove the scripting define symbols for each `AssemblyDefinitionFiles`
* Support C#8
* `dotnet` is not required
* Export as 'portable dll'
  * Publish a dll that works **without this package**

#### NOTE: Unsupported Features

* Inherit internal/private classes
  * Same for interfaces
  * Try `InternalsVisibleToAttribute` if possible
* Set value into readonly field
  * Use reflection
* IDE support
  * Try `InternalsVisibleToAttribute` if possible


<br><br><br><br>
## Install

Find `Packages/manifest.json` in your project and edit it to look like this:
```js
{
  "dependencies": {
    "com.coffee.open-sesame-compiler": "https://github.com/mob-sakai/OpenSesameCompilerForUnity.git",
    ...
  },
}
```

To update the package, add/change prefix `#version` to the target version.  
Or, use [UpmGitExtension](https://github.com/mob-sakai/UpmGitExtension).


### Requirement

* Unity 2018.3 or later



<br><br><br><br>
## Usage

### Compile AssemblyDefinitionFile to an 'internal accessible' dll

1. Select a `AssemblyDefinitionFile` in project view
2. Configure setting for the assembly in inspector view:  
![](https://user-images.githubusercontent.com/12690315/71836600-ad411380-30f7-11ea-8295-45fac816437a.png)
   * **Open Sesame**: Use OpenSesameCompiler instead of default csc to compile this assembly. In other words, allow this assembly to access to internals/privates to other assemblies without reflection.
   * **Symbols**: how/hide the scripting define symbols to modify for this assembly.
     * **Modify Symbols**: When compiling this assembly, add/remove semicolon separated symbols. Symbols starting with '!' will be removed.  
     **NOTE: This feature is available even when 'Open Sesame' is disabled** 
   * **Publish**: Publish this assembly as dll to the parent directory.
   * **Help**: Open help page on browser.
3. Enjoy!



<br><br><br><br>
## Demo

1. Clone demo branch in this repo and open it with Unity 2018.3 or later
```
git clone -b demo https://github.com/mob-sakai/OpenSesameCompilerForUnity.git
```
2. The project has some inaccessible compilation errors  
![](https://user-images.githubusercontent.com/12690315/71837690-6274cb00-30fa-11ea-949f-5f60b1a1dbcd.png)
3. Do not worry, they are proper errors.  
The demo project access to internals/privates:
```cs
// EditorApplication.CallDelayed is an internal-static method in UnityEditor assembly.
EditorApplication.CallDelayed(() => Debug.Log("delayed"), 1);
```
4. Select `Assets/Tests/Coffee.OpenSesame.Test.asmdef` in project view and activate 'Open Sesame' in inspector view  
![](https://user-images.githubusercontent.com/12690315/71837979-255d0880-30fb-11ea-99bc-3bb96b77cfa6.gif)
5. Run all edit mode tests in test runner view (`Windows > General > Test Runner`).  
The compilation error is gone, but some tests do not pass.  
![](https://user-images.githubusercontent.com/12690315/71838489-483bec80-30fc-11ea-9af4-83e2ddd7d894.png)
```cs
[Test]
public void DefineSymbols()
{
    const string log = "OSC_TEST is defined.";
    LogAssert.Expect(LogType.Log, log);
#if OSC_TEST // <- not defined!
    Debug.Log(log);
#endif
}

[Test]
public void RemoveSymbols()
{
    const string log = "TRACE is not defined.";
    LogAssert.Expect(LogType.Log, log);
#if !TRACE // <- defined automatically by Unity!
    Debug.Log(log);
#endif
}
```
6. Enable `symbols` to modify scripting define symbols for this assembly.  
Then edit `Modify Symbols` to `OSC_TEST;!TRACE`. This means "add `OSC_TEST` symbol and remove `TRACE` symbol for this assembly."  
![](https://user-images.githubusercontent.com/12690315/71839029-9a314200-30fd-11ea-8596-d1a6ea188741.png)
1. All tests pass!  
![](https://user-images.githubusercontent.com/12690315/71839100-c2b93c00-30fd-11ea-86a7-a2f1aac0a4cc.png)

For more details, see [the article 1 (Japanese)](https://qiita.com/mob-sakai/items/f3bbc0c45abc31ea7ac0) and [the article 2 (Japanese)](https://qiita.com/mob-sakai/items/a24780d68a6133be338f).



<br><br><br><br>
## License

* MIT



## Author

[mob-sakai](https://github.com/mob-sakai)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)  
[![become_a_sponsor_on_github](https://user-images.githubusercontent.com/12690315/66942881-03686280-f085-11e9-9586-fc0b6011029f.png)](https://github.com/users/mob-sakai/sponsorship)



## See Also

* GitHub page : https://github.com/mob-sakai/OpenSesameCompilerForUnity
* Releases : https://github.com/mob-sakai/OpenSesameCompilerForUnity/releases
* Issue tracker : https://github.com/mob-sakai/OpenSesameCompilerForUnity/issues
* Change log : https://github.com/mob-sakai/OpenSesameCompilerForUnity/blob/upm/CHANGELOG.md
* [No InternalsVisibleTo, no problem – bypassing C# visibility rules with Roslyn](https://www.strathweb.com/2018/10/no-internalvisibleto-no-problem-bypassing-c-visibility-rules-with-roslyn/)
* Nuget version: https://www.nuget.org/packages/OpenSesameCompiler
* asmdefScriptingDefines([@pCYSl5EDgo](https://github.com/pCYSl5EDgo)) : https://github.com/pCYSl5EDgo/asmdefScriptingDefines
* Csc-Manager([@pCYSl5EDgo](https://github.com/pCYSl5EDgo)) : https://github.com/pCYSl5EDgo/Csc-Manager


## Special Thanks

* Special thanks to [@pCYSl5EDgo](https://github.com/pCYSl5EDgo), your ideas contributed to improve this package.