Package Development Sandbox
===

**NOTE: This branch is for development purposes only.**

## Develop the package with sandbox branch

1. Fork the repository.
2. Clone `sandbox` branch with submodule.  
3. Develop the package
4. Test the package with test runnner (`Window > Generals > Test Runner`)
5. Commit with a message based on [Angular Commit Message Conventions](https://gist.github.com/stephenparish/9941e89d80e2bc58a153)
6. Create a pull request on GitHub

For details, see [CONTRIBUTING](https://github.com/mob-sakai/ParticleEffectForUGUI/blob/upm/CONTRIBUTING.md) and [CODE_OF_CONDUCT](https://github.com/mob-sakai/ParticleEffectForUGUI/blob/upm/CODE_OF_CONDUCT.md).


## How to release this package

When you push to `preview`, `master` or `v1.x` branch, this package is automatically released by GitHub Action.

* Update version in `package.json` 
* Update CHANGELOG.md
* Commit documents and push
* Update and tag upm branch
* Release on GitHub
* ~~Publish npm registory~~

Alternatively, you can release it manually with the following command:

```bash
$ npm run release -- --no-ci
```
