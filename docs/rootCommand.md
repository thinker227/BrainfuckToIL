# Root command

Compiles a file to IL.

## Syntax
```ps1
bftoil <source> [<output>] [--output-kind <exe|dll>] [-m|--memory-size <size>] [--no-wrap]
bftoil run <source>
```

## Arguments
| Argument | Description | Default value |
| --- | --- | --- |
| `<source>` | The source file to compile. | required |
| `[<output>]` | The output destination for the compiled binary file. If the provided value is a directory then the output file will be located in the specified directory and use the file name of the source file. If not specified, the output file will be located in the same directory as the source file and use the file name of the source file. | null |

## Options
| Option | Description | Default value |
| --- | --- | --- |
| `-o\|--output <exe\|dll>` | Whether to output an exe or DLL file. | `exe` |
| `-m\|--memory-size <size>` | The size of the memory in the amount of cells long it is. | `30000` |
| `--no-wrap` | Whether to disable memory wrapping, i.e. that memory below cell 0 wraps around to the maximum cell and that memory above the maximum cell wraps around to cell 0. | `false` |

## Global options

| Option | Description |
| --- | --- |
| `--plain` | Disables color output from the compiler CLI. |
| `-?\|-h\|--help\|` | Shows help and usage information. |
| `--version` | Shows version information. |

## Subcommands

| Command | Description |
| --- | --- |
| [`run <source>`](./run.md) | Compiles and runs a file. |
 
