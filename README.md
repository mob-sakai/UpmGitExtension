# <img src="https://git-scm.com/images/logos/downloads/Git-Icon-1788C.svg" height="24px" > UPM Git Extension

This package enhances the user interface (UI) of the Unity Package Manager (UPM) specifically for packages installed
from a git repository.

![](https://user-images.githubusercontent.com/12690315/60764681-20c28380-a0c9-11e9-9c3c-75e3d4e0279e.png)

[![](https://img.shields.io/npm/v/com.coffee.upm-git-extension?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.coffee.upm-git-extension/)
[![](https://img.shields.io/github/v/release/mob-sakai/UpmGitExtension?include_prereleases)](https://github.com/mob-sakai/UpmGitExtension/releases)
[![](https://img.shields.io/github/license/mob-sakai/UpmGitExtension.svg)](https://github.com/mob-sakai/UpmGitExtension/blob/main/LICENSE.txt)  
![](https://img.shields.io/badge/Unity-2018.3+-57b9d3.svg?style=flat&logo=unity)
![](https://github.com/mob-sakai/UpmGitExtension/actions/workflows/test.yml/badge.svg?branch=develop)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-orange.svg)](http://makeapullrequest.com)
[![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai)

<< [📝 Description](#-description) | [⚙ Installation](#-installation) | [🚀 Usage](#-usage) | [🛠 Development Note](#-development-note) | [🤝 Contributing](#-contributing) >>

## 📝 Description

In Unity 2018.3, the Unity Package Manager (UPM) introduced Git support, providing a convenient way to install packages
from code hosting services such as GitHub. However, certain limitations and shortcomings were identified, including:

- Incorrect links to documents (readme, changelog, license)
- Lack of a direct link to the repository URL
- Limited functionality for adding, updating, and removing packages through the UI

This project addresses these issues by extending the UI of the Unity Package Manager for packages installed using Git.

### Features

- Direct link to the Git repository URL
- Links to offline documents, including:
    - Documentations
    - Changelog
    - License
- Support for various Git hosting platforms such as GitHub, GitLab, Bitbucket, Azure DevOps, etc.
- Installation of packages from Git repository URLs using the UI
- Updating packages with a specific tag/branch
- Removing packages
- Compatibility with Unity 2018.3 or later
- Support for `.Net 3.5`, `.Net 4.x`, and `.Net Standard 2.0`
- Version filtering
- Support for path query parameters (for Unity 2019.3.4f or later)
- (Version 2.0.0) Git repositories are cached, and installed/searched packages are automatically indexed:
    - Cached repositories are shared between different projects
    - Cached repository URLs are displayed as history in the installation window
    - Indexed packages are shown in `My Registries`
- (Version 2.0.0) Additional menu options to open `manifest.json` with a code editor, open the cache directory, clear
  the cache, and fetch packages:
  ![](https://user-images.githubusercontent.com/12690315/169232173-943ee8cf-9d18-435d-aea2-3fdd16538cf7.png)

<br><br>

## ⚙ Installation

This package requires as following:

- **v1.x**: Unity 2018.3 to 2019.4
- **v2.x**: Unity 2020.1 or later

#### Install via OpenUPM

This package is available on [OpenUPM](https://openupm.com) package registry.
This is the preferred method of installation, as you can easily receive updates as they're released.

If you have [openupm-cli](https://github.com/openupm/openupm-cli) installed, then run the following command in your
project's directory:

```sh
# for Unity 2020 or later
openupm add com.coffee.upm-git-extension
# for Unity 2018 or 2019
openupm add com.coffee.upm-git-extension@v1 
```

#### Install via UPM (using Git URL)

Navigate to your project's Packages folder and open the `manifest.json` file. Then add this package somewhere in
the `dependencies` block:

```json
{
  "dependencies": {
    // for Unity 2020 or later
    "com.coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGitExtension.git",
    // for Unity 2018 or 2019
    "com.coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGitExtension.git#v1",
    ...
  },
}
```

To update the package, change suffix `#{version}` to the target version.

* e.g. `"com.coffee.upm-git-extension": "https://github.com/mob-sakai/UpmGitExtension.git#2.1.0",`

<br><br>

## 🚀 Usage

### Install a Package from a Git Repository

1.
Click ![giticon](https://user-images.githubusercontent.com/12690315/60764763-7fd4c800-a0ca-11e9-957b-ca68e3ca6123.png)
button in the package manager UI to open the `Install Package Window`.  
![](https://user-images.githubusercontent.com/12690315/60766233-dbf71680-a0e1-11e9-8303-fbd790e9e35b.png)  
![](https://user-images.githubusercontent.com/12690315/60764768-91b66b00-a0ca-11e9-9ccd-9fef88c77d5e.png)

2. Input a git repository URL and click the `Find Versions` button. In Unity 2019.3.4 or later, you can specify a
   subdirectory.  
   ![](https://user-images.githubusercontent.com/12690315/60766258-4314cb00-a0e2-11e9-91f8-3aad514450bc.png)

3. Select a tag or branch and click the `Find Package` button.  
   ![](https://user-images.githubusercontent.com/12690315/60766257-4314cb00-a0e2-11e9-8b2e-23efc50ded72.png)

4. Wait a few seconds for validation.

5. Click the `Install Package` button to install the package.  
   ![](https://user-images.githubusercontent.com/12690315/60766259-4314cb00-a0e2-11e9-9b89-0bc0d4f71517.png)

### Update Package with a Specific Tag or Branch as Version

You can update or remove the package in your project, _just as you would for official packages._

<br><br>

## 🛠 Development Note

### Develop a package for UPM

See https://www.patreon.com/posts/25070968, https://www.jianshu.com/u/275cca6e5f17 (Chinese)

<br><br>

## 🤝 Contributing

### Issues

Issues are incredibly valuable to this project:

- Ideas provide a valuable source of contributions that others can make.
- Problems help identify areas where this project needs improvement.
- Questions indicate where contributors can enhance the user experience.

### Pull Requests

Pull requests offer a fantastic way to contribute your ideas to this repository.  
Please refer to [CONTRIBUTING.md](https://github.com/mob-sakai/UpmGitExtension/tree/develop/CONTRIBUTING.md) and [develop branch](https://github.com/mob-sakai/UpmGitExtension/tree/develop) for guidelines.

### Support

This is an open-source project developed during my spare time.  
If you appreciate it, consider supporting me.  
Your support allows me to dedicate more time to development. 😊

[![](https://user-images.githubusercontent.com/12690315/50731629-3b18b480-11ad-11e9-8fad-4b13f27969c1.png)](https://www.patreon.com/join/2343451?)  
[![](https://user-images.githubusercontent.com/12690315/66942881-03686280-f085-11e9-9586-fc0b6011029f.png)](https://github.com/users/mob-sakai/sponsorship)

<br><br>

## License

* MIT

## Author

* ![](https://user-images.githubusercontent.com/12690315/96986908-434a0b80-155d-11eb-8275-85138ab90afa.png) [mob-sakai](https://github.com/mob-sakai) [![](https://img.shields.io/twitter/follow/mob_sakai.svg?label=Follow&style=social)](https://twitter.com/intent/follow?screen_name=mob_sakai) ![GitHub followers](https://img.shields.io/github/followers/mob-sakai?style=social)

## See Also

- GitHub page : https://github.com/mob-sakai/UpmGitExtension
- Releases : https://github.com/mob-sakai/UpmGitExtension/releases
- Issue tracker : https://github.com/mob-sakai/UpmGitExtension/issues
- Current project : https://github.com/mob-sakai/UpmGitExtension/projects/1
- Change log : https://github.com/mob-sakai/UpmGitExtension/blob/main/CHANGELOG.md
