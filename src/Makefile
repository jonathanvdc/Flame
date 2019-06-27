SHELL := /bin/bash
LLVM_PATH ?= $(shell llvm-config --libdir)

.PHONY: release debug all dsl nuget clean test
release: dsl
	msbuild /p:Configuration=Release /verbosity:quiet /nologo Flame.sln

debug: dsl
	msbuild /p:Configuration=Debug /verbosity:quiet /nologo Flame.sln

all: debug release

LEMP_EXE = FlameMacros/bin/Release/net47/LeMP.exe
MACROS_DLL = FlameMacros/bin/Release/net47/FlameMacros.dll
MACROS_CS_FILES = $(shell find FlameMacros -name '*.cs')
RUN_EXE ?= mono

%.out.cs: %.ecs $(MACROS_DLL)
	$(RUN_EXE) $(LEMP_EXE) --macros $(MACROS_DLL) --nologo --outext=.out.cs $<

$(MACROS_DLL): $(MACROS_CS_FILES)
	msbuild /p:Configuration=Release /verbosity:quiet /nologo FlameMacros/FlameMacros.csproj

dsl: Flame.Compiler/Transforms/InstructionSimplification.out.cs \
	Flame.Compiler/Transforms/FuseMemoryAccesses.out.cs

nuget:
	nuget restore Flame.sln

clean:
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test: debug
	$(RUN_EXE) ./UnitTests/bin/Debug/net47/UnitTests.exe portable

test-llvm: debug
	LD_LIBRARY_PATH="$(LLVM_PATH):${LD_LIBRARY_PATH}" $(RUN_EXE) ./UnitTests/bin/Debug/net47/UnitTests.exe 7
