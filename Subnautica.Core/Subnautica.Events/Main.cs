namespace Subnautica.Events
{
    using HarmonyLib;

    using Subnautica.API.Enums;
    using Subnautica.API.Features;

    using System;
    using System.Linq;

    using UnityEngine.SceneManagement;

    public sealed class Main : SubnauticaPlugin
    {
        public override string Name { get; } = "Subnautica Events";

        public override SubnauticaPluginPriority Priority { get; set; } = SubnauticaPluginPriority.First;

        public override void OnEnabled()
        {
            base.OnEnabled();

            SceneManager.sceneLoaded += Patches.Events.Game.SceneLoaded.Run;

            try
            {
                var harmony = new Harmony("Subnautica.Events.Main");

                int patched = 0, failed = 0;

                foreach (var type in typeof(Main).Assembly.GetTypes().Where(q => q.Namespace != null && q.Namespace.StartsWith("Subnautica.Events")))
                {
                    try
                    {
                        if (harmony.CreateClassProcessor(type).Patch() != null)
                        {
                            patched++;
                        }
                    }
                    catch (Exception e)
                    {
                        failed++;
                        Log.Error($"Harmony - Patch failed for {type.FullName}: {e.InnerException?.Message ?? e.Message}");
                    }
                }

                Log.Info($"Harmony - Subnautica.Events: {patched} patch classes applied, {failed} failed.");
            }
            catch (Exception e)
            {
                Log.Error($"Harmony - Patching failed! {e}");
            }
        }
    }
}
