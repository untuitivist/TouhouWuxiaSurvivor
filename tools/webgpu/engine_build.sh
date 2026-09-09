set -euo pipefail
repository="$1"
stage="$2"
original="$HOME/.cache/touhou-threadless-b94985982"
workspace="$HOME/.cache/touhou-webgpu-mono-f329e39c-b9498598"
cache="$repository/artifacts/webgpu-engine-f329e39c"
printf 'ENGINE_ENVIRONMENT %s\n' "$workspace"
df -h "$HOME"
if [ ! -x "$original/dotnet/dotnet" ] || [ ! -f "$original/engine/SConstruct" ]; then echo 'Existing Mono compiler/toolchain unavailable'; exit 2; fi
python3 "$repository/tools/webgpu/engine_prepare.py" "$repository" "$workspace"
if [ "$stage" = 'prepare' ]; then exit 0; fi
if ! command -v c++ >/dev/null; then echo 'Native C++20 compiler required for Tint; use -InstallHostCompiler once.'; exit 2; fi
c++ --version
if [ ! -f "$workspace/dependency-caches-copied" ]; then
    mkdir -p "$workspace/emscripten-cache" "$workspace/nuget"
    cp -a "$original/emsdk/upstream/emscripten/cache/." "$workspace/emscripten-cache/"
    cp -a "$original/nuget/." "$workspace/nuget/"
    printf '%s\n' 'copied without links to the original package files' > "$workspace/dependency-caches-copied"
fi
export DOTNET_ROOT="$original/dotnet"
export DOTNET_CLI_HOME="$workspace/dotnet-home"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_MULTILEVEL_LOOKUP=0
export DOTNET_CLI_UI_LANGUAGE=en
export DOTNET_GENERATE_ASPNET_CERTIFICATE=false
export DOTNET_CLI_WORKLOAD_UPDATE_NOTIFY_DISABLE=true
export NUGET_PACKAGES="$workspace/nuget"
export EM_CACHE="$workspace/emscripten-cache"
export PYTHONUNBUFFERED=1
export PYTHONPATH="$original/python"
export PATH="$DOTNET_ROOT:/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin"
export EMSDK_QUIET=1
printf '%s\n' '{"sdk":{"version":"9.0.317","rollForward":"disable"}}' > "$workspace/global.json"
set +u
. "$original/emsdk/emsdk_env.sh"
set -u
emcc --version
dotnet --version
em++ --use-port=emdawnwebgpu -c "$repository/tools/webgpu/port_probe.cpp" -o "$workspace/port-probe.o"
cd "$workspace/engine"
python3 modules/mono/build_scripts/build_assemblies.py --godot-output-dir=bin --push-nupkgs-local bin/nuget
mkdir -p "$workspace/signatures"
cp "$repository/tools/threadless/signatures/Signatures.csproj" "$repository/tools/threadless/signatures/Program.cs" "$workspace/signatures/"
dotnet run --project "$workspace/signatures/Signatures.csproj" --configuration Release -- "$workspace/engine/bin/GodotSharp/Api/Release/GodotSharp.dll" "$workspace/engine/modules/mono/runtime/GetRuntimePack/NativeSignatures.cs"
python3 -c 'import SCons.Script; SCons.Script.main()' platform=web target=template_release module_mono_enabled=yes webgpu=yes opengl3=yes threads=no optimize=speed lto=none linkflags=--emit-symbol-map -j12
output="$cache/output/$(date -u +%Y%m%dT%H%M%SZ)"
mkdir -p "$output"
cp -a bin/*.zip "$output/"
cp -a bin/*.symbols "$output/"
cp -a bin/nuget "$output/"
printf '%s\n' "$output" > "$cache/latest-output.txt"
printf 'INTEGRATED_MONO_WEBGPU_BUILD_PASS %s\n' "$output"
