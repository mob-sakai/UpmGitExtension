#!/bin/sh

COMPILER=Packages/com.coffee.internal-accessible/Compiler~/Compiler2.0.csproj
OUT_DIR=Packages/com.coffee.upm-git-extension/Editor
PREFIX=Coffee.UpmGitExtension.Bridge

dotnet restore $COMPILER
dotnet run -p $COMPILER -- $PREFIX.2018.3.csproj $OUT_DIR/$PREFIX.2018.3.dll
dotnet run -p $COMPILER -- $PREFIX.2019.1.csproj $OUT_DIR/$PREFIX.2019.1.dll
dotnet run -p $COMPILER -- $PREFIX.2019.2.csproj $OUT_DIR/$PREFIX.2019.2.dll
dotnet run -p $COMPILER -- $PREFIX.2019.3.csproj $OUT_DIR/$PREFIX.2019.3.dll
