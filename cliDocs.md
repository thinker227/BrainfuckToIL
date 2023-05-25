This file documents the command-line interface for the library.

# Global options

| Option | Description |
| --- | --- |
| `--version` | Displays the version of the tool currently in use. |
| `-?\|-h\|--help\|` | Displays help text for the tool. |
| `--plain` | Disables color output from the compiler CLI. |

# Commands

## `compile`

Compiles a file to IL.

### Syntax
```ps1
bftoil compile <source> [<output>] [-o|--output-kind <dll|exe>] [-m|--memory-size <value>] [--no-wrap] [--plain]
```

### Arguments
| Argument | Syntax | Description | Default value |
| --- | --- | --- | --- |
| `source` | `<source>` | The source file to compile. |  |
| `output` | `[<output>]` | The output destination for the compiled binary file. If the provided value is a directory then the output file will be located in the specified directory and use the file name of the source file. If not specified, the output file will be located in the same directory as the source file and use the file name of the source file. | null |

### Options
| Option | Syntax | Description | Default value |
| --- | --- | --- | --- |
| `-o\|--output-kind` | `-o\|--output <dll\|exe>` | Whether to output an exe or DLL file. | `exe` |
| `-m\|--memory-size` | `-m\|--memory-size <value>` | The size of the memory in the amount of cells long it is. | `30000` |
| `--no-wrap` | `--no-wrap` | Whether to disable memory wrapping, i.e. that memory below cell 0 wraps around to the maximum cell and that memory above the maximum cell wraps around to cell 0. | `false` |

## `run`

Compiles and runs a file.

### Syntax
```ps1
bftoil run <source> [-m|--memory-size <value>] [--no-wrap] [--plain]
```

### Arguments
| Argument | Syntax | Description | Default value |
| --- | --- | --- | --- |
| `source` | `<source>` | The source file to compile and run. |

### Options
| Option | Syntax | Description | Default value |
| --- | --- | --- | --- |
| `-m\|--memory-size` | `-m\|--memory-size <value>` | The size of the memory in the amount of cells long it is. | `30000` |
| `--no-wrap` | `--no-wrap` | Whether to disable memory wrapping, i.e. that memory below cell 0 wraps around to the maximum cell and that memory above the maximum cell wraps around to cell 0. | `false` |
