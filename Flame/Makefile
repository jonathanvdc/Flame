files = \
	*.cs

ifeq ($(ASM_VERSION),)
	ASM_VERSION:=0.0.1.0
endif

common_options = -asm-name=Flame -asm-version=$(ASM_VERSION) -Wall -Wextra

.PHONY: all flo dll clean
all: flo dll

flo: bin/flo/Flame.flo
dll: bin/clr/Flame.dll

clean:
	rm -rf bin/
	rm -rf obj/

include ../flame-make-scripts/use-ecsc.mk

bin/flo/Flame.flo: $(files) $(libraries) | ecsc
	$(ECSC) $(files) -platform ir -runtime clr -o $@ $(common_options)

bin/clr/Flame.dll: $(files) $(libraries) | ecsc
	$(ECSC) $(files) -platform clr -o $@ $(common_options)
