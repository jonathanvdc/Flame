files = \
	Flow/*.cs \
	*.cs

libraries = \
	../Flame/bin/clr/Flame.dll \
	../packages/System.Collections.Immutable.1.4.0/lib/portable-net45+win8+wp8+wpa81/System.Collections.Immutable.dll

ifeq ($(ASM_VERSION),)
	ASM_VERSION:=0.0.1.0
endif

common_options = -asm-name=Flame.Compiler -asm-version=$(ASM_VERSION) -Wall -Wextra -Wno-unused-method

.PHONY: all flo dll clean
all: flo dll

flo: bin/flo/Flame.Compiler.flo
dll: bin/clr/Flame.Compiler.dll

clean:
	rm -rf bin/
	rm -rf obj/

include ../flame-make-scripts/use-ecsc.mk

bin/flo/Flame.Compiler.flo: $(files) $(libraries) | ecsc
	$(ECSC) $(files) -platform ir -runtime clr -libs $(libraries) -rt-libs System -o $@ $(common_options)

bin/clr/Flame.Compiler.dll: $(files) $(libraries) | ecsc
	$(ECSC) $(files) -platform clr -libs $(libraries) -rt-libs System -o $@ $(common_options)
