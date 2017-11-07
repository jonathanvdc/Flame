exe:
	make -C Flame dll

all:
	make -C Flame all

dll:
	make -C Flame dll

flo:
	make -C Flame flo

nuget:
	nuget restore Flame.sln

clean: clean-ecsc
	make -C Flame clean

include flame-make-scripts/use-ecsc.mk
