UPM Git Extension
===

**NOTE: This branch is for development purposes only.**
**To use a released package, see [Releases page](https://github.com/mob-sakai/UpmGitExtension/releases) or [default branch](https://github.com/mob-sakai/UpmGitExtension).**

## How to develop internal bridge

* Modify `Coffee.UpmGitExtension.Bridge.*.csproj`
* Execute `generate-dlls.sh`

## How to release

When you push to the preview or master branch, this package is automatically released by GitHubAction.

* Update version in `package.json` 
* Update changelog.md
* Commit documents and push
* Update and tag upm branch
* Release on GitHub
* (Publish npm registory)

Alternatively, you can release it manually with the following command:

```bash
$ npx upm-release --pkg-root Packages/com.coffee.upm-git-extension --debug
```
