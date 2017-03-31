#!/bin/bash
shopt -s expand_aliases

alias dsc="mono $1"

dsc Pixie/Pixie.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Pixie/Pixie.Xml.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Flame/Flame.dsc.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Flame.Compiler/Flame.Compiler.dsc.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Flame.Optimization/Flame.Optimization.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Flame.Syntax/Flame.Syntax.dsc.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Flame.Markdown/Flame.Markdown.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
dsc Flame.DSharp/Flame.DSharp.dsc.dsproj -Wall -Wextra -pedantic $2 -repeat-command -time -g --debug
cd ./Flame.Cecil/
xbuild /p:Configuration=Release Flame.Cecil.sln
cd ..
