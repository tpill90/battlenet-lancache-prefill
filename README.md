# BuildBackup

## Description
BuildBackup was created to back up [CASC](https://wowdev.wiki/CASC) data from Blizzard's CDN. Blizzard often removes data for older builds from their CDN, making them unavailable for install. The goal is to have it back up all data needed to install a specific version for a specific application.

Data is currently backed up to [this archive](https://bnet.marlam.in). For more information on the archive or if you want to download large amounts of data from it, please contact me at marlamin@marlamin.com.

In addition to the backup functionality there are also several utilities implemented to dump information/extract data from the CASC filesystem.

## Supported products
Basic data for all [product codes](https://wowdev.wiki/CASC#NGDP_Program_Codes) is supported (when available on CDN), but BuildBackup currently supports full data backups for the following applications:
- World of Warcraft (wow, wowt, wow_beta)
- Overwatch (pro, prot, proc, **not prodev**)

## Thanks
- WoWDev wiki authors
- Blizzard
