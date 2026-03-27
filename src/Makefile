SHELL := /bin/bash
LLVM_PATH ?= $(shell llvm-config --libdir)

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

test-llvm: debug
	LD_LIBRARY_PATH="$(LLVM_PATH):${LD_LIBRARY_PATH}" $(RUN_EXE) ./UnitTests/bin/Debug/net10.0/UnitTests.dll 7
