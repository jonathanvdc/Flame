files = \
	Flame/*.cs \
	*.cs

libraries = \
	../Flame/bin/clr/Flame.dll \
	../packages/Loyc.Essentials.24.3.0/lib/net45/Loyc.Essentials.dll \
	../packages/Loyc.Collections.24.3.0/lib/net45/Loyc.Collections.dll \
	../packages/Loyc.Math.24.3.0/lib/net45/Loyc.Math.dll \
	../packages/Loyc.Syntax.24.3.0/lib/net45/Loyc.Syntax.dll \
	../packages/Loyc.Utilities.24.3.0/lib/net45/Loyc.Utilities.dll

ifeq ($(ASM_VERSION),)
	ASM_VERSION:=0.0.1.0
endif

common_options = -asm-name=UnitTests -asm-version=$(ASM_VERSION) -Wall -Wextra -fno-whole-program

.PHONY: all flo exe clean
all: flo exe

flo: bin/flo/UnitTests.flo
exe: bin/clr/UnitTests.exe

clean:
	rm -rf bin/
	rm -rf obj/

include ../flame-make-scripts/use-ecsc.mk

bin/flo/UnitTests.flo: $(files) $(libraries) | ecsc
	$(ECSC) $(files) -platform ir -runtime clr -libs $(libraries) -o $@ $(common_options)

bin/clr/UnitTests.exe: $(files) $(libraries) | ecsc
	$(ECSC) $(files) -platform clr -libs $(libraries) -o $@ $(common_options)
