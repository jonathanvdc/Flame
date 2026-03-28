SHELL := /bin/bash
UNAME_S := $(shell uname -s)
UNAME_M := $(shell uname -m)
ifeq ($(UNAME_S),Linux)
ifeq ($(UNAME_M),aarch64)
DOTNET_RID ?= linux-arm64
else
DOTNET_RID ?= linux-x64
endif
endif
ifeq ($(UNAME_S),Darwin)
DOTNET_RID ?= osx-arm64
endif
ifeq ($(OS),Windows_NT)
ifeq ($(PROCESSOR_ARCHITECTURE),ARM64)
DOTNET_RID ?= win-arm64
else
DOTNET_RID ?= win-x64
endif
endif

.PHONY: release debug all nuget clean test test-llvm
release:
	dotnet build -c Release /verbosity:quiet Flame.sln

debug:
	dotnet build -c Debug /verbosity:quiet Flame.sln

all: debug release

nuget:
	dotnet restore Flame.sln

pack:
	dotnet pack Flame.sln -c Release

clean:
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test:
	dotnet test UnitTests/UnitTests.csproj -c Debug --filter "TestCategory!=LLVM"

test-llvm:
	test -n "$(DOTNET_RID)"
	dotnet test UnitTests/UnitTests.csproj -c Debug -r $(DOTNET_RID) --filter "TestCategory=LLVM"
