set -euo pipefail
repository="$1"
cache="$repository/artifacts/threadless-toolchain"
workspace="$HOME/.cache/touhou-threadless-b94985982"
mkdir -p "$workspace/engine" "$workspace/emsdk" "$workspace/dotnet" "$workspace/python"
if [ ! -f "$workspace/engine/SConstruct" ]; then tar -xzf "$cache/engine.tar.gz" --strip-components=1 -C "$workspace/engine"; fi
if [ ! -f "$workspace/emsdk/emsdk" ]; then tar -xzf "$cache/emsdk.tar.gz" --strip-components=1 -C "$workspace/emsdk"; fi
if [ ! -x "$workspace/dotnet/dotnet" ]; then tar -xzf "$cache/dotnet.tar.gz" -C "$workspace/dotnet"; fi
python3 -m zipfile -e "$cache/scons.whl" "$workspace/python"
export DOTNET_ROOT="$workspace/dotnet"
export DOTNET_CLI_HOME="$workspace/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_MULTILEVEL_LOOKUP=0
export NUGET_PACKAGES="$workspace/nuget"
export PYTHONPATH="$workspace/python"
export PATH="$DOTNET_ROOT:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
export EMSDK_QUIET=1
export DOTNET_CLI_UI_LANGUAGE=en
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
printf '%s\n' '{"sdk":{"version":"9.0.317","rollForward":"disable"}}' > "$workspace/global.json"
cd "$workspace"
dotnet --info
if [ ! -d "$DOTNET_ROOT/packs/Microsoft.NET.Runtime.WebAssembly.Sdk" ]; then dotnet workload install wasm-tools --skip-manifest-update; fi
cd "$workspace/emsdk"
./emsdk install 4.0.11
./emsdk activate 4.0.11
set +u
. ./emsdk_env.sh
set -u
emcc --version
cp -a "$cache/glue/." "$workspace/engine/modules/mono/glue/"
python3 "$repository/tools/threadless/prepare_source.py" "$workspace/engine"
cd "$workspace/engine"
python3 modules/mono/build_scripts/build_assemblies.py --godot-output-dir=bin --push-nupkgs-local bin/nuget
mkdir -p "$workspace/signatures"
cp "$repository/tools/threadless/signatures/Signatures.csproj" "$repository/tools/threadless/signatures/Program.cs" "$workspace/signatures/"
dotnet run --project "$workspace/signatures/Signatures.csproj" --configuration Release -- "$workspace/engine/bin/GodotSharp/Api/Release/GodotSharp.dll" "$workspace/engine/modules/mono/runtime/GetRuntimePack/NativeSignatures.cs"
python3 -c 'import SCons.Script; SCons.Script.main()' platform=web target=template_release module_mono_enabled=yes threads=no optimize=speed lto=none linkflags=--emit-symbol-map -j12
mkdir -p "$cache/output"
cp -a bin/*.zip "$cache/output/"
cp -a bin/*.symbols "$cache/output/"
cp -a bin/nuget "$cache/output/"
printf 'THREADLESS_TEMPLATE_BUILD_PASS %s\n' "$cache/output"
