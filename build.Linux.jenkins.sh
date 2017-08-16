#!/bin/bash

set -e

PLATFRM="Any CPU"

sn -k LXRrestore/LXRrestore.snk
sn -k LXRbackup/LXRbackup.snk

xbuild /t:clean /p:Configuration="Debug" /p:Platform="${PLATFRM}" elykseer-cli.Mono.sln
xbuild /p:Configuration="Debug" /p:Platform="${PLATFRM}" elykseer-cli.Mono.sln

