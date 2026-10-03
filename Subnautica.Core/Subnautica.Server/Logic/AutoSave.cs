namespace Subnautica.Server.Logic
{
    using Core;

    using Subnautica.API.Features;
    using Subnautica.Server.Abstracts;

    public class AutoSave : BaseLogic
    {
        public StopwatchItem Timing { get; set; } = new StopwatchItem(Settings.ModConfig.AutoSaveInterval.GetInt(5) * 1000f);

        public override void OnAsyncUpdate()
        {
            if (API.Features.World.IsLoaded && this.Timing.IsFinished())
            {
                this.Timing.Restart();
                this.SaveAll();
            }
        }

        public void SaveAll()
        {
            Server.Instance.Storages.Encyclopedia.SaveToDisk();
            Server.Instance.Storages.Construction.SaveToDisk();
            Server.Instance.Storages.PictureFrame.SaveToDisk();
            Server.Instance.Storages.Technology.SaveToDisk();
            Server.Instance.Storages.Scanner.SaveToDisk();
            Server.Instance.Storages.Player.SaveToDisk();
            Server.Instance.Storages.World.SaveToDisk();
            Server.Instance.Storages.Story.SaveToDisk();
        }
    }
}
