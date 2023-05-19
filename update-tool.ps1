dotnet build src/BrainfuckToIL.CLI -c ToolDeploy
dotnet tool update --global --add-source src/BrainfuckToIL.CLI/bin/Package thinker227.BrainfuckToIL --prerelease
