#!/bin/bash

WORKSPACE=.
LUBAN_DLL=$WORKSPACE/Luban/Luban.dll
CONF_ROOT=.

dotnet $LUBAN_DLL \
    -t all \
	-c cs-bin \
    -d bin \
    --conf $CONF_ROOT/luban.conf \
	  -x outputCodeDir=..\Assets/Gen ^
    -x outputDataDir=..\Assets\GenerateDatas\bytes
