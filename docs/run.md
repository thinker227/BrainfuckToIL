# `run`

Compiles and runs a file.

## Syntax
```ps1
bftoil run <source> [--input <input>] [-m|--memory-size <size>] [--no-wrap]
```

## Arguments
| Argument | Description | Default value |
| --- | --- | --- |
| `<source>` | The source file to compile and run. |

## Options
| Option | Description | Default value |
| --- | --- | --- |
| `--input <input>` | If specified, the program will read from this value when encountering a , instruction rather than using input from the console. If a , instruction is encountered after the entire input has already been read, 0 will always be returned. | null |
| `-m\|--memory-size <value>` | The size of the memory in the amount of cells long it is. | `30000` |
| `--no-wrap` | Whether to disable memory wrapping, i.e. that memory below cell 0 wraps around to the maximum cell and that memory above the maximum cell wraps around to cell 0. | `false` |
