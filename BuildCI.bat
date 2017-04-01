
SET dsc=%1
shift

set args=
:loop
      ::-------------------------- has argument ?
      if ["%~1"]==[""] (
        goto end
      )
      set args=%args% %1
      shift
      goto loop
:end

%dsc% Pixie/Pixie.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Pixie/Pixie.Xml.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Flame/Flame.dsc.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Flame.Compiler/Flame.Compiler.dsc.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Flame.Optimization/Flame.Optimization.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Flame.Syntax/Flame.Syntax.dsc.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Flame.Markdown/Flame.Markdown.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
%dsc% Flame.DSharp/Flame.DSharp.dsc.dsproj -Wall -Wextra -pedantic -time -g --debug -repeat-command %args%
cd ./Flame.Cecil/
msbuild /p:Configuration=Release /verbosity:minimal Flame.Cecil.sln
cd ..
