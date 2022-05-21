# UPM Git Extension

This package extends the UI of Unity Package Manager (UPM) for the packages installed from git repository.

![](https://user-images.githubusercontent.com/12690315/60764681-20c28380-a0c9-11e9-9c3c-75e3d4e0279e.png)

[![openupm](https://img.shields.io/npm/v/com.coffee.upm-git-extension?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.coffee.upm-git-extension/)
[![GitHub release (latest SemVer including pre-releases)](https://img.shields.io/github/v/release/mob-sakai/UpmGitExtension?include_prereleases)](https://github.com/mob-sakai/UpmGitExtension/releases)
![](https://img.shields.io/badge/unity-2018.3%20or%20later-green.svg)
[![](https://img.shields.io/github/license/mob-sakai/UpmGitExtension.svg)](https://github.com/mob-sakai/UpmGitExtension/blob/upm/LICENSE.txt)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-orange.svg)](http://makeapullrequest.com)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)

<< [Description](#description) | [Installation](#installation-for-unity-2020-or-later) | [Usage](#usage) | [Development Note](#development-note) >>

### What's new? [See changelog ![](https://img.shields.io/github/release-date/mob-sakai/UpmGitExtension.svg?label=last%20updated)](https://github.com/mob-sakai/UpmGitExtension/blob/upm/CHANGELOG.md)

### Do you want to receive notifications for new releases? [Watch this repo ![](https://img.shields.io/github/watchers/mob-sakai/UpmGitExtension.svg?style=social&label=Watch)](https://github.com/mob-sakai/UpmGitExtension/subscription)

### Support me on GitHub!

<br><br><br><br>

## Description

In Unity 2018.3, the Unity Package Manager (UPM) supported Git. :)  
https://forum.unity.com/threads/git-support-on-package-manager.573673/

This update allows us to quickly install packages on code hosting services such as GitHub.  
But, I'm not quite satisfied with the feature. :(

- Incorrect links to documents (readme, changelog, license)
- There is not a link to repo URL
- I wanna add, update, and remove the packages in the UI

This project extends the UI of Unity Package Manager for package installed using git!

### Features

- Link to git repository URL
- Link to offline documents
  - Documentations
  - Changelog
  - License
- Support GitHub, GitLab, Bitbucket, Azure Dev Ops etc.
- Install package from git repository URL with UI
- Update package with a specific tag/branch
- Remove package
- Support Unity 2018.3 or later
- Support `.Net 3.5`, `.Net 4.x` and `.Net Standard 2.0`
- Version filtering
- Support path query parameter (for Unity 2019.3.4f or later)
- (2.0.0) Git repositories are cached and installed/searched packages are automatically indexed
  - Cached repositories will be shared between different projects
  - Cached repository urls will be displayed as history in the installation window  
![](https://user-images.githubusercontent.com/12690315/169232949-260b6fe0-b31c-4179-b9d1-3385cf1a2ac1.png)
  - Indexed packages will be displayed in `My Registries`  
![](https://user-images.githubusercontent.com/12690315/169231822-18dfb10f-894f-4883-9331-297f9e50a0e6.png)
- (2.0.0) Add menu to open `manifest.json` with code editor, open cache directory, clear cache and fetch packages  
![](https://user-images.githubusercontent.com/12690315/169232173-943ee8cf-9d18-435d-aea2-3fdd16538cf7.png)

<br><br><br><br>

## Installation (for Unity 2020 or later)

### Using OpenUPM

This package is available on [OpenUPM](https://openupm.com).  
You can install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.coffee.upm-git-extension
```

<br><br>

### Using Unity Package Manager

Find the manifest.json file in the Packages folder of your project and edit it to look like this:

```
{
  "dependencies": {
    "com.coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGitExtension.git",
    ...
  },
}
```

<br><br>

## Installation (For Unity 2018 or 2019)

`v2.x` supports 2020.1 or later.  
Please use `v1.x` for Unity 2018 or 2019.

Install via OpenUPM

```
openupm add com.coffee.upm-git-extension@v1
```

or install via Unity Package Manager

```
{
  "dependencies": {
    "com.coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGitExtension.git#v1",
    ...
  },
}
```

### Requirement

- Unity 2018.3 or later
  - Unity 2018 or 2019 -> `v1.x`
  - Unity 2020 or later -> `v2.x`
- Git (executable on command-line)

<br><br><br><br>

## Usage

### Install a package from git repository

1. Click ![giticon](https://user-images.githubusercontent.com/12690315/60764763-7fd4c800-a0ca-11e9-957b-ca68e3ca6123.png) button in package manager UI to open `Install Package Window`.  
   ![](https://user-images.githubusercontent.com/12690315/60766233-dbf71680-a0e1-11e9-8303-fbd790e9e35b.png)  
   ![](https://user-images.githubusercontent.com/12690315/60764768-91b66b00-a0ca-11e9-9ccd-9fef88c77d5e.png)
1. Input a git repository url and click `Find Versions` button.  
In Unity 2019.3.4 or later, you can specify a subdirectory.  
   ![](https://user-images.githubusercontent.com/12690315/60766258-4314cb00-a0e2-11e9-91f8-3aad514450bc.png)
2. Select a tag or branch and click `Find Package` button.  
   ![](https://user-images.githubusercontent.com/12690315/60766257-4314cb00-a0e2-11e9-8b2e-23efc50ded72.png)
3. Wait a few seconds for validation.
4. Click `Add Package` button to install the package.  
   ![](https://user-images.githubusercontent.com/12690315/60766259-4314cb00-a0e2-11e9-9b89-0bc0d4f71517.png)

### Update package with a specific tag or branch as version

You can update the package in your project, _just like official packages._

#### For Unity 2019.1 or later

1. Select the version of the package
2. Click `Update To ***` button  
   ![](https://user-images.githubusercontent.com/12690315/60766318-fc73a080-a0e2-11e9-9020-23dfc05939a0.png)

#### For Unity 2018.3

1. Click version popup and select a tag or branch in repository  
   ![](https://user-images.githubusercontent.com/12690315/60766391-1cf02a80-a0e4-11e9-8c3f-d420b7e84b46.png)
2. Click `Update To` button

### Remove package

You can update the package from your project, just like official packages.

1. Click `Remove` button  
   ![](https://user-images.githubusercontent.com/12690315/60766319-fd0c3700-a0e2-11e9-9154-b88161496a3e.png)

<br><br><br><br>

## Development Note

### Develop a package for UPM

See https://www.patreon.com/posts/25070968, https://www.jianshu.com/u/275cca6e5f17 (Chinese)

<br><br><br><br>

## Contributing

### Issues

Issues are very valuable to this project.

- Ideas are a valuable source of contributions others can make
- Problems show where this project is lacking
- With a question you show where contributors can improve the user experience

### Pull Requests

Pull requests are, a great way to get your ideas into this repository.  
See [CONTRIBUTING.md](/../../blob/upm/CONTRIBUTING.md).

### Support

This is an open source project that I am developing in my spare time.  
If you like it, please support me.  
With your support, I can spend more time on development. :)

[![](https://user-images.githubusercontent.com/12690315/50731629-3b18b480-11ad-11e9-8fad-4b13f27969c1.png)](https://www.patreon.com/join/mob_sakai?)  
[![](https://user-images.githubusercontent.com/12690315/66942881-03686280-f085-11e9-9586-fc0b6011029f.png)](https://github.com/users/mob-sakai/sponsorship)

<br><br><br><br>
## License

* MIT

## Author

[mob-sakai](https://github.com/mob-sakai)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai) 


## See Also

- GitHub page : https://github.com/mob-sakai/UpmGitExtension
- Releases : https://github.com/mob-sakai/UpmGitExtension/releases
- Issue tracker : https://github.com/mob-sakai/UpmGitExtension/issues
- Current project : https://github.com/mob-sakai/UpmGitExtension/projects/1
- Change log : https://github.com/mob-sakai/UpmGitExtension/blob/upm/CHANGELOG.md
