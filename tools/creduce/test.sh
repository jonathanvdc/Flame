#!/usr/bin/env bash

# This script tests if feeding a working C# file to ilopt either
# changes its semantics or makes it break.
#
# Use it like so:
#
#     creduce --not-c test.sh main.cs
#

csc /o+ main.cs && \
mono main.exe > normal-output.txt && \
$(dirname $0)/../../src/ILOpt/bin/Release/ilopt.exe main.exe && \
(! mono main.opt.exe > opt-output.txt || \
 ! diff normal-output.txt opt-output.txt)
