### Subnautica Below Zero Multiplayer Mod - LAN / direct IP edition

Fork of the Subnautica Below Zero Multiplayer mod made for playing on a LAN with friends.

- No NetBird, no lobby server, no invite codes. The host runs the server inside the game and friends join by IP address.
- Host a world straight from the game menu (Multiplayer -> Host). The host's LAN IP is shown in the intro screen and under "Show Server IP" in the in-game menu.
- Join by typing `192.168.x.x` or `192.168.x.x:port`.
- Option to set the path to `SubnauticaZero.exe` (`GameExePath` in `Config.json`, used for the Windows Firewall rule; auto-detected if empty).
- Portable launcher (`SubnauticaBZ-Launcher.exe`): installs/repairs the mod, launches the game, hosts or joins straight from the launcher, and edits the settings. Nitrox-style, but the server still runs inside the host's game.
- Updated to work with the current Steam build of Subnautica: Below Zero (Oct 2025).

## Install (players)
Download `SubnauticaBZ-Multiplayer-LAN.zip` from the Releases page, unzip, run `SubnauticaBZ-Launcher.exe` (or `Install.bat`) and follow `README-LAN.txt`.
The installer patches *your own* copy of `Assembly-CSharp.dll` (a backup is kept) so no game files are redistributed.
Everyone must be on the same game version.

## Config
Run the game once and close it, then edit `Multiplayer/Game/Core/Config.json`:

| Option | Meaning |
| --- | --- |
| `PlayerName` | Your name in multiplayer (2-24 chars). Empty = Steam name. A set name replaces Steam for the player id too, so every player just needs a different name. |
| `GameExePath` | Path to `SubnauticaZero.exe` or its folder. Empty = auto-detect. |
| `ConfigureFirewall` | Ask once (UAC) to allow the game through Windows Firewall when hosting. |
| `HostOnPort` | UDP port the host listens on (default 7777). |
| `DefaultJoinPort` | Port used when joiners type an address without `:port` (default 7777). |
| `MaxPlayer` | Max players. |
| `ConnectionTimeout` | Connection timeout in seconds. |

## Build
See [Tools/BUILDING.md](Tools/BUILDING.md).

## Thanks
Thanks BOT Benson for creating this, and Detanup01 for the fork this one is based on.
