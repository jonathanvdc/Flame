
SET dsc=%1
SHIFT

%dsc% Pixie/Pixie.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Pixie/Pixie.Xml.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Flame/Flame.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Flame.Compiler/Flame.Compiler.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Flame.Optimization/Flame.Optimization.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Flame.Syntax/Flame.Syntax.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Flame.Markdown/Flame.Markdown.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
%dsc% Flame.DSharp/Flame.DSharp.dsc.dsproj -Wall -Wextra -pedantic -O2 -time -g --debug -repeat-command %*
cd ./Flame.Cecil/
msbuild /p:Configuration=Release Flame.Cecil.mono.sln
cd ..
