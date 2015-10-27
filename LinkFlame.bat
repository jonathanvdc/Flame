dsc Pixie\Pixie.dsproj -time -platform ir -indirect-platform clr
dsc Flame\Flame.dsc.dsproj -time -platform ir -indirect-platform clr -libs Pixie\bin\Pixie.flo
dsc Flame.Compiler\Flame.Compiler.dsc.dsproj -time -platform ir -indirect-platform clr -libs Flame\bin\Flame.flo Pixie\bin\Pixie.flo
dsc Flame.Optimization\Flame.Optimization.dsproj -time -platform ir -indirect-platform clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Pixie\bin\Pixie.flo
dsc Flame.Syntax\Flame.Syntax.dsc.dsproj -time -platform ir -indirect-platform clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Pixie\bin\Pixie.flo
dsc Flame.Markdown\Flame.Markdown.dsproj -time -platform ir -indirect-platform clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Flame.Syntax\bin\Flame.Syntax.flo Pixie\bin\Pixie.flo
dsc Flame.DSharp\Flame.DSharp.dsc.dsproj -time -platform ir -indirect-platform clr -libs Flame\bin\Flame.flo Flame.Compiler\bin\Flame.Compiler.flo Flame.Syntax\bin\Flame.Syntax.flo Pixie\bin\Pixie.flo
dsc Flame.DSharp\bin\Flame.DSharp.flo Flame.Syntax\bin\Flame.Syntax.flo Flame.Markdown\bin\Flame.Markdown.flo Flame.Optimization\bin\Flame.Optimization.flo Flame.Compiler\bin\Flame.Compiler.flo Flame\bin\Flame.flo Pixie\bin\Pixie.flo -time -platform clr -o Flame.DSharp_Static.dll
