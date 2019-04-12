#!/usr/bin/env python3

# This is a super simple Python script that accepts a single version string,
# the (AppVeyor) build number, and prints a NuGet package version number for
# the corresponding Flame package.

import sys
import os

build_number = sys.argv[1]
split_build_number = build_number.split('.')
base_number = '.'.join(split_build_number[0:3])
if os.getenv('APPVEYOR_REPO_TAG', 'False') == 'True':
    print(base_number)
else:
    print('%s-ci%s' % (base_number, split_build_number[-1]))
