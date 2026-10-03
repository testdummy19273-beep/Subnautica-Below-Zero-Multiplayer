using System;
using System.IO;
using System.Reflection;

// Injected into the game's Assembly-CSharp.dll by Patcher\gametool.exe and called from StartScreen.Start.
// Loads Multiplayer\Game\Dependencies\*.dll and then runs Subnautica.Loader.Loader.Run().
public class SubnauticaBootstrap
{
    private static bool IsLoaded;

    public static void Load()
    {
        if (IsLoaded)
        {
            return;
        }

        try
        {
            string[] files = Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Multiplayer", "Game", "Dependencies"), "*.dll");
            for (int i = 0; i < files.Length; i++)
            {
                Assembly.Load(File.ReadAllBytes(files[i]));
            }

            File.AppendAllText("logs.txt", "Dependencies Loaded!" + Environment.NewLine);

            string loaderPath = Path.Combine(Directory.GetCurrentDirectory(), "Multiplayer", "Game", "Subnautica.Loader.dll");
            if (File.Exists(loaderPath))
            {
                Assembly assembly = Assembly.Load(File.ReadAllBytes(loaderPath));
                if (assembly != null)
                {
                    Type type = assembly.GetType("Subnautica.Loader.Loader");
                    if (type != null)
                    {
                        MethodInfo method = type.GetMethod("Run");
                        if (method != null)
                        {
                            method.Invoke(null, null);
                        }
                    }
                }

                IsLoaded = true;
                File.AppendAllText("logs.txt", "Successfully Loadded!" + Environment.NewLine);
            }
            else
            {
                File.AppendAllText("logs.txt", "File not exists! " + loaderPath + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            File.AppendAllText("logs.txt", "Exception: " + ex + Environment.NewLine);
        }
    }
}
