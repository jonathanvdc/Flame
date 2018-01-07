exe:
	make -C Flame dll
	make -C UnitTests exe

all:
	make -C Flame all
	make -C UnitTests all

dll:
	make -C Flame dll

flo:
	make -C Flame flo
	make -C UnitTests flo

nuget:
	nuget restore Flame.sln

clean: clean-ecsc
	make -C Flame clean

test: exe
	./UnitTests/bin/clr/UnitTests.exe 1

include flame-make-scripts/use-ecsc.mk
