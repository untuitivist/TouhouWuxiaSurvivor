import pathlib
import sys
import tarfile


def replace_checked(text, before, after):
    if text.count(before) != 1:
        raise SystemExit('Unexpected pinned source block: ' + before)
    return text.replace(before, after)


def main():
    source = pathlib.Path(sys.argv[1]).resolve()
    archive_path = pathlib.Path(__file__).resolve().parents[2] / 'artifacts/threadless-toolchain/engine.tar.gz'
    patches = {
        'SConstruct': [('        else:\n            env.AppendUnique(LINKFLAGS=["-s"])', '        elif not methods.using_emcc(env):\n            env.AppendUnique(LINKFLAGS=["-s"])')],
        'modules/mono/editor/Godot.NET.Sdk/Godot.NET.Sdk/Sdk/Browser.targets': [('<WasmEnableThreads>true</WasmEnableThreads>', '<WasmEnableThreads>false</WasmEnableThreads>')],
        'modules/mono/runtime/GetRuntimePack/GetRuntimePack.csproj': [
            ('<WasmEnableThreads>true</WasmEnableThreads>', '<WasmEnableThreads>false</WasmEnableThreads>'),
            ('<AssemblyName>GodotSharp</AssemblyName>', '<AssemblyName>GodotRuntimePack</AssemblyName>'),
            ('<TrimmerRootAssembly Include="$(TargetName)" />', '<TrimmerRootAssembly Include="$(TargetName)" />\n    <TrimmerRootAssembly Include="GodotSharp" />\n    <Compile Remove="ManagedCallbacks.cs" />\n    <Reference Include="GodotSharp">\n      <HintPath>../../../../bin/GodotSharp/Api/Release/GodotSharp.dll</HintPath>\n    </Reference>')
        ],
        'modules/mono/build_scripts/mono_configure.py': [
            ("        try:\n            shutil.rmtree(mono_runtime_copy_path)\n        except FileNotFoundError:\n            # It's fine if this directory doesn't exist.\n            pass\n", ''),
            ('shutil.copytree(mono_runtime_path, mono_runtime_copy_path)', 'shutil.copytree(mono_runtime_path, mono_runtime_copy_path, dirs_exist_ok=True)')
        ]
    }
    with tarfile.open(archive_path, 'r:gz') as archive:
        for relative, replacements in patches.items():
            members = [member for member in archive.getmembers() if member.isfile() and member.name.partition('/')[2] == relative]
            if len(members) != 1:
                raise SystemExit('Ambiguous pinned archive member: ' + relative)
            text = archive.extractfile(members[0]).read().decode('utf-8')
            for before, after in replacements:
                text = replace_checked(text, before, after)
            target = source / relative
            if not target.is_file():
                raise SystemExit('Missing extracted source file: ' + relative)
            if target.read_text(encoding='utf-8') != text:
                target.write_text(text, encoding='utf-8')
    print('THREADLESS_SOURCE_PREPARED', source)


if __name__ == '__main__':
    main()
