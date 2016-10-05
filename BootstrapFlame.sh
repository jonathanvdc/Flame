#!/bin/bash
curl -L https://github.com/jonathanvdc/Flame/releases/download/v0.8.3/dsc.zip > dsc.zip
unzip -o dsc.zip -d bin_dsc
nuget restore Flame.Cecil/Flame.Cecil.sln
# Perform one build to check that Flame is buildable
./BuildCI.sh bin_dsc/dsc.exe
# Remove the version of dsc that we curled down.
rm -rf dsc.zip bin_dsc
# Perform a second build to make sure that Flame can bootstrap
./BuildCI.sh dsc/bin/Release/dsc.exe
