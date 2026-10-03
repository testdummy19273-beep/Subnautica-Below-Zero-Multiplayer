Subnautica Below Zero Multiplayer - LAN / direct IP edition
=========================================================

Everyone needs the SAME game version. This build was compiled against the Steam build of
SubnauticaZero_Data\Managed\Assembly-CSharp.dll dated 4 Oct 2025. If Steam updates the game,
open the launcher and press Update / Repair (or run Install.bat again), and make sure all players
are on the same version.

QUICK START: SubnauticaBZ-Launcher.exe  (portable, no install)
  Unzip this folder anywhere and keep the Multiplayer and Patcher folders next to the launcher.
  * Play game  Finds your game (Steam) or lets you Browse to SubnauticaZero.exe. Install / Update /
               Uninstall the mod and press PLAY. Your original Assembly-CSharp.dll is backed up as
               Assembly-CSharp.dll.original; no game files are redistributed in this zip.
  * Servers    Your hosted worlds as cards (name, mode, Online/Offline). CREATE NEW SERVER asks for a name
               and a game mode (Survival / Freedom / Hardcore / Creative). Start opens the game straight
               into that world; keep it running while your friends play. While it runs the button
               turns into Stop, which saves the world and closes the game. Manage opens the server page:
               name, player limit, auto save interval, port, automatic backups, the addresses your friends
               type (LAN, ZeroTier ...) with a Copy button, Open world folder, Back up now, Restore a backup
               and Delete server.
  * Join       Type the host's address (192.168.1.20 or 192.168.1.20:7777), or pick a saved server, and press
               Join. The game starts and connects by itself. If the game is already open on its main menu,
               Join sends the address to it instead (no restart needed; in a world, quit to the menu first).
  * Options    Your player name, default join port and connection timeout.
  * Game log   Shows the latest mod log, handy if something does not connect.
  Hosting and joining also still work from the game's own Multiplayer menu.
  The server always runs inside the host's game (there is no separate "External" server program).

MANUAL INSTALL (without the launcher)
  1. Run Install.bat. If your game is not in the default Steam folder, run it from a command prompt as
     Install.bat "D:\Games\SubnauticaZero"   or type the folder when asked.
  2. To remove it again, run Uninstall.bat (or Uninstall in the launcher).

HOST (in-game menu)
  1. Start the game, Multiplayer -> Host, create a new world (or load a hosted save).
  2. The first time, Windows asks for administrator permission to open the firewall for the game.
     Say yes (it only adds an "allow SubnauticaZero.exe" rule). Turn the firewall option off in
     Settings if you do not want that.
  3. In the intro screen, or in the in-game menu ("Show Server IP"), your LAN address is shown,
     e.g. 192.168.1.20:7777. Tell your friends that address.

JOIN (in-game menu)
  Multiplayer -> Join, type the host's address (192.168.1.20 or 192.168.1.20:7777) and click Join.

OVER ZEROTIER / ANOTHER VPN
  * Everyone joins the same ZeroTier network and is authorized in ZeroTier Central (both machines online).
  * The host gives out its ZeroTier address (Manage page lists every address, with the adapter name).
  * Type it with the port if the world is not on 7777, e.g. 10.147.17.5:7777. The in-game box now accepts ':'.
  * The first connection over a VPN can take a few seconds; the game waits up to 10 seconds.
  * If it still fails, the game log (Game log page) says why: timeout, rejected, or version mismatch.

OPTIONS  (Multiplayer\Game\Core\Config.json - the launcher's Settings tab edits it; edit by hand with the game closed)
  PlayerName       Your name in multiplayer (2-24 characters: letters, digits, space, _ - .). Empty = your Steam
                   name. With a name set, nothing depends on Steam: your player id is made from the name, so
                   keep using the same name to keep your character in a world. Every player on a server needs
                   a different name.
  AutoSaveInterval How often the host saves the world, in seconds (default 5, 1-600). The launcher sets it
                   per server from the Manage page.
  GameExePath      Full path to SubnauticaZero.exe (or its folder). Empty = auto-detect. Only used for the
                   firewall rule.
  ConfigureFirewall  true/false - allow the game through Windows Firewall when hosting.
  HostOnPort       UDP port the host listens on (default 7777).
  DefaultJoinPort  Port used when a joiner types an address without ":port" (default 7777).
  MaxPlayer        Maximum players.
  ConnectionTimeout  Seconds before a connection times out.

COMMAND LINE (what the launcher uses; you can use it in a Steam shortcut too)
  SubnauticaZero.exe -bzmp-host <world id>      host an existing world
  SubnauticaZero.exe -bzmp-host new:Survival    create a world and host it
  SubnauticaZero.exe -bzmp-join 192.168.1.20:7777
  SubnauticaZero.exe -bzmp-name Alice           use this player name for this run

NOTES
  * No NetBird, lobby server or invite code is used any more. Playing over the internet needs
    port forwarding of the host port (UDP) or a VPN of your own (ZeroTier works); LAN needs nothing.
  * The server runs inside the host's game, so the host has to keep the game running while others play.
  * This is an unofficial port of a mod built for an older game build. Not every feature has been
    tested in-game. Back up your saves.
  * Based on the Subnautica Below Zero Multiplayer mod by BOT Benson (ismail0234) and the
    Detanup01 fork.
