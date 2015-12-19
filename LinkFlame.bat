dsc Pixie\Pixie.dsproj -time -platform ir -runtime clr -fgenerate-static false
dsc Pixie\Pixie.Xml.dsproj -time -platform ir -runtime clr -libs Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame\Flame.dsc.dsproj -time -platform ir -runtime clr -libs Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame.Compiler\Flame.Compiler.dsc.dsproj -time -platform ir -runtime clr -libs Flame\bin\Flame.flo Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame.Optimization\Flame.Optimization.dsproj -time -platform ir -runtime clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame.Syntax\Flame.Syntax.dsc.dsproj -time -platform ir -runtime clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame.Markdown\Flame.Markdown.dsproj -time -platform ir -runtime clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Flame.Syntax\bin\Flame.Syntax.flo Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame.DSharp\Flame.DSharp.dsc.dsproj -time -platform ir -runtime clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Flame.Syntax\bin\Flame.Syntax.flo Pixie\bin\Pixie.flo -fgenerate-static false
dsc Flame.DSharp\bin\Flame.DSharp.flo Flame.Syntax\bin\Flame.Syntax.flo Flame.Markdown\bin\Flame.Markdown.flo Flame.Optimization\bin\Flame.Optimization.flo Flame.Compiler\bin\Flame.Compiler.flo Flame\bin\Flame.flo Pixie\bin\Pixie.Xml.flo Pixie\bin\Pixie.flo -time -platform clr -fgenerate-static -o Flame.DSharp\bin\Flame.DSharp_Static.dll
