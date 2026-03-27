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

.PHONY: release debug all dsl nuget clean test
release: dsl
	dotnet build -c Release /verbosity:quiet Flame.sln

debug: dsl
	dotnet build -c Debug /verbosity:quiet Flame.sln

all: debug release

LEMP_EXE = FlameMacros/bin/Release/net10.0/LeMP.dll
MACROS_DLL = FlameMacros/bin/Release/net10.0/FlameMacros.dll
MACROS_CS_FILES = $(shell find FlameMacros -name '*.cs')
RUN_EXE ?= dotnet

%.out.cs: %.ecs $(MACROS_DLL)
	$(RUN_EXE) $(LEMP_EXE) --macros $(MACROS_DLL) --nologo --outext=.out.cs $<

$(MACROS_DLL): $(MACROS_CS_FILES)
	dotnet build -c Release /verbosity:quiet FlameMacros/FlameMacros.csproj

dsl: Flame.Compiler/Transforms/InstructionSimplification.out.cs \
	Flame.Compiler/Transforms/FuseMemoryAccesses.out.cs

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
