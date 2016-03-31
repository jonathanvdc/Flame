# This script compiles and then statically links the Flame core libraries.

# First build all libraries individually
dsc Pixie/Pixie.dsproj -repeat-command -time -platform ir \
    -runtime clr -fgenerate-static false $@
dsc Pixie/Pixie.Xml.dsproj -repeat-command -time -platform ir \
    -runtime clr -libs Pixie/bin/Pixie.flo -fgenerate-static false $@
dsc Flame/Flame.dsc.dsproj -repeat-command -time -platform ir \
    -runtime clr -libs Pixie/bin/Pixie.flo -fgenerate-static false $@
dsc Flame.Compiler/Flame.Compiler.dsc.dsproj -repeat-command -time -platform ir \
    -runtime clr -libs Flame/bin/Flame.flo Pixie/bin/Pixie.flo \
    -fgenerate-static false $@
dsc Flame.Optimization/Flame.Optimization.dsproj -repeat-command -time \
    -platform ir -runtime clr -libs Flame/bin/Flame.flo \
    Flame.Compiler/bin/Flame.Compiler.flo Pixie/bin/Pixie.flo \
    -fgenerate-static false $@
dsc Flame.Syntax/Flame.Syntax.dsc.dsproj -repeat-command -time -platform ir \
    -runtime clr -libs Flame/bin/Flame.flo \
    Flame.Compiler/bin/Flame.Compiler.flo Pixie/bin/Pixie.flo \
    -fgenerate-static false $@
dsc Flame.Markdown/Flame.Markdown.dsproj -repeat-command -time -platform ir \
    -runtime clr -libs Flame/bin/Flame.flo \
    Flame.Compiler/bin/Flame.Compiler.flo Flame.Syntax/bin/Flame.Syntax.flo \
    Pixie/bin/Pixie.flo -fgenerate-static false $@
dsc Flame.DSharp/Flame.DSharp.dsc.dsproj -repeat-command -time -platform ir \
    -runtime clr -libs Flame/bin/Flame.flo \
    Flame.Compiler/bin/Flame.Compiler.flo Flame.Syntax/bin/Flame.Syntax.flo \
    Pixie/bin/Pixie.flo -fgenerate-static false $@

# Now link the resulting IR assemblies together, and compile that
# down to a dll.
dsc Flame.DSharp/bin/Flame.DSharp.flo Flame.Syntax/bin/Flame.Syntax.flo \
    Flame.Markdown/bin/Flame.Markdown.flo Flame.Optimization/bin/Flame.Optimization.flo \
    Flame.Compiler/bin/Flame.Compiler.flo Flame/bin/Flame.flo \
    Pixie/bin/Pixie.Xml.flo Pixie/bin/Pixie.flo \
    -repeat-command -time -platform clr -fgenerate-static $@ \
    -o Flame.DSharp/bin/Flame.DSharp_Static.dll
