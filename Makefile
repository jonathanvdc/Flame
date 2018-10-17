.PHONY: release debug all macros dsl nuget clean test
release:
	msbuild /p:Configuration=Release /verbosity:quiet /nologo Flame.sln

debug:
	msbuild /p:Configuration=Debug /verbosity:quiet /nologo Flame.sln

all: debug release

%.out.cs: %.ecs
	FlameMacros/bin/Release/LeMP.exe --macros FlameMacros/bin/Release/FlameMacros.dll --outext=.out.cs $<

macros:
	msbuild /p:Configuration=Release /verbosity:quiet /nologo FlameMacros/FlameMacros.csproj

dsl: macros Flame.Compiler/Transforms/InstructionSimplification.out.cs

nuget:
	nuget restore Flame.sln

clean:
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test: debug
	mono ./UnitTests/bin/Debug/UnitTests.exe 123456
