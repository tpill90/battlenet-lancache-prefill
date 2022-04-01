
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


# Other Docs
* [Development Configuration](/docs/Development.md)

# External Docs
* https://wowdev.wiki/TACT
* https://github.com/d07RiV/blizzget/wiki

## Acknowledgements

- https://github.com/Marlamin/BuildBackup
- https://github.com/WoW-Tools/CASCExplorer
- https://github.com/d07RiV/blizzget