SHELL := /bin/bash

.PHONY: release debug all dsl nuget clean test
release:
	$(MAKE) -C src release

debug:
	$(MAKE) -C src debug

all:
	$(MAKE) -C src all

dsl:
	$(MAKE) -C src dsl

nuget:
	$(MAKE) -C src nuget

clean:
	$(MAKE) -C src clean

test:
	$(MAKE) -C src test

test-llvm:
	$(MAKE) -C src test-llvm
