
# battlenet-lancache-prefill

[![](https://dcbadge.vercel.app/api/server/BKnBS4u?style=for-the-badge)](https://discord.com/invite/BKnBS4u)
[![view - Documentation](https://img.shields.io/badge/view-Documentation-green?style=for-the-badge)](https://tpill90.github.io/battlenet-lancache-prefill/)
[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Y8Y5DWGZN)

![GitHub all releases](https://img.shields.io/github/downloads/tpill90/battlenet-lancache-prefill/total?color=red&style=for-the-badge)
[![dockerhub](https://img.shields.io/docker/pulls/tpill90/battlenet-lancache-prefill?color=9af&style=for-the-badge)](https://hub.docker.com/r/tpill90/battlenet-lancache-prefill)


Automatically fills a [Lancache](https://lancache.net/) with games from Battle.net, so that subsequent downloads for the same content will be served from the Lancache, improving speeds and reducing load on your internet connection.

![Prefilling game](docs/img/HeaderImage.png)

# Features
* Downloads specific games by product ID
* High-performance!  Downloads are significantly faster than using Battle.net, and can easily reach 10gbit/s or more!
* Game install writes no data to disk, so there is no need to have enough free space available.  This also means no unnecessary wear-and-tear to SSDs!
* Multi-platform support (Windows, Linux, MacOS, Arm64)
* No installation required! A completely self-contained, portable application.

# Table of contents
- [Initial Setup](#initial-setup)
- [Getting Started](#getting-started)
- [Frequently Asked Questions](#frequently-asked-questions)
- [Detailed Command Usage](#detailed-command-usage)
- [Updating](#updating)
- [Need Help?](#need-help)

# Initial Setup

**BattleNetPrefill** is flexible and portable, and supports multiple platforms and configurations.  It can be run directly on the Lancache server itself,  or on your gaming machine as an alternative Battlenet client.  You should decide which one works better for your use case.

Detailed setup guides are available for the following platforms:

<a target="_blank" href="https://tpill90.github.io/battlenet-lancache-prefill/install-guides/Linux-Setup-Guide">
    <img src="/docs/img/badges/linux-setup-badge.svg" height="32px" title="Linux" alt="Linux" />
</a> &nbsp; 
<a target="_blank" href="https://tpill90.github.io/battlenet-lancache-prefill/install-guides/Docker-Setup-Guide">
    <img src="/docs/img/badges/docker-setup-badge.svg" height="32px" title="Docker" alt="Docker" />
</a> &nbsp; 
<a target="_blank" href="https://tpill90.github.io/battlenet-lancache-prefill/install-guides/Unraid-Setup-Guide">
    <img src="/docs/img/badges/unraid-setup-badge.svg" height="32px" title="unRAID" alt="unRAID" />
</a> &nbsp; 
<a target="_blank" href="https://tpill90.github.io/battlenet-lancache-prefill/install-guides/Windows-Setup-Guide">
    <img src="/docs/img/badges/windows-setup-badge.svg" height="32px" title="Windows" alt="Windows" />
</a>

</br>

# Getting Started

## Selecting what to prefill

> **Warning**
> This guide was written with Linux in mind.  If you are running **BattleNetPrefill** on Windows you will need to substitute `./BattleNetPrefill` with `.\BattleNetPrefill.exe` instead.

Prior to prefilling for the first time, you will have to decide which apps should be prefilled.  This will be done using an interactive menu, for selecting what to prefill from all of your currently owned apps. To display the interactive menu, run the following command
```powershell
./BattleNetPrefill select-apps
```

All of your currently owned apps will be now displayed for selection.  Navigating using the arrow keys, select any apps that you are interested in prefilling with **space**.  Once you are satisfied with your selections, save them with **enter**.

<img src="docs/img/Interactive-App-Selection.png" height="350" alt="Interactive app selection">

These selections will be saved permanently, and can be freely updated at any time by simply rerunning `select-apps` again at any time.

## Initial prefill

Now that a prefill app list has been created, we can now move onto our initial prefill run by using 
```powershell
./BattleNetPrefill prefill
```

The `prefill` command will automatically pickup the prefill app list, and begin downloading each app.  During the initial run, it is likely that the Lancache is empty, so download speeds should be expected to be around your internet line speed (in the below example, a 300mbit/s connection was used).  Once the prefill has completed, the Lancache should be fully ready to serve clients cached data.

<img src="docs/img/Initial-Prefill.png" width="720" alt="Initial Prefill">

## Updating previously prefilled games

Updating any previously prefilled games can be done by simply re-running the `prefill` command, with the same games specified as before.

**BattleNetPrefill** keeps track of which version of each game was previously prefilled, and will only re-download if there is a newer version of the game available.  
Any games that are currently up to date, will simply be skipped.

<img src="docs/img/Prefill-UpToDate.png" width="630" alt="Prefilled game up to date">


However, if there is a newer version of a game that is available, then **BattleNetPrefill** will re-download the game.  
Due to how Lancache works, this subsequent run should complete much faster than the initial prefill (example below used a 10gbit connection).
Any data that was previously downloaded, will be retrieved from the Lancache, while any new data from the update will be retrieved from the internet.

<img src="docs/img/Prefill-NewVersionAvailable.png" width="730" alt="Prefill run when game has an update">

# Frequently Asked Questions

### Can I run BattleNetPrefill on the Lancache server?

You certainly can!  All you need to do is download **BattleNetPrefill** onto the server, and run it as you regularly would!

If everything works as expected, you should see a message saying it found the server at `127.0.0.1`
<img src="docs/img/AutoDns-Server.png" width="830" alt="Prefill running on Lancache Server">

Running from a Docker container on the Lancache server is also supported!  You should instead see a message saying the server was found at `172.17.0.1`
<img src="docs/img/AutoDns-Docker.png" width="830" alt="Prefill running on Lancache Server in Docker">

Running on the Lancache server itself can give you some advantages over running **BattleNetPrefill** on a client machine, primarily the speed at which you can prefill apps.  
Since there is no network transfer happening, the `prefill` should only be limited by disk I/O and CPU throughput.  
For example, using a **SK hynix Gold P31 2TB NVME** and running `prefill --force` on previously cached game yields the following performance 
<img src="docs/img/AutoDns-ServerPerf.png" width="830" alt="Prefill running on Lancache Server in Docker">

# Detailed Command Usage

> **Note**
> Detailed command documentation has been moved to the wiki : [Detailed Command Usage](https://tpill90.github.io/battlenet-lancache-prefill/detailed-command-usage/Prefill/)

# Updating
**BattleNetPrefill** will automatically check for updates, and notify you when an update is available :

<img src="docs/img/UpdateAvailable.png" width="675" alt="Update available message">

To update:
1.  Download the latest version for your OS from the [Releases](https://github.com/tpill90/battlenet-lancache-prefill/releases) page.
2.  Unzip to the directory where **BattleNetPrefill** is currently installed, overwriting the previous executable.
3.  Thats it!  You're all up to date!

### Docker update:
sudo docker pull tpill90/battlenet-lancache-prefill:latest

# Need Help?
If you are running into any issues, feel free to open up a Github issue on this repository.

You can also find us at the [**LanCache.NET** Discord](https://discord.com/invite/BKnBS4u), in the `#battlenet-prefill` channel.

# Additional Documentation
*  Interested in compiling the project from source?  See [Development Setup Guide](https://tpill90.github.io/battlenet-lancache-prefill/dev-guides/Compiling-from-source/)

# External Docs
* https://wowdev.wiki/TACT
* https://github.com/d07RiV/blizzget/wiki

## Acknowledgements

- https://github.com/Marlamin/BuildBackup
- https://github.com/WoW-Tools/CASCExplorer
- https://github.com/d07RiV/blizzget
