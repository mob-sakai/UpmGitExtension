UPM Git Extension
===

Git extension for Unity Package Manager (UPM)

![](https://user-images.githubusercontent.com/12690315/52934439-cb940880-3399-11e9-8cc7-a0b70e3e64d8.png)

[![](https://img.shields.io/github/release/mob-sakai/UpmGitExtension.svg?label=latest%20version)](https://github.com/mob-sakai/UpmGitExtension/releases)
[![](https://img.shields.io/github/release-date/mob-sakai/UpmGitExtension.svg)](https://github.com/mob-sakai/UpmGitExtension/releases)
![](https://img.shields.io/badge/unity-2017%2B-green.svg)
[![](https://img.shields.io/github/license/mob-sakai/UpmGitExtension.svg)](https://github.com/mob-sakai/UpmGitExtension/blob/upm/LICENSE.txt)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-orange.svg)](http://makeapullrequest.com)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)

<< [Description](#Description) | [WebGL Demo](#demo) | [Download](https://github.com/mob-sakai/UpmGitExtension/releases) | [Usage](#usage) | [Development Note](#development-note) >>

### What's new? [See changelog ![](https://img.shields.io/github/release-date/mob-sakai/UpmGitExtension.svg?label=last%20updated)](https://github.com/mob-sakai/UpmGitExtension/blob/upm/CHANGELOG.md)
### Do you want to receive notifications for new releases? [Watch this repo ![](https://img.shields.io/github/watchers/mob-sakai/UpmGitExtension.svg?style=social&label=Watch)](https://github.com/mob-sakai/UpmGitExtension/subscription)
### Support me on Patreon! [![become_a_patron](https://user-images.githubusercontent.com/12690315/50731629-3b18b480-11ad-11e9-8fad-4b13f27969c1.png)](https://www.patreon.com/join/2343451?)



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

* Show link to repository URL
* Override link to the document URL
  * Readme
  * Changelog
  * License
* Support GitHub, Bitbucket and GitLab
* Add package from url
* Remove package
* Update package with a specific tag/branch
* Support Unity 2019.1+
* Support .Net 3.5 & 4.x


#### Future plans

* View offline documents for private repository
* Display license
* Override the document URL with package.json



<br><br><br><br>
## Install

Find `Packages/manifest.json` in your project and edit it to look like this:
```js
{
  "dependencies": {
    "com.coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGitExtension.git#0.8.1",
    ...
  },
}
```
For Unity 2019.1+, use UpmGitExtension 0.5.0 or higher.


##### Requirement

* Unity 2018.3+ *(including 2019.1+)*



<br><br><br><br>
## Usage

#### Add package from url

![](https://user-images.githubusercontent.com/12690315/52932712-0dba4b80-3394-11e9-8cb7-f141fcb24cb6.png)

1. Click `+` button and select `Add package from url` to open window
2. Input repository url and select a tag or branch
3. Wait a few seconds for validation
4. Click `Add` button to add package


#### Update package with a specific tag/branch

![](https://user-images.githubusercontent.com/12690315/52932818-638ef380-3394-11e9-973f-8e2e1dc72342.png)

1. Click version popup and select a tag/branch in repository
2. Click `Update To` button


#### Remove package

1. Click `Remove` button



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
7. Tag on ump branch as new version.
8. Release.

For details, see https://www.patreon.com/posts/25070968.


#### About default document URL

The document URL in UnityPackageManager 2.0.3 is hard-coded.  
The default values are as follows:
* View document: `http://docs.unity3d.com/Packages/{ShortVersionId}/index.html` or official manual page (only for built in package)
* View changelog: `http://docs.unity3d.com/Packages/{ShortVersionId}/changelog/CHANGELOG.html`
* View licenses: `http://docs.unity3d.com/Packages/{ShortVersionId}/license/index.html` or `https://unity3d.com/legal/licenses/Unity_Companion_License`

ShortVersionId is defined as follows:
* `{PackageName}@{MajorVersion}.{MinorVersion}`
* For example: `com.coffee.upm-git-extension@0.1`


#### How to add/update/remove a package from script?

Use `UnityEditor.PackageManager.Client` class.  
https://docs.unity3d.com/ScriptReference/PackageManager.Client.html
* Add/Update: `Client.Add({PackageId})`
* Remove: `Client.Remove({PackageName})`

PackageId is defined as follows:
* `{PackageName}@{MajorVersion}.{MinorVersion}.{PatchVersion}` (Unity official package)
* `{PackageName}@{RepoURL}#{BranchOrTagOrRevision}`
* For example: `com.unity.package-manager-ui@2.0.3`, `coffee.upm-git-extension@https://github.com/mob-sakai/UpmGitExtension.git#1.1.1`



<br><br><br><br>
## License

* MIT



## Author

[mob-sakai](https://github.com/mob-sakai)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)  
[![become_a_patron](https://user-images.githubusercontent.com/12690315/50731615-ce9db580-11ac-11e9-964f-e0423533dc69.png)](https://www.patreon.com/join/2343451?)



## See Also

* GitHub page : https://github.com/mob-sakai/UpmGitExtension
* Releases : https://github.com/mob-sakai/UpmGitExtension/releases
* Issue tracker : https://github.com/mob-sakai/UpmGitExtension/issues
* Current project : https://github.com/mob-sakai/UpmGitExtension/projects/1
* Change log : https://github.com/mob-sakai/UpmGitExtension/blob/upm/CHANGELOG.md
