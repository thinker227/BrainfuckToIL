dotnet build src/BrainfuckToIL.CLI -c ToolDeploy
dotnet tool install --global --add-source src/BrainfuckToIL.CLI/bin/Package thinker227.BrainfuckToIL --prerelease
