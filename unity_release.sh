#!/bin/bash -e

# NOTE: Run the following command at the prompt
#   bash <(curl -sL 'https://gist.github.com/mob-sakai/a883999a32dd8b1927639e46b3cd6801/raw/unity_release.sh')
# NOTE: Set an environment variable `CHANGELOG_GITHUB_TOKEN` by running the following command at the prompt, or by adding it to your shell profile (e.g., ~/.bash_profile or ~/.zshrc):
#   export CHANGELOG_GITHUB_TOKEN="«your-40-digit-github-token»"



# 1. << Input release version >>
echo -e ">> Start Github Release:"
PACKAGE_NAME=`node -pe 'require("./package.json").name'`
echo -e ">> Package name: ${PACKAGE_NAME}"
PACKAGE_SRC=`node -pe 'require("./package.json").src'`
echo -e ">> Package src: ${PACKAGE_SRC}"
CURRENT_VERSION=`grep -o -e "\"version\".*$" package.json | sed -e "s/\"version\": \"\(.*\)\".*$/\1/"`

read -p "[? (1/8) Input release version (for current: ${CURRENT_VERSION}): " RELEASE_VERSION
[ -z "${RELEASE_VERSION}" ] && exit

read -p "[? Are the issues on this release closed all? (y/N):" yn
case "$yn" in [yY]*) ;; *) exit ;; esac

read -p "[? Is package editor only? (y/N):" yn
case "$yn" in [yY]*) EDITOR_ONLY=true;; *) ;; esac

read -p "[? Is package for UnityPackageManager? (y/N):" yn
case "$yn" in [yY]*) UNITY_PACKAGE_MANAGER=true;; *) ;; esac

echo -e ">> OK"



# 2. << Update version in package.json >>
echo -e "\n>> (2/8) Update version... package.json"
git checkout -B release develop
sed -i '' -e "s/\"version\": \(.*\)/\"version\": \"${RELEASE_VERSION}\",/g" package.json
echo -e ">> OK"



# 4. << Generate change log >>
CHANGELOG_GENERATOR_ARG=`grep -o -e ".*git\"$" package.json | sed -e "s/^.*\/\([^\/]*\)\/\([^\/]*\).git.*$/--user \1 --project \2/"`
CHANGELOG_GENERATOR_ARG="--future-release ${RELEASE_VERSION} ${CHANGELOG_GENERATOR_ARG}"
echo -e "\n>> (4/8) Generate change log... ${CHANGELOG_GENERATOR_ARG}"
github_changelog_generator ${CHANGELOG_GENERATOR_ARG}

git diff -- CHANGELOG.md
read -p "[? Is the change log correct? (y/N):" yn
case "$yn" in [yY]*) ;; *) exit ;; esac
echo -e ">> OK"



# 6. << Commit release files >>
echo -e "\n>> (6/8) Commit release files..."
cp -f package.json CHANGELOG.md README.md $PACKAGE_SRC
git add -u
git commit -m "update documents for $RELEASE_VERSION"
echo -e ">> OK"



#  7. << Split for upm >>
if [ "$UNITY_PACKAGE_MANAGER" == "true" ]; then
  echo -e "\n>> Split for upm..."
  git fetch
  git show-ref --quiet refs/remotes/origin/upm && git branch -f upm origin/upm
  git-snapshot --prefix="$PACKAGE_SRC" --message="$RELEASE_VERSION" --branch=upm
  git push origin upm
fi



# 7. << Merge and push master and develop branch >>
echo -e "\n>> (7/8) Merge and push..."
git checkout master
git merge --no-ff release -m "release $RELEASE_VERSION"
git branch -D release
git push origin master
git checkout develop
git merge --ff master
git push origin develop
echo -e ">> OK"



# 8. << Upload unitypackage and release on Github >>
echo -e "\n>> (8/8) Releasing..."
[ "$UNITY_PACKAGE_MANAGER" == "true" ] && git checkout upm -f
gh-release --name $RELEASE_VERSION --tag_name $RELEASE_VERSION
echo -e ">> OK"




echo -e "\n\n>> $PACKAGE_NAME $RELEASE_VERSION has been successfully released!\n"
