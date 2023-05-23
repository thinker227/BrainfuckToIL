# Brainfuck to IL

A simplistic [Brainfuck](https://esolangs.org/wiki/Brainfuck) to IL compiler.

# Installation

> **Note**
> The [.NET 7.0 SDK](https://dotnet.microsoft.com/en-us/) is required to install this tool.

## Build from source

Run the following commands in your favorite terminal:
```ps1
git clone https://github.com/thinker227/BrainfuckToIL.git/
cd BrainfuckToIL
./install-tool.ps1
```

## Install from Nuget

A Nuget package is currently not available.

# Usage

Compile a Brainfuck file to a .NET executable:
```ps1
bftoil compile foo.bf
```

You can alternatively specify whether to output a DLL or exe, of which exe is the default:
```ps1
# Output an exe
bftoil compile bar.bf -o exe

# Output a DLL
bftoil compile bar.bf -o dll
```

You can also explicitly specify the output file or directory:
```ps1
# Output to baz.exe
bftoil compile baz.bf baz.exe

# Output to a subdirectory
bftoil compile baz.bf output/
```

Run a Brainfuck file without having to compile to an executable file first:
```ps1
bftoil run qux.bf
```
