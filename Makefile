.PHONY: release debug all nuget clean test
release:
	msbuild /p:Configuration=Release /verbosity:quiet /nologo Flame.sln

debug:
	msbuild /p:Configuration=Debug /verbosity:quiet /nologo Flame.sln

all: debug release

nuget:
	nuget restore Flame.sln

clean:
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test: debug
	mono ./UnitTests/bin/Debug/UnitTests.exe 1234
