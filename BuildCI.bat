
%1 Pixie/Pixie.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Pixie/Pixie.Xml.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Flame/Flame.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Flame.Compiler/Flame.Compiler.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Flame.Optimization/Flame.Optimization.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Flame.Syntax/Flame.Syntax.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Flame.Markdown/Flame.Markdown.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
%1 Flame.DSharp/Flame.DSharp.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug
cd ./Flame.Cecil/
msbuild /p:Configuration=Release Flame.Cecil.mono.sln
cd ..
