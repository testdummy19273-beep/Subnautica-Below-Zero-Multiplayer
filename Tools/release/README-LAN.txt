Subnautica Below Zero Multiplayer - LAN / direct IP edition
=========================================================

Everyone needs the SAME game version. This build was compiled against the Steam build of
SubnauticaZero_Data\Managed\Assembly-CSharp.dll dated 4 Oct 2025. If Steam updates the game,
run Install.bat again (and make sure all players are on the same version).

INSTALL (each player)
  1. Unzip this folder anywhere.
  2. Run Install.bat. If your game is not in the default Steam folder, either run it from a
     command prompt as   Install.bat "D:\Games\SubnauticaZero"   or type the folder when asked.
     It keeps a backup of the original game file (Assembly-CSharp.dll.original) and patches your
     own copy - no game files are redistributed in this zip.
  3. To remove it again, run Uninstall.bat.

HOST
  1. Start the game, Multiplayer -> Host, create a new world (or load a hosted save).
  2. The first time, Windows asks for administrator permission to open the firewall for the game.
     Say yes (it only adds an "allow SubnauticaZero.exe" rule). Set ConfigureFirewall to false in
     Config.json if you do not want that.
  3. In the intro screen, or in the in-game menu ("Show Server IP"), your LAN address is shown,
     e.g. 192.168.1.20:7777. Tell your friends that address.

JOIN
  Multiplayer -> Join, type the host's address (192.168.1.20 or 192.168.1.20:7777) and click Join.

OPTIONS  (Multiplayer\Game\Core\Config.json - appears after the first launch, close the game to edit)
  GameExePath      Full path to SubnauticaZero.exe (or its folder). Empty = auto-detect. Only used for the
                   firewall rule.
  ConfigureFirewall  true/false - allow the game through Windows Firewall when hosting.
  HostOnPort       UDP port the host listens on (default 7777).
  DefaultJoinPort  Port used when a joiner types an address without ":port" (default 7777).
  MaxPlayer        Maximum players.
  ConnectionTimeout  Seconds before a connection times out.

NOTES
  * No NetBird, lobby server or invite code is used any more. Playing over the internet needs
    port forwarding of the host port (UDP) or a VPN of your own; LAN needs nothing.
  * This is an unofficial port of a mod built for an older game build. Not every feature has been
    tested in-game. Back up your saves.
  * Based on the Subnautica Below Zero Multiplayer mod by BOT Benson (ismail0234) and the
    Detanup01 fork.
