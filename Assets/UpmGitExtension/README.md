UPM Git Extension
===

Git extension for Unity Package Manager (UPM)

![](https://user-images.githubusercontent.com/12690315/50732386-8dfa6800-11bd-11e9-81ba-0aff931212a1.png)

[![](https://img.shields.io/github/release/mob-sakai/UpmGitExtension.svg?label=latest%20version)](https://github.com/mob-sakai/UpmGitExtension/releases)
[![](https://img.shields.io/github/release-date/mob-sakai/UpmGitExtension.svg)](https://github.com/mob-sakai/UpmGitExtension/releases)
![](https://img.shields.io/badge/unity-2017%2B-green.svg)
[![](https://img.shields.io/github/license/mob-sakai/UpmGitExtension.svg)](https://github.com/mob-sakai/UpmGitExtension/blob/master/LICENSE.txt)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-orange.svg)](http://makeapullrequest.com)

<< [Description](#Description) | [WebGL Demo](#demo) | [Download](https://github.com/mob-sakai/UpmGitExtension/releases) | [Usage](#usage) | [Development Note](#development-note) >>

### What's new? [See changelog ![](https://img.shields.io/github/release-date/mob-sakai/UpmGitExtension.svg?label=last%20updated)](https://github.com/mob-sakai/UpmGitExtension/blob/develop/CHANGELOG.md)
### Do you want to receive notifications for new releases? [Watch this repo ![](https://img.shields.io/github/watchers/mob-sakai/UpmGitExtension.svg?style=social&label=Watch)](https://github.com/mob-sakai/UpmGitExtension/subscription)
### Support me on Patreon [![become_a_patron](https://user-images.githubusercontent.com/12690315/50731629-3b18b480-11ad-11e9-8fad-4b13f27969c1.png)](https://www.patreon.com/join/2343451?)



<br><br><br><br>
## Description

In Unity 2018.3, the Unity Package Manager (UPM) supported Git. :)  
https://forum.unity.com/threads/git-support-on-package-manager.573673/

This update allows us to quickly install packages on code hosting services such as GitHub.  
But, I'm not quite satisfied with the feature. :(
* Incorrect links to documents (readme, changelog, license)
* There is not a link to repo URL
* I wanna add, update, and remove the packages in the UI

This project extends the UI of Unity Package Manager for package installed using git!


#### Features

* Show link to repo URL
* Override link to document URL
  * Readme
  * Changelog
  * License
* Support GitHub


#### Future plans

* Support BitBucket
* Add package button
* Remove package button
* Update package button
* Display license
* Override the document URL with package.json



<br><br><br><br>
## Usage

1. Find the manifest.json file in the Packages folder of your project.
2. Edit it to look like this:
```js
{
  "dependencies": {
    "coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGithubExtension.git#0.1.0",
    ...
  },
}
```
3. Back in Unity, the package will be downloaded and imported.
4. To open the package manager window, select `Window > Package Manager` from the main menu in Unity.
5. The UI will be overridden when you select a package installed using git, such as UPM Git Extension.  
![](https://user-images.githubusercontent.com/12690315/50732475-850a9600-11bf-11e9-97fd-b2e5520e4f84.png)
6. Enjoy!


##### Requirement

* Unity 2018.3+



<br><br><br><br>
## Development Note

#### Develop a package for UPM

The branching strategy when I develop a package for UPM is as follows.

|Branch|Description|'Assets' directory|
|-|-|-|
|develop|Development, Testing|Included|
|master|Publishing|Included|
|upm(default)|Subtree to publish for UPM|Excluded|
|{tags}|Tags to install using UPM|Excluded|

**Steps to release a package:**
1. Update version in `package.json`.
2. Develop package project on develop branch.
3. Close all issues on GitHub for new version.
4. Generate `CHANGELOG.md` using `github_changelog_generator` and commit it.
5. Merge into master branch and publish as new version.
6. Split subtree into ump branch.
7. Squash and push.
8. Tag on ump branch as new version.
9. Release.


#### About default document URL

The document URL in UnityPackageManager 2.0.3 is hard-coded.  
The default values are as follows:
* View document: `http://docs.unity3d.com/Packages/{ShortVersionId}/index.html` or official manual page (only for built in package)
* View changelog: `http://docs.unity3d.com/Packages/{ShortVersionId}/changelog/CHANGELOG.html`
* View licenses: `http://docs.unity3d.com/Packages/{ShortVersionId}/license/index.html` or `https://unity3d.com/legal/licenses/Unity_Companion_License`

ShortVersionId is defined as follows:
* `{PackageName}@{MajorVersion}.{MinorVersion}`
* For example: `coffee.upm-git-extension@1.0`


#### How to add/update/remove a package from script?

Use `UnityEditor.PackageManager.Client` class.  
https://docs.unity3d.com/ScriptReference/PackageManager.Client.html
* Add/Update: `Client.Add({PackageId})`
* Remove: `Client.Remove({PackageName})`

PackageId is defined as follows:
* `{PackageName}@{MajorVersion}.{MinorVersion}.{PatchVersion}` (Unity official package)
* `{PackageName}@{RepoURL}#{BranchOrTagOrRevision}`
* For example: `com.unity.package-manager-ui@2.0.3`, `coffee.upm-git-extension@https://github.com/mob-sakai/UpmGitExtension.git#1.0.0`



<br><br><br><br>
## License

* MIT



## Author

[mob-sakai](https://github.com/mob-sakai)  
[![become_a_patron](https://user-images.githubusercontent.com/12690315/50731615-ce9db580-11ac-11e9-964f-e0423533dc69.png)](https://www.patreon.com/join/2343451?)



## See Also

* GitHub page : https://github.com/mob-sakai/UpmGitExtension
* Releases : https://github.com/mob-sakai/UpmGitExtension/releases
* Issue tracker : https://github.com/mob-sakai/UpmGitExtension/issues
* Current project : https://github.com/mob-sakai/UpmGitExtension/projects/1
* Change log : https://github.com/mob-sakai/UpmGitExtension/blob/master/CHANGELOG.md
