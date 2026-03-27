#!/usr/bin/env bash

# This script tests if feeding a working C# file to ilopt either
# changes its semantics or makes it break.
#
# Use it like so:
#
#     creduce --not-c test.sh main.cs
#

set -euo pipefail

workdir="$(mktemp -d)"
cleanup() {
  rm -rf "$workdir"
}
trap cleanup EXIT

cp main.cs "$workdir/main.cs"
cat > "$workdir/main.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <UseAppHost>false</UseAppHost>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>disable</Nullable>
    <Optimize>true</Optimize>
  </PropertyGroup>
</Project>
EOF

dotnet build "$workdir/main.csproj" -c Release /nologo /verbosity:quiet && \
dotnet exec "$workdir/bin/Release/net10.0/main.dll" > normal-output.txt && \
$(dirname "$0")/../../src/ILOpt/bin/Release/net10.0/ilopt "$workdir/bin/Release/net10.0/main.dll" -o "$workdir/bin/Release/net10.0/main.opt.dll" && \
(! dotnet exec "$workdir/bin/Release/net10.0/main.opt.dll" > opt-output.txt || \
 ! diff normal-output.txt opt-output.txt)
