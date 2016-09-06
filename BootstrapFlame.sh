#!/bin/bash
curl -L https://github.com/jonathanvdc/Flame/releases/download/v0.8.3/dsc.zip > dsc.zip
unzip dsc.zip -d bin_dsc
nuget restore Flame.Cecil/Flame.Cecil.sln
# Perform one build to check that Flame is buildable
./BuildCI.sh bin_dsc/dsc.exe
# Perform a second build to make sure that Flame can bootstrap
./BuildCI.sh dsc/bin/Release/dsc.exe
rm -rf dsc.zip bin_dsc
