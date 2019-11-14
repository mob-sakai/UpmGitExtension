#!/bin/sh

REPO_URL=$1
DIR=$2
UNITY=$3

# Get value from package.json
get_from_package_json() {
    echo `grep -o -e "\"$1\".*$" package.json | head -n 1 | cut -d ':' -f 2 | sed -e "s/[\",\t ]//g"`
}

# Create repo.
if [ ! -e "${DIR}" ]; then
    mkdir "${DIR}"
    cd "${DIR}"
    git init
    git remote add origin "${REPO_URL}"
else
    cd "${DIR}"
fi

# Clear cache file.
: > versions

# Fetch all branches/tags.
git fetch --depth=1 -fq --prune origin 'refs/tags/*:refs/tags/*' '+refs/heads/*:refs/remotes/origin/*'
for ref in `git show-ref | cut -d ' ' -f 2`
do
	# Check if package.json and package.json.meta exist.
    git checkout -q $ref -- package.json package.json.meta
    [ $? != 0 ] && continue

	# Check supported unity versions.
    SUPPORTED_VERSION=`get_from_package_json unity`
    VERSION=`get_from_package_json version`
    [[ "${UNITY}" < "${SUPPORTED_VERSION}" ]] && continue

	# Output only available names
    NAME=`get_from_package_json name`
    echo ${ref},${VERSION},${NAME} >> versions
done
