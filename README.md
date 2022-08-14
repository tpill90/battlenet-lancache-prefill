
# battlenet-lancache-prefill

[![](https://dcbadge.vercel.app/api/server/BKnBS4u?style=flat-square)](https://discord.com/invite/BKnBS4u)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=tpill90_Battlenet-lancache-prefill&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=tpill90_Battlenet-lancache-prefill)

Automatically fills a [Lancache](https://lancache.net/) with games from Battle.net, so that subsequent downloads for the same content will be served from the Lancache, improving speeds and reducing load on your internet connection.

![Prefilling game](docs/img/HeaderImage.png)

Inspired by the [lancache-autofill](https://github.com/zeropingheroes/lancache-autofill) project for Steam games.

---

## Features
* Downloads specific games by product ID
* High-performance!  Downloads are significantly faster than using Battle.net, and can easily reach 10gbit/s or more!
* Game install writes no data to disk, so there is no need to have enough free space available.  This also means no unnecessary wear-and-tear to SSDs!
* Multi-platform support (Windows, Linux, MacOS, Arm64)
* No installation required! A completely self-contained, portable application.

---

## Initial Setup
1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/battlenet-lancache-prefill/releases) page.
2.  Unzip to a directory of your choice
3.  (**Linux / OSX Only**)  Give the downloaded executable permissions to be run with `chmod +x .\BattleNetPrefill`
4.  (**Windows Only - Optional**)  Configure your terminal to use Unicode, for much nicer looking UI output.
    - <img src="docs/img/ConsoleWithUtf8.png" width="730" alt="Initial Prefill">
    - As the default console in Windows does not support UTF8, Windows Terminal should be installed from the [App Store](https://apps.microsoft.com/store/detail/windows-terminal/9N0DX20HK701), or [Chocolatey](https://community.chocolatey.org/packages/microsoft-windows-terminal).
    - Unicode on Windows is not enabled by default, however running the following will enable it if it hasn't already been enabled.
    - `if(!(Test-Path $profile) -or !(gc $profile).Contains("OutputEncoding")) { ac $profile "[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()";  & $profile; }`

---

## Getting Started

### Selecting what to prefill

Prior to prefilling for the first time, you will have to decide which games should be prefilled.  
A table of all currently downloadable games can be listed with the following command
```powershell
.\BattleNetPrefill.exe list-products
```
<img src="docs/img/ListProducts.png" width="630" alt="List Products">

This table will show a list of available games, and their corresponding **product code**.  
These product codes will be used in subsequent commands, in order to specify which games to prefill.

### Initial prefill

Now that we've decided on some games that we'd like to prefill, we can move onto running the prefill.

One or more games can be downloaded by specifying as many product codes as desired, in this example we will be prefilling 3 total games
```powershell
.\BattleNetPrefill.exe prefill --products s1 d3 zeus
```

Alternatively, optional flags can be used to bulk preload products, without having to specify each product code individually.  This can be useful when you are interested
in installing most of the available games, as specifiying the individual product codes is not required.
```powershell
.\BattleNetPrefill.exe prefill --all
.\BattleNetPrefill.exe prefill --blizzard 
.\BattleNetPrefill.exe prefill --activision 
```

During this initial run, it is likely that the Lancache is empty, 
so download speeds should be expected to be around your internet line speed (in the below example, a 300megabit connection was used).  
Once the prefill has completed, the Lancache should be fully ready to serve clients cached data.

<img src="docs/img/Initial-Prefill.png" width="730" alt="Initial Prefill">

### Updating previously prefilled games

Updating any previously prefilled games can be done by simply re-running the `prefill` command, with the same games specified as before.

**BattleNetPrefill** keeps track of which version of each game was previously prefilled, and will only re-download if there is a newer version of the game available.  
Any games that are currently up to date, will simply be skipped.

<img src="docs/img/Prefill-UpToDate.png" width="630" alt="Prefilled game up to date">


However, if there is a newer version of a game that is available, then **BattleNetPrefill** will re-download the game.  
Due to how Lancache works, this subsequent run should complete much faster than the initial prefill (example below used a 10gbit connection).
Any data that was previously downloaded, will be retrieved from the Lancache, while any new data from the update will be retrieved from the internet.

<img src="docs/img/Prefill-NewVersionAvailable.png" width="730" alt="Prefill run when game has an update">

## Frequently Asked Questions

### Can I run BattleNetPrefill on the Lancache server?

You certainly can!  All you need to do is download **BattleNetPrefill** onto the server, and run it as you reguarly would!

If everything works as expected, you should see a message saying it found the server at `127.0.0.1`
<img src="docs/img/AutoDns-Server.png" width="830" alt="Prefill running on Lancache Server">

Running from a Docker container on the Lancache server is also supported!  You should instead see a message saying the server was found at `172.17.0.1`
<img src="docs/img/AutoDns-Docker.png" width="830" alt="Prefill running on Lancache Server in Docker">

Running on the Lancache server itself can give you some advantages over running **BattleNetPrefill** on a client machine, primarily the speed at which you can prefill apps.  
Since there is no network transfer happening, the `prefill` should only be limited by disk I/O and CPU throughput.  
For example, using a **SK hynix Gold P31 2TB NVME** and running `prefill --force` on previously cached game yields the following performance 
<img src="docs/img/AutoDns-ServerPerf.png" width="830" alt="Prefill running on Lancache Server in Docker">

# Detailed Usage

## list-products
Displays a table of all currently supported Activision and Blizzard games.  Only currently supports retail products, and does not include any PTR or beta products. 

These product IDs can then be used with the `prefill` command to specify which games to be prefilled.

## prefill
Fills a Lancache by downloading the exact same files from Blizzard's CDN as the official Battle.Net client.  Expected initial download speeds should be the speed of your internet connection.

Subsequent runs of this command should be hitting the Lancache, and as such should be dramatically faster than the initial run.  

### -p|--products
If a list of products is supplied, only these products will be downloaded.  This parameter is ideally used when only interested in a small number of games.

### --all, --activision, --blizzard
Downloads multiple products, useful for prefilling a completely empty cache.  Can be combined with `--products`.

### --nocache
By default, **BattleNetPrefill** will cache copies of certain files on disk, in order to dramatically speed up future runs (in some cases 3X faster).  These cache files will be stored in the `/cache` directory in the same directory as **BattleNetPrefill**.
However, in some scenarios this disk cache can potentially take up a non-trivial amount of storage (~1gb), which may not be ideal for all use cases.

By running with the additional flag `--nocache`, **BattleNetPrefill** will no longer cache any files locally, at the expense of slower runtime.

### -f|--force
By default, **BattleNetPrefill** will keep track of the most recently prefilled product, and will only attempt to prefill if there it determines there a newer version available for download.  This default behavior will work best for most use cases, as no time will be wasted re-downloading files that have been previously prefilled.

Running with the flag `--force` will override this behavior, and instead will always run the prefill, re-downloading all files for the specified product.  This flag may be useful for diagnostics, or benchmarking network performance.

# Need Help?
If you are running into any issues, feel free to open up a Github issue on this repository.

You can also find us at the [**LanCache.NET** Discord](https://discord.com/invite/BKnBS4u), in the `#battlenet-prefill` channel.

# Additional Documentation
* [Development Configuration](/docs/Development.md)

# External Docs
* https://wowdev.wiki/TACT
* https://github.com/d07RiV/blizzget/wiki

## Acknowledgements

- https://github.com/Marlamin/BuildBackup
- https://github.com/WoW-Tools/CASCExplorer
- https://github.com/d07RiV/blizzget