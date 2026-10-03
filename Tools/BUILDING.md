# Building

You need the .NET SDK (8.0 is fine), your own Subnautica: Below Zero install, and Windows.

1. **Reference assemblies.** The mod compiles against *publicized* copies of the game's `Assembly-CSharp` and
   `Assembly-CSharp-firstpass` (private members made public) and `Sentry`. Create them with the tool in this repo:

   ```
   dotnet build Tools/gametool -c Release
   Tools\gametool\bin\Release\net472\gametool.exe publicize "<Game>\SubnauticaZero_Data\Managed\Assembly-CSharp.dll" refs\Assembly-CSharp_public.dll
   Tools\gametool\bin\Release\net472\gametool.exe publicize "<Game>\SubnauticaZero_Data\Managed\Assembly-CSharp-firstpass.dll" refs\Assembly-CSharp-firstpass-publicized.dll
   Tools\gametool\bin\Release\net472\gametool.exe publicize "<Game>\SubnauticaZero_Data\Managed\Sentry.dll" refs\Sentry-publicized.dll
   ```

2. **Tell MSBuild where they are.** Create `Subnautica.Core/ReferencePaths.props.user` and
   `Subnautica.Loader/ReferencePaths.props.user` (both git-ignored):

   ```xml
   <Project><PropertyGroup><ManagedPath>C:\...\SubnauticaZero_Data\Managed;C:\...\refs</ManagedPath></PropertyGroup></Project>
   ```

3. **Build.**

   ```
   dotnet build Subnautica.Core/Subnautica.Core.csproj -c Release
   dotnet build Subnautica.Loader/Subnautica.Loader.csproj -c Release
   dotnet build Tools/bootstrap -c Release
   python Tools/release/make_package.py      # writes out/SubnauticaBZ-Multiplayer-LAN.zip
   ```

`Tools/patchcheck` is an optional sanity check: it lists Harmony patches whose target method no longer exists in the game
(`patchcheck.exe "<Managed dir>" Subnautica.Core.dll targets`). Run it after a game update.
