@echo off

rem 
rem Output available branch / tag names for Unity Package Manager
rem 

setlocal

set REPO_URL=%1
set DIR=%2
set UNITY=%3

rem Create repo.
if not exist %DIR% ( 
	mkdir %DIR%
	cd %DIR%
	git init
	git remote add origin %REPO_URL%
) else ( 
	cd %DIR%
)

rem Clear cache file.
type nul > versions

rem Fetch all branches/tags.
git fetch --depth=1 -fq --prune origin "refs/tags/*:refs/tags/*" "+refs/heads/*:refs/remotes/origin/*"
for /F "usebackq tokens=2" %%i in (`git show-ref`) do (
	setlocal enabledelayedexpansion
	rem Check if package.json and package.json.meta exist.
	git checkout %%i --  package.json package.json.meta
	if !ERRORLEVEL! equ 0 (
		rem Check supported unity versions.
		for /F "usebackq tokens=2 delims=:" %%v in (`findstr "unity" package.json`) do (
			set f=%%v
			set f=!f:,=!
			set f=!f:	=!
			set f=!f: =!
			
			rem Output only available names
			if !f! leq "%UNITY%" @echo %%i >> versions
		)
	)
	endlocal
)
