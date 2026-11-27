#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"
: "${CELESTE_DIR:?Set CELESTE_DIR to the path of your Everest installation.}"
tag_dotnet=${TAG_DOTNET:-dotnet}
version=$(awk '$1 == "Version:" { print $2; exit }' everest.yaml)

"$tag_dotnet" build src/Client/CelesteTag.csproj -c Release -o dist/client
"$tag_dotnet" build src/Host/CelesteTag.Host.csproj -c Release -o dist/host
cp everest.yaml dist/client/

cd dist/client
zip -q "../CelesteTag-$version.zip" everest.yaml CelesteTag.dll
cd ../host
zip -q "../CelesteTag-Host-$version.zip" \
    CelesteTag.Host.dll CelesteTag.Host.deps.json CelesteTag.Host.runtimeconfig.json
