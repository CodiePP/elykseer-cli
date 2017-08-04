@echo off

rem set PLATFRM="x86"
set PLATFRM="Any CPU"

sn -k LXRrestore\LXRrestore.snk
sn -k LXRbackup\LXRbackup.snk

msbuild /t:clean /p:Configuration="Debug" /p:Platform=%PLATFRM% elykseer-cli.Win32.sln
msbuild /p:Configuration="Debug" /p:Platform=%PLATFRM% elykseer-cli.Win32.sln

