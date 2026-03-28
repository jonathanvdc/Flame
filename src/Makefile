SHELL := /bin/bash
LLVM_PATH ?= $(shell llvm-config --libdir)
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
LLVM_TEST_PUBLISH_DIR = UnitTests/bin/Debug/net10.0/$(DOTNET_RID)/publish

.PHONY: release debug all nuget clean test
release:
	dotnet build -c Release /verbosity:quiet Flame.sln

debug:
	dotnet build -c Debug /verbosity:quiet Flame.sln

all: debug release

RUN_EXE ?= dotnet

nuget:
	nuget restore Flame.sln

clean:
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test: debug
	$(RUN_EXE) ./UnitTests/bin/Debug/net10.0/UnitTests.dll portable

test-llvm:
	test -n "$(DOTNET_RID)"
	dotnet publish UnitTests/UnitTests.csproj -c Debug -r $(DOTNET_RID) --self-contained false /verbosity:quiet
	$(RUN_EXE) ./$(LLVM_TEST_PUBLISH_DIR)/UnitTests.dll 7 --clang-path clang
