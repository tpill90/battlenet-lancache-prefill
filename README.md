
# BattleNet-lancache-autofill

## Description

- https://github.com/zeropingheroes/lancache-autofill
- https://lancache.net/

# Development Pre-reqs

Only the .NET 5 SDK is required to compile the project.  This can be installed through one of the following methods

## Using Chocolatey
```powershell
choco install dotnet-5.0-sdk
# Needs to be removed, in order to resolve issue with Nuget being preconfigured wrong.  Will 
# auto-regenerate on first run.
Remove-Item "C:\Users\$Env:USERNAME\AppData\Roaming\NuGet\nuget.config"
```

## Manually
The latest .NET 5.0 SDK can be found here : [.NET 5.0 SDK - Windows x64 Installer]( https://download.visualstudio.microsoft.com/download/pr/5303da13-69f7-407a-955a-788ec4ee269c/dc803f35ea6e4d831c849586a842b912/dotnet-sdk-5.0.403-win-x64.exe)

# Compiling

The project can be compiled by running the following in the repository root (the directory with the .sln file).  This will generate an .exe that can be run locally.  Subsequent `dotnet build` commands will perform incremental compilation.

```powershell
dotnet build
```

# Running the project

Typically, for development you will want to run the project in `Debug` mode.  This mode will run dramatically slower than `Release`, however it will leave useful debugging information in the compiled assembly.  Running the following will detect and changes, and both `build` and `run` the project :
```powershell
dotnet run --project .\BuildBackup\BuildBackup.csproj
```

Alternatively, to run the project at full speed with all compilation optimizations enabled, add the additional `--configuration Release` flag:
```powershell
dotnet run --project .\BuildBackup\BuildBackup.csproj --configuration Release
```

# Executing Unit Tests

To compile and run all tests in the entire repo, run the following command:
```powershell
dotnet test
```

## Supported products
Basic data for all [product codes](https://wowdev.wiki/CASC#NGDP_Program_Codes) is supported (when available on CDN), but BuildBackup currently supports full data backups for the following applications:
- World of Warcraft
- Battle.net Agent
- Battle.net App

## Acknowledgements

- https://github.com/Marlamin/BuildBackup
- https://github.com/WoW-Tools/CASCExplorer
- https://github.com/d07RiV/blizzget