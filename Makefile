SHELL := /bin/bash

.PHONY: release debug all dsl nuget clean test
release: dsl
	msbuild /p:Configuration=Release /verbosity:quiet /nologo Flame.sln

debug: dsl
	msbuild /p:Configuration=Debug /verbosity:quiet /nologo Flame.sln

all: debug release

MACROS_DLL = FlameMacros/bin/Release/FlameMacros.dll
MACROS_CS_FILES = $(shell find FlameMacros -name '*.cs')
RUN_EXE ?= mono

%.out.cs: %.ecs $(MACROS_DLL)
	$(RUN_EXE) FlameMacros/bin/Release/LeMP.exe --macros $(MACROS_DLL) --nologo --outext=.out.cs $<

$(MACROS_DLL): $(MACROS_CS_FILES)
	msbuild /p:Configuration=Release /verbosity:quiet /nologo FlameMacros/FlameMacros.csproj

dsl: Flame.Compiler/Transforms/InstructionSimplification.out.cs

nuget:
	nuget restore Flame.sln

clean:
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test: debug
	$(RUN_EXE) ./UnitTests/bin/Debug/UnitTests.exe 123456
