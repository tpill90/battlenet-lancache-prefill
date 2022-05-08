
# Battlenet-lancache-prefill

Automatically fills a [lancache](https://lancache.net/) with games from Battlenet, so that subsequent downloads for the same content will be served from the lancache, improving speeds and reducing load on your internet connection.

Inspired by the [lancache-autofill](https://github.com/zeropingheroes/lancache-autofill) project for Steam games.

# Features
* Downloads specific games by product ID
* Incredibly fast, can easily saturate a 10gbe line!
* Game install writes no data to disk,  no unnecessary wear-and-tear to SSDs!
* Multi-platform support (Windows, Linux, MacOS)
* Self-contained application, no installation required!

# Screenshots
![Prefilling game](docs/screenshot1-prefill.png)

# Installation
1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/Battlenet-lancache-prefill/releases) page.
2.  Unzip to a directory of your choice
3.  (Windows Only)  Configure your terminal to use Unicode, for much nicer looking UI output.
    - Unicode on Windows is not enabled by default, however adding the following to your Powershell `profile.ps1` will enable it.
    - `[console]::InputEncoding = [console]::OutputEncoding = [System.Text.UTF8Encoding]::new()
`
    - If you do not already have a Powershell profile created, follow this step-by-step guide https://lazyadmin.nl/powershell/powershell-profile/
# Basic Usage

A single game can be downloaded by specifying a single product code
```
.\BattleNetPrefill.exe prefill --products s1
```

Multiple games can be downloaded by specifying as many product codes as desired
```
.\BattleNetPrefill.exe prefill --products s1 d3 zeus
```

Optional flags can be used to bulk preload products, without having to specify each product code individually
```
.\BattleNetPrefill.exe prefill --all
.\BattleNetPrefill.exe prefill --blizzard 
.\BattleNetPrefill.exe prefill --activision 
```

The list of currently supported products to download can be displayed using the following
```
.\BattleNetPrefill.exe list-products
```

# Detailed Usage

## list-products
Displays a table of all currently supported Activision and Blizzard games.  Only currently supports retail products, and does not include any PTR or beta products.

## prefill
Fills a Lancache by downloading the exact same files from Blizzard's CDN as the official Battle.Net client.  Expected initial download speeds should be the speed of your internet connection.

Subsequent runs of this command should be hitting the Lancache, and as such should be dramatically faster than the initial run.  

### -p|--products
If a list of products is supplied, only these products will be downloaded.  This parameter is ideally used when only interested in a small number of games.

### --all, --activision, --blizzard
Downloads multiple products, useful for prefilling a completely empty cache.  Can be combined with `--products`.

### --nocache
By default, **BattleNetPrefill** will cache copies of certain files on disk, in order to dramatically speed up future runs (in some cases 3X faster).  
However, in some scenarios this disk cache can potentially take up a non-trivial amount of storage (~1gb), which may not be ideal for all use cases.

By running with the additional flag `--nocache`, **BattleNetPrefill** will no longer cache any files locally, at the expense of slower runtime.

### -f|--force
By default, **BattleNetPrefill** will keep track of the most recently prefilled product, and will only attempt to prefill if there it determines there a newer version available for download.  This default behavior will work best for most use cases, as no time will be wasted re-downloading files that have been previously prefilled.

Running with the flag `--force` will override this behavior, and instead will always run the prefill, re-downloading all files for the specified product.  This flag may be useful for diagnostics, or benchmarking network performance.

# Other Docs
* [Development Configuration](/docs/Development.md)

# External Docs
* https://wowdev.wiki/TACT
* https://github.com/d07RiV/blizzget/wiki

## Acknowledgements

- https://github.com/Marlamin/BuildBackup
- https://github.com/WoW-Tools/CASCExplorer
- https://github.com/d07RiV/blizzget