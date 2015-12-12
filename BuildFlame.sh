# This script builds the Flame core libraries with dsc, and then
# proceeds to call xbuild on the peripheral Flame libraries.

dsc Pixie/Pixie.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Pixie/Pixie.Xml.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Flame/Flame.dsc.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Flame.Compiler/Flame.Compiler.dsc.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Flame.Optimization/Flame.Optimization.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Flame.Syntax/Flame.Syntax.dsc.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Flame.Markdown/Flame.Markdown.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
dsc Flame.DSharp/Flame.DSharp.dsc.dsproj -Wall -Wextra -pedantic -O2 -repeat-command -time
cd ./Flame.Cecil/
xbuild /p:Configuration=Release Flame.Cecil.mono.sln
cd ..
