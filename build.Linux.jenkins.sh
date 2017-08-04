#!/bin/sh

PLATFRM="Any CPU"

sn -k LXRrestore/LXRrestore.snk
sn -k LXRbackup/LXRbackup.snk

msbuild /t:clean /p:Configuration="Debug" /p:Platform=${PLATFRM} elykseer-cli.Linux.sln
msbuild /p:Configuration="Debug" /p:Platform=${PLATFRM} elykseer-cli.Linux.sln

