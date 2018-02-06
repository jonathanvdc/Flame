exe:
	make -C Flame dll
	make -C Flame.Compiler dll
	make -C UnitTests exe

all:
	make -C Flame all
	make -C Flame.Compiler all
	make -C UnitTests all

dll:
	make -C Flame dll
	make -C Flame.Compiler dll

flo:
	make -C Flame flo
	make -C Flame.Compiler flo
	make -C UnitTests flo

nuget:
	nuget restore Flame.sln

clean: clean-ecsc
	make -C Flame clean
	make -C Flame.Compiler clean
	make -C UnitTests clean

test: exe
	mono ./UnitTests/bin/clr/UnitTests.exe 1

include flame-make-scripts/use-ecsc.mk
