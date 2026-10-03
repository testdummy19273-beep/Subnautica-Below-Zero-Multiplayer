namespace Subnautica.Client
{
    using Subnautica.Client.Modules;
    using Subnautica.Events.EventArgs;
    using Building = Subnautica.Client.Synchronizations.Processors.Building;
    using Creatures = Subnautica.Client.Synchronizations.Processors.Creatures;
    using Encyclopedia = Subnautica.Client.Synchronizations.Processors.Encyclopedia;
    using General = Subnautica.Client.Synchronizations.Processors.General;
    using Initial = Subnautica.Client.Synchronizations.InitialSync;
    using Inventory = Subnautica.Client.Synchronizations.Processors.Inventory;
    using Items = Subnautica.Client.Synchronizations.Processors.Items;
    using Metadata = Subnautica.Client.Synchronizations.Processors.Metadata;
    using PDA = Subnautica.Client.Synchronizations.Processors.PDA;
    using Player = Subnautica.Client.Synchronizations.Processors.Player;
    using Story = Subnautica.Client.Synchronizations.Processors.Story;
    using Technology = Subnautica.Client.Synchronizations.Processors.Technology;
    using Vehicle = Subnautica.Client.Synchronizations.Processors.Vehicle;
    using World = Subnautica.Client.Synchronizations.Processors.World;
    using WorldEntities = Subnautica.Client.Synchronizations.Processors.WorldEntities;

    public class Router
    {
        public void OnPluginEnabled()
        {
            MainProcess.OnPluginEnabled();
        }

        public void OnInGameMenuOpened(InGameMenuOpenedEventArgs ev)
        {
            LanHostModule.OnInGameMenuOpened(ev);
            ClientServerConnection.OnInGameMenuOpened(ev);
        }

        public void OnInGameMenuClosed(InGameMenuClosedEventArgs ev)
        {
            ClientServerConnection.OnInGameMenuClosed(ev);
        }

        public void OnSettingsRunInBackgroundChanging(SettingsRunInBackgroundChangingEventArgs ev)
        {
            ClientServerConnection.OnSettingsRunInBackgroundChanging(ev);
        }

        public void OnSceneLoaded(SceneLoadedEventArgs ev)
        {
            MainProcess.OnSceneLoaded(ev);
            MultiplayerMainMenu.OnSceneLoaded(ev);
        }

        public void OnMenuSaveCancelDeleteButtonClicking(MenuSaveCancelDeleteButtonClickingEventArgs ev)
        {
            MultiplayerMainMenu.OnMenuSaveCancelDeleteButtonClicking(ev);
        }

        public void OnMenuSaveLoadButtonClicking(MenuSaveLoadButtonClickingEventArgs ev)
        {
            MultiplayerMainMenu.OnMenuSaveLoadButtonClicking(ev);
        }

        public void OnMenuSaveDeleteButtonClicking(MenuSaveDeleteButtonClickingEventArgs ev)
        {
            MultiplayerMainMenu.OnMenuSaveDeleteButtonClicking(ev);
        }

        public void OnMenuSaveUpdateLoadedButtonState(MenuSaveUpdateLoadedButtonStateEventArgs ev)
        {
            MultiplayerMainMenu.OnMenuSaveUpdateLoadedButtonState(ev);
        }

        public void OnEncyclopediaAdded(EncyclopediaAddedEventArgs ev)
        {
            Encyclopedia.AddedProcessor.OnEncyclopediaAdded(ev);
        }

        public void OnConstructingGhostMoved(ConstructionGhostMovedEventArgs ev)
        {
            Building.GhostMovedProcessor.OnConstructingGhostMoved(ev);
        }

        public void OnConstructingGhostTryPlacing(ConstructionGhostTryPlacingEventArgs ev)
        {
            Building.GhostTryPlacingProcessor.OnConstructingGhostTryPlacing(ev);
        }

        public void OnPlayerUpdated(PlayerUpdatedEventArgs ev)
        {
            Player.UpdatedProcessor.OnPlayerUpdated(ev);
        }

        public void OnTechnologyAdded(TechnologyAddedEventArgs ev)
        {
            Technology.AddedProcessor.OnTechnologyAdded(ev);
        }

        public void OnTechnologyFragmentAdded(TechnologyFragmentAddedEventArgs ev)
        {
            Technology.FragmentAddedProcessor.OnTechnologyFragmentAdded(ev);
        }

        public void OnSettingsPdaGamePauseChanging(SettingsPdaGamePauseChangingEventArgs ev)
        {
            ClientServerConnection.OnSettingsPdaGamePauseChanging(ev);
        }

        public void OnConstructingAmountChanged(ConstructionAmountChangedEventArgs ev)
        {
            Building.AmountChangedProcessor.OnConstructingAmountChanged(ev);
        }

        public void OnConstructingCompleted(ConstructionCompletedEventArgs ev)
        {
            Building.CompletedProcessor.OnConstructingCompleted(ev);
            Building.ConstructionSyncedProcessor.OnConstructingCompleted(ev);
        }

        public void OnConstructingRemoved(ConstructionRemovedEventArgs ev)
        {
            Building.RemovedProcessor.OnConstructingRemoved(ev);
            Building.ConstructionSyncedProcessor.OnConstructingRemoved(ev);
        }

        public void OnInventoryItemAdded(InventoryItemAddedEventArgs ev)
        {
            Inventory.ItemProcessor.OnInventoryItemAdded(ev);
        }

        public void OnInventoryItemRemoved(InventoryItemRemovedEventArgs ev)
        {
            Inventory.ItemProcessor.OnInventoryItemRemoved(ev);
        }

        public void OnPlayerStatsUpdated(PlayerStatsUpdatedEventArgs ev)
        {
            Player.StatsProcessor.OnPlayerStatsUpdated(ev);
        }

        public void OnToolBatteryEnergyChanged(ToolBatteryEnergyChangedEventArgs ev)
        {
            Player.ToolEnergyProcessor.OnToolBatteryEnergyChanged(ev);
        }

        public void OnUsingCommand(PlayerUsingCommandEventArgs ev)
        {
            Player.ConsoleCommandProcessor.OnUsingCommand(ev);
        }

        public void OnPlayerRespawnPointChanged(PlayerRespawnPointChangedEventArgs ev)
        {
            Player.RespawnPointProcessor.OnPlayerRespawnPointChanged(ev);
        }

        public void OnPingVisibilityChanged(PlayerPingVisibilityChangedEventArgs ev)
        {
            PDA.NotificationProcessor.OnPingVisibilityChanged(ev);
        }

        public void OnPingColorChanged(PlayerPingColorChangedEventArgs ev)
        {
            PDA.NotificationProcessor.OnPingColorChanged(ev);
        }

        public void OnQuittingToMainMenu(QuittingToMainMenuEventArgs ev)
        {
            MainProcess.OnQuittingToMainMenu(ev);
        }

        public void OnQuitting()
        {
            // Closing the game window (or the launcher's Stop button) must save and stop a hosted world.
            try
            {
                Subnautica.Client.Core.NetworkServer.AbortServer();
            }
            catch (System.Exception e)
            {
                Subnautica.API.Features.Log.Error($"Router.OnQuitting: {e}");
            }
        }

        public void OnEquipmentEquiped()
        {
            Inventory.EquipmentProcessor.OnProcessEquipment();
        }

        public void OnEquipmentUnequiped()
        {
            Inventory.EquipmentProcessor.OnProcessEquipment();
        }

        public void OnQuickSlotBinded()
        {
            Inventory.QuickSlotProcessor.OnProcessQuickSlot();
        }

        public void OnQuickSlotUnbinded()
        {
            Inventory.QuickSlotProcessor.OnProcessQuickSlot();
        }

        public void OnQuickSlotActiveChanged(QuickSlotActiveChangedEventArgs ev)
        {
            Inventory.QuickSlotProcessor.OnProcessQuickSlot();
        }

        public void OnScannerCompleted(ScannerCompletedEventArgs ev)
        {
            Technology.ScannerCompletedProcessor.OnScannerCompleted(ev);
        }

        public void OnItemPinAdded()
        {
            Inventory.ItemPinProcessor.OnProcessPin();
        }

        public void OnItemPinRemoved()
        {
            Inventory.ItemPinProcessor.OnProcessPin();
        }

        public void OnItemPinMoved()
        {
            Inventory.ItemPinProcessor.OnProcessPin();
        }

        public void OnPDALogAdded(PDALogAddedEventArgs ev)
        {
            PDA.LogAddedProcessor.OnPDALogAdded(ev);
        }

        public void OnNotificationToggle(NotificationToggleEventArgs ev)
        {
            PDA.NotificationProcessor.OnNotificationToggle(ev);
        }

        public void OnTechAnalyzeAdded(TechAnalyzeAddedEventArgs ev)
        {
            PDA.TechAnalyzeAddedProcessor.OnTechAnalyzeAdded(ev);
        }

        public void OnDeconstructionBegin(DeconstructionBeginEventArgs ev)
        {
            Building.DeconstructionBeginProcessor.OnDeconstructionBegin(ev);
        }

        public void OnFurnitureDeconstructionBegin(FurnitureDeconstructionBeginEventArgs ev)
        {
            Building.FurnitureDeconstructionBeginProcessor.OnFurnitureDeconstructionBegin(ev);
        }

        public void OnToiletSwitchToggle(ToiletSwitchToggleEventArgs ev)
        {
            Metadata.ToiletProcessor.OnToiletSwitchToggle(ev);
        }

        public void OnEmmanuelPendulumSwitchToggle(EmmanuelPendulumSwitchToggleEventArgs ev)
        {
            Metadata.EmmanuelPendulumProcessor.OnEmmanuelPendulumSwitchToggle(ev);
        }

        public void OnAromatherapyLampSwitchToggle(AromatherapyLampSwitchToggleEventArgs ev)
        {
            Metadata.AromatherapyProcessor.OnAromatherapyLampSwitchToggle(ev);
        }

        public void OnSmallStoveSwitchToggle(SmallStoveSwitchToggleEventArgs ev)
        {
            Metadata.SmallStoveProcessor.OnSmallStoveSwitchToggle(ev);
        }

        public void OnSinkSwitchToggle(SinkSwitchToggleEventArgs ev)
        {
            Metadata.SinkProcessor.OnSinkSwitchToggle(ev);
        }

        public void OnShowerSwitchToggle(ShowerSwitchToggleEventArgs ev)
        {
            Metadata.ShowerProcessor.OnShowerSwitchToggle(ev);
        }

        public void OnSnowmanDestroying(SnowmanDestroyingEventArgs ev)
        {
            Metadata.SnowmanProcessor.OnSnowmanDestroying(ev);
            World.StaticEntityProcessor.OnSnowmanDestroying(ev);
        }

        public void OnSignDataChanged(SignDataChangedEventArgs ev)
        {
            Metadata.SignProcessor.OnSignDataChanged(ev);
            Items.DeployableStorageProcessor.OnSignDataChanged(ev);
            Vehicle.SeaTruckStorageModuleProcessor.OnSignDataChanged(ev);
            Vehicle.SeaTruckFabricatorModuleProcessor.OnSignDataChanged(ev);
        }

        public void OnPictureFrameImageSelecting(PictureFrameImageSelectingEventArgs ev)
        {
            Metadata.PictureFrameProcessor.OnPictureFrameImageSelecting(ev);
        }

        public void OnBedIsCanSleepChecking(BedIsCanSleepCheckingEventArgs ev)
        {
            Metadata.BedProcessor.OnBedIsCanSleepChecking(ev);
        }

        public void OnBedEnterInUseMode(BedEnterInUseModeEventArgs ev)
        {
            Metadata.BedProcessor.OnBedEnterInUseMode(ev);
            Vehicle.SeaTruckSleeperModuleProcessor.OnBedEnterInUseMode(ev);
        }

        public void OnBedExitInUseMode(BedExitInUseModeEventArgs ev)
        {
            Metadata.BedProcessor.OnBedExitInUseMode(ev);
            Vehicle.SeaTruckSleeperModuleProcessor.OnBedExitInUseMode(ev);
        }

        public void OnJukeboxUsed(JukeboxUsedEventArgs ev)
        {
            Metadata.JukeboxProcessor.OnJukeboxUsed(ev);
            Vehicle.SeaTruckSleeperModuleProcessor.OnJukeboxUsed(ev);
        }

        public void OnJukeboxDiskAdded(JukeboxDiskAddedEventArgs ev)
        {
            PDA.JukeboxDiskAddedProcessor.OnJukeboxDiskAdded(ev);
        }

        public void OnCrafterItemPickup(CrafterItemPickupEventArgs ev)
        {
            Metadata.CrafterProcessor.OnCrafterItemPickup(ev);
        }

        public void OnCrafterClosed(CrafterClosedEventArgs ev)
        {
            Metadata.CrafterProcessor.OnCrafterClosed(ev);
        }

        public void OnCrafterBegin(CrafterBeginEventArgs ev)
        {
            Metadata.CrafterProcessor.OnCrafterBegin(ev);
        }

        public void OnCrafterEnded(CrafterEndedEventArgs ev)
        {
            Metadata.CrafterProcessor.OnCrafterEnded(ev);
        }

        public void OnBenchSitdown(BenchSitdownEventArgs ev)
        {
            Metadata.BenchProcessor.OnBenchSitdown(ev);
        }

        public void OnBenchStandup(BenchStandupEventArgs ev)
        {
            Metadata.BenchProcessor.OnBenchStandup(ev);
        }

        public void OnSpotLightInitialized(SpotLightInitializedEventArgs ev)
        {
            Metadata.SpotLightProcessor.OnSpotLightInitialized(ev);
        }

        public void OnTechLightInitialized(TechLightInitializedEventArgs ev)
        {
            Metadata.TechlightProcessor.OnTechLightInitialized(ev);
        }

        public void OnBaseMapRoomScanStarting(BaseMapRoomScanStartingEventArgs ev)
        {
            Metadata.BaseMapRoomProcessor.OnBaseMapRoomScanStarting(ev);
        }

        public void OnBaseMapRoomScanStopping(BaseMapRoomScanStoppingEventArgs ev)
        {
            Metadata.BaseMapRoomProcessor.OnBaseMapRoomScanStopping(ev);
        }

        public void OnBaseMapRoomCameraChanging(MapRoomCameraChangingEventArgs ev)
        {
            Metadata.BaseMapRoomProcessor.OnBaseMapRoomCameraChanging(ev);
        }

        public void OnBaseMapRoomResourceDiscovering(BaseMapRoomResourceDiscoveringEventArgs ev)
        {
            General.ResourceDiscoverProcessor.OnBaseMapRoomResourceDiscovering(ev);
        }

        public void OnBaseMapRoomInitialized(BaseMapRoomInitializedEventArgs ev)
        {
            General.ResourceDiscoverProcessor.OnBaseMapRoomInitialized(ev);
        }

        public void OnBaseMoonpoolExpansionUndockingTimelineCompleting(BaseMoonpoolExpansionUndockingTimelineCompletingEventArgs ev)
        {
            Metadata.MoonpoolProcessor.OnBaseMoonpoolExpansionUndockingTimelineCompleting(ev);
        }

        public void OnBaseMoonpoolExpansionDockingTimelineCompleting(BaseMoonpoolExpansionDockingTimelineCompletingEventArgs ev)
        {
            Metadata.MoonpoolProcessor.OnBaseMoonpoolExpansionDockingTimelineCompleting(ev);
        }

        public void OnBaseMoonpoolExpansionDockTail(BaseMoonpoolExpansionDockTailEventArgs ev)
        {
            Metadata.MoonpoolProcessor.OnBaseMoonpoolExpansionDockTail(ev);
        }

        public void OnBaseMoonpoolExpansionUndockTail(BaseMoonpoolExpansionUndockTailEventArgs ev)
        {
            Metadata.MoonpoolProcessor.OnBaseMoonpoolExpansionUndockTail(ev);
        }

        public void OnPlayerBaseEntered(PlayerBaseEnteredEventArgs ev)
        {
            Player.SubrootToggleProcessor.OnPlayerBaseEntered(ev);
        }

        public void OnPlayerBaseExited(PlayerBaseExitedEventArgs ev)
        {
            Player.SubrootToggleProcessor.OnPlayerBaseExited(ev);
        }

        public void OnPictureFrameOpening(PictureFrameOpeningEventArgs ev)
        {
            Metadata.PictureFrameProcessor.OnPictureFrameOpening(ev);
        }

        public void OnCrafterOpening(CrafterOpeningEventArgs ev)
        {
            Metadata.CrafterProcessor.OnCrafterOpening(ev);
        }

        public void OnChargerOpening(ChargerOpeningEventArgs ev)
        {
            Metadata.ChargerProcessor.OnChargerOpening(ev);
        }

        public void OnStorageOpening(StorageOpeningEventArgs ev)
        {
            General.StorageOpenProcessor.OnStorageOpening(ev);
        }

        public void OnStorageItemAdding(StorageItemAddingEventArgs ev)
        {
            General.LifepodProcessor.OnStorageItemAdding(ev);
            Metadata.StorageProcessor.OnStorageItemAdding(ev);
            Metadata.AquariumProcessor.OnStorageItemAdding(ev);
            Metadata.BioReactorProcessor.OnStorageItemAdding(ev);
            Metadata.FridgeProcessor.OnStorageItemAdding(ev);
            Metadata.BaseMapRoomProcessor.OnStorageItemAdding(ev);
            Items.DeployableStorageProcessor.OnStorageItemAdding(ev);
            Items.SpyPenguinProcessor.OnStorageItemAdding(ev);
            Vehicle.SeaTruckFabricatorModuleProcessor.OnStorageItemAdding(ev);
            Vehicle.SeaTruckStorageModuleProcessor.OnStorageItemAdding(ev);
            Vehicle.SeaTruckAquariumModuleProcessor.OnStorageItemAdding(ev);
            Vehicle.ExosuitStorageProcessor.OnStorageItemAdding(ev);

            Metadata.FiltrationMachineProcessor.OnStorageItemAdding(ev);
            Metadata.CoffeeVendingMachineProcessor.OnStorageItemAdding(ev);
        }

        public void OnStorageItemRemoving(StorageItemRemovingEventArgs ev)
        {
            General.LifepodProcessor.OnStorageItemRemoving(ev);
            Metadata.StorageProcessor.OnStorageItemRemoving(ev);
            Metadata.AquariumProcessor.OnStorageItemRemoving(ev);
            Metadata.FridgeProcessor.OnStorageItemRemoving(ev);
            Metadata.BaseMapRoomProcessor.OnStorageItemRemoving(ev);
            Items.DeployableStorageProcessor.OnStorageItemRemoving(ev);
            Items.SpyPenguinProcessor.OnStorageItemRemoving(ev);
            Vehicle.SeaTruckFabricatorModuleProcessor.OnStorageItemRemoving(ev);
            Vehicle.SeaTruckStorageModuleProcessor.OnStorageItemRemoving(ev);
            Vehicle.SeaTruckAquariumModuleProcessor.OnStorageItemRemoving(ev);
            Vehicle.ExosuitStorageProcessor.OnStorageItemRemoving(ev);

            Metadata.FiltrationMachineProcessor.OnStorageItemRemoving(ev);
            Metadata.CoffeeVendingMachineProcessor.OnStorageItemRemoving(ev);
        }

        public void OnNuclearReactorItemAdded(NuclearReactorItemAddedEventArgs ev)
        {
            Metadata.NuclearReactorProcessor.OnNuclearReactorItemAdded(ev);
        }

        public void OnNuclearReactorItemRemoved(NuclearReactorItemRemovedEventArgs ev)
        {
            Metadata.NuclearReactorProcessor.OnNuclearReactorItemRemoved(ev);
        }

        public void OnSignSelect(SignSelectEventArgs ev)
        {
            Metadata.SignProcessor.OnSignSelect(ev);
            Items.DeployableStorageProcessor.OnSignSelect(ev);
            Vehicle.SeaTruckStorageModuleProcessor.OnSignSelect(ev);
            Vehicle.SeaTruckFabricatorModuleProcessor.OnSignSelect(ev);
        }

        public void OnSignDeselect(SignDeselectEventArgs ev)
        {
            General.InteractProcessor.OnSignDeselect(ev);
        }

        public void OnClosing(PDAClosingEventArgs ev)
        {
            Metadata.ChargerProcessor.OnClosing(ev);
            General.InteractProcessor.OnClosing(ev);
        }

        public void OnChargerItemAdded(ChargerItemAddedEventArgs ev)
        {
            Metadata.ChargerProcessor.OnChargerItemAdded(ev);
        }

        public void OnChargerItemRemoved(ChargerItemRemovedEventArgs ev)
        {
            Metadata.ChargerProcessor.OnChargerItemRemoved(ev);
        }

        public void OnHoverpadHoverbikeSpawning(HoverpadHoverbikeSpawningEventArgs ev)
        {
            Metadata.HoverpadProcessor.OnHoverpadHoverbikeSpawning(ev);
        }

        public void OnRecyclotronRecycle(RecyclotronRecycleEventArgs ev)
        {
            Metadata.RecyclotronProcessor.OnRecyclotronRecycle(ev);
        }

        public void OnPlanterItemAdded(PlanterItemAddedEventArgs ev)
        {
            Metadata.PlanterProcessor.OnPlanterItemAdded(ev);
        }

        public void OnPlanterProgressCompleted(PlanterProgressCompletedEventArgs ev)
        {
            Metadata.PlanterProcessor.OnPlanterProgressCompleted(ev);
        }

        public void OnPlanterGrowned(PlanterGrownedEventArgs ev)
        {
            Metadata.PlanterProcessor.OnPlanterGrowned(ev);
        }

        public void OnBulkheadOpening(BulkheadOpeningEventArgs ev)
        {
            Metadata.BulkheadProcessor.OnBulkheadOpening(ev);
            WorldEntities.BulkheadDoorProcessor.OnBulkheadOpening(ev);
        }

        public void OnBulkheadClosing(BulkheadClosingEventArgs ev)
        {
            Metadata.BulkheadProcessor.OnBulkheadClosing(ev);
            WorldEntities.BulkheadDoorProcessor.OnBulkheadClosing(ev);
        }

        public void OnThermalLilyRangeChecking(ThermalLilyRangeCheckingEventArgs ev)
        {
            WorldEntities.ThermalLilyProcessor.OnThermalLilyRangeChecking(ev);
        }

        public void OnThermalLilyAnimationAnglesChecking(ThermalLilyAnimationAnglesCheckingEventArgs ev)
        {
            WorldEntities.ThermalLilyProcessor.OnThermalLilyAnimationAnglesChecking(ev);
        }

        public void OnOxygenPlantClicking(OxygenPlantClickingEventArgs ev)
        {
            WorldEntities.OxygenPlantProcessor.OnOxygenPlantClicking(ev);
        }

        public void OnTeleporterTerminalActivating(TeleporterTerminalActivatingEventArgs ev)
        {
            World.PrecursorTeleporterProcessor.OnTeleporterTerminalActivating(ev);
        }

        public void OnTeleporterInitialized(TeleporterInitializedEventArgs ev)
        {
            World.PrecursorTeleporterProcessor.OnTeleporterInitialized(ev);
        }

        public void OnPrecursorTeleporterUsed()
        {
            World.PrecursorTeleporterProcessor.OnPrecursorTeleporterUsed();
        }

        public void OnPrecursorTeleportationCompleted()
        {
            World.PrecursorTeleporterProcessor.OnPrecursorTeleportationCompleted();
        }

        public void OnElevatorInitialized(ElevatorInitializedEventArgs ev)
        {
            MultiplayerElevator.OnElevatorInitialized(ev);
        }

        public void OnEntitySpawning(EntitySpawningEventArgs ev)
        {
            World.EntitySpawnProcessor.OnEntitySpawning(ev);
            World.EntitySlotSpawnProcessor.OnEntitySpawning(ev);
            General.LifepodProcessor.OnEntitySpawning(ev);
        }

        public void OnEntitySlotSpawning(EntitySlotSpawningEventArgs ev)
        {
            World.EntitySlotSpawnProcessor.OnEntitySlotSpawning(ev);
        }

        public void OnEntitySpawned(EntitySpawnedEventArgs ev)
        {
            World.EntitySpawnProcessor.OnEntitySpawned(ev);
            World.EntitySlotSpawnProcessor.OnEntitySpawned(ev);
            World.BrinicleProcessor.OnEntitySpawned(ev);
            Metadata.CrafterProcessor.OnEntitySpawned(ev);
        }

        public void OnKnifeUsing(KnifeUsingEventArgs ev)
        {
            Items.KnifeProcessor.OnKnifeUsing(ev);
        }

        public void OnWorldLoading(WorldLoadingEventArgs ev)
        {
            Initial.WorldProcessor.OnWorldLoading(ev);
        }

        public void OnCellLoading(CellLoadingEventArgs ev)
        {
            World.CellProcessor.OnCellLoading(ev);
        }

        public void OnCellUnLoading(CellUnLoadingEventArgs ev)
        {
            World.CellProcessor.OnCellUnLoading(ev);
        }

        public void OnWorldLoaded(WorldLoadedEventArgs ev)
        {
            Initial.WorldProcessor.OnWorldLoaded(ev);
            PingLatency.OnWorldLoaded();
        }

        public void OnEntityScannerCompleted(EntityScannerCompletedEventArgs ev)
        {
            World.EntityScannerProcessor.OnEntityScannerCompleted(ev);
        }

        public void OnAlterraPdaPickedUp(AlterraPdaPickedUpEventArgs ev)
        {
            World.StaticEntityProcessor.OnAlterraPdaPickedUp(ev);
        }

        public void OnJukeboxDiskPickedUp(JukeboxDiskPickedUpEventArgs ev)
        {
            World.StaticEntityProcessor.OnJukeboxDiskPickedUp(ev);
        }

        public void OnPlayerItemPickedUp(PlayerItemPickedUpEventArgs ev)
        {
            World.StaticEntityProcessor.OnPlayerItemPickedUp(ev);
            World.EntitySlotSpawnProcessor.OnPlayerItemPickedUp(ev);
            World.CosmeticItemProcessor.OnPlayerItemPickedUp(ev);
            Player.ItemPickupProcessor.OnPlayerItemPickedUp(ev);
            Items.PipeSurfaceFloaterProcessor.OnPlayerItemPickedUp(ev);
            Metadata.BaseMapRoomProcessor.OnPlayerItemPickedUp(ev);
            Metadata.BaseWaterParkProcessor.OnPlayerItemPickedUp(ev);
        }

        public void OnScannerUsing(ScannerUsingEventArgs ev)
        {
            Items.ScannerProcessor.OnScannerUsing(ev);
        }

        public void OnDroneCameraDeploying(DroneCameraDeployingEventArgs ev)
        {
            Items.MapRoomCameraProcessor.OnDroneCameraDeploying(ev);
        }

        public void OnPipeSurfaceFloaterDeploying(PipeSurfaceFloaterDeployingEventArgs ev)
        {
            Items.PipeSurfaceFloaterProcessor.OnPipeSurfaceFloaterDeploying(ev);
        }

        public void OnOxygenPipePlacing(OxygenPipePlacingEventArgs ev)
        {
            Items.PipeSurfaceFloaterProcessor.OnOxygenPipePlacing(ev);
        }

        public void OnWelding(WeldingEventArgs ev)
        {
            World.WelderProcessor.OnWelding(ev);
        }

        public void OnSupplyCrateOpened(SupplyCrateOpenedEventArgs ev)
        {
            WorldEntities.SupplyCrateProcessor.OnSupplyCrateOpened(ev);
        }

        public void OnDataboxItemPickedUp(DataboxItemPickedUpEventArgs ev)
        {
            WorldEntities.DataboxProcessor.OnDataboxItemPickedUp(ev);
        }

        public void OnTakeDamaging(TakeDamagingEventArgs ev)
        {
            WorldEntities.DestroyableEntityProcessor.OnTakeDamaging(ev);
            WorldEntities.DestroyableDynamicEntityProcessor.OnTakeDamaging(ev);
            Metadata.PlanterProcessor.OnTakeDamaging(ev);
            Building.BaseHullStrengthProcessor.OnTakeDamaging(ev);
            Building.HealthProcessor.OnTakeDamaging(ev);
            Creatures.HealthProcessor.OnTakeDamaging(ev);
            Vehicle.HealthProcessor.OnTakeDamaging(ev);
            World.BrinicleProcessor.OnTakeDamaging(ev);
        }

        public void OnFruitHarvesting(FruitHarvestingEventArgs ev)
        {
            Metadata.PlanterProcessor.OnFruitHarvesting(ev);
            WorldEntities.FruitHarvestProcessor.OnFruitHarvesting(ev);
        }

        public void OnGrownPlantHarvesting(GrownPlantHarvestingEventArgs ev)
        {
            Metadata.PlanterProcessor.OnGrownPlantHarvesting(ev);
        }

        public void OnPlayerAnimationChanged(PlayerAnimationChangedEventArgs ev)
        {
            Player.AnimationChangedProcessor.OnPlayerAnimationChanged(ev);
        }

        public void OnPlayerItemDroping(PlayerItemDropingEventArgs ev)
        {
            Player.ItemDropProcessor.OnPlayerItemDroping(ev);
            Metadata.BaseWaterParkProcessor.OnPlayerItemDroping(ev);
        }

        public void OnSleepScreenStartingCompleted()
        {
            Metadata.BedProcessor.OnSleepScreenStartingCompleted();
        }

        public void OnSleepScreenStopingStarted()
        {
            Metadata.BedProcessor.OnSleepScreenStopingStarted();
        }

        public void OnIntroChecking(IntroCheckingEventArgs ev)
        {
            General.IntroProcessor.OnIntroChecking(ev);
        }

        public void OnLifepodZoneSelecting(LifepodZoneSelectingEventArgs ev)
        {
            General.LifepodProcessor.OnLifepodZoneSelecting(ev);
        }

        public void OnLifepodZoneCheck(LifepodZoneCheckEventArgs ev)
        {
            General.LifepodProcessor.OnLifepodZoneCheck(ev);
        }

        public void OnLifepodInterpolation(LifepodInterpolationEventArgs ev)
        {
            General.LifepodProcessor.OnLifepodInterpolation(ev);
        }

        public void OnUseableDiveHatchClicking(UseableDiveHatchClickingEventArgs ev)
        {
            Player.UseableDiveHatchProcessor.OnUseableDiveHatchClicking(ev);
        }

        public void OnEnteredInterior(PlayerEnteredInteriorEventArgs ev)
        {
            Player.InteriorToggleProcessor.OnEnteredInterior(ev);
        }

        public void OnExitedInterior(PlayerExitedInteriorEventArgs ev)
        {
            Player.InteriorToggleProcessor.OnExitedInterior(ev);
        }

        public void OnBaseHullStrengthCrushing(BaseHullStrengthCrushingEventArgs ev)
        {
            Building.BaseHullStrengthProcessor.OnBaseHullStrengthCrushing(ev);
        }

        public void OnSubNameInputDeselected(SubNameInputDeselectedEventArgs ev)
        {
            Metadata.BaseControlRoomProcessor.OnSubNameInputDeselected(ev);
            Metadata.HoverpadProcessor.OnSubNameInputDeselected(ev);
            Metadata.MoonpoolProcessor.OnSubNameInputDeselected(ev);
        }

        public void OnSubNameInputSelecting(SubNameInputSelectingEventArgs ev)
        {
            Metadata.BaseControlRoomProcessor.OnSubNameInputSelecting(ev);
            Metadata.HoverpadProcessor.OnSubNameInputSelecting(ev);
            Metadata.MoonpoolProcessor.OnSubNameInputSelecting(ev);
        }

        public void OnBaseControlRoomMinimapUsing(BaseControlRoomMinimapUsingEventArgs ev)
        {
            Metadata.BaseControlRoomProcessor.OnBaseControlRoomMinimapUsing(ev);
        }

        public void OnBaseControlRoomMinimapExiting(BaseControlRoomMinimapExitingEventArgs ev)
        {
            Metadata.BaseControlRoomProcessor.OnBaseControlRoomMinimapExiting(ev);
        }

        public void OnBaseControlRoomCellPowerChanging(BaseControlRoomCellPowerChangingEventArgs ev)
        {
            Metadata.BaseControlRoomProcessor.OnBaseControlRoomCellPowerChanging(ev);
        }

        public void OnBaseControlRoomMinimapMoving(BaseControlRoomMinimapMovingEventArgs ev)
        {
            Metadata.BaseControlRoomProcessor.OnBaseControlRoomMinimapMoving(ev);
        }

        public void OnConstructorDeploying(ConstructorDeployingEventArgs ev)
        {
            Items.ConstructorProcessor.OnConstructorDeploying(ev);
        }

        public void OnConstructorEngageToggle(ConstructorEngageToggleEventArgs ev)
        {
            Items.ConstructorProcessor.OnConstructorEngageToggle(ev);
        }

        public void OnConstructorCrafting(ConstructorCraftingEventArgs ev)
        {
            Items.ConstructorProcessor.OnConstructorCrafting(ev);
        }

        public void OnPlayerClimbing(PlayerClimbingEventArgs ev)
        {
            Player.ClimbProcessor.OnPlayerClimbing(ev);
        }

        public void OnUpgradeConsoleOpening(UpgradeConsoleOpeningEventArgs ev)
        {
            Vehicle.UpgradeConsoleProcessor.OnUpgradeConsoleOpening(ev);
        }

        public void OnUpgradeConsoleModuleAdded(UpgradeConsoleModuleAddedEventArgs ev)
        {
            Vehicle.UpgradeConsoleProcessor.OnUpgradeConsoleModuleAdded(ev);
        }

        public void OnUpgradeConsoleModuleRemoved(UpgradeConsoleModuleRemovedEventArgs ev)
        {
            Vehicle.UpgradeConsoleProcessor.OnUpgradeConsoleModuleRemoved(ev);
        }

        public void OnHoverpadDocking(HoverpadDockingEventArgs ev)
        {
            Metadata.HoverpadProcessor.OnHoverpadDocking(ev);
        }

        public void OnHoverpadUnDocking(HoverpadUnDockingEventArgs ev)
        {
            Metadata.HoverpadProcessor.OnHoverpadUnDocking(ev);
        }

        public void OnHoverpadShowroomTriggering(HoverpadShowroomTriggeringEventArgs ev)
        {
            Metadata.HoverpadProcessor.OnHoverpadShowroomTriggering(ev);
        }

        public void OnVehicleEntering(VehicleEnteringEventArgs ev)
        {
            Vehicle.EnterProcessor.OnVehicleEntering(ev);
        }

        public void OnVehicleExited(VehicleExitedEventArgs ev)
        {
            Vehicle.ExitProcessor.OnVehicleExited(ev);
        }

        public void OnVehicleUpdated(VehicleUpdatedEventArgs ev)
        {
            Vehicle.UpdatedProcessor.OnVehicleUpdated(ev);
        }

        public void OnPlayerDead(PlayerDeadEventArgs ev)
        {
            Player.DeadProcessor.OnPlayerDead(ev);
        }

        public void OnPlayerSpawned()
        {
            Player.SpawnProcessor.OnPlayerSpawned();
        }

        public void OnHoverbikeDeploying(HoverbikeDeployingEventArgs ev)
        {
            Items.HoverbikeProcessor.OnHoverbikeDeploying(ev);
        }

        public void OnEnergyMixinSelecting(EnergyMixinSelectingEventArgs ev)
        {
            Vehicle.BatteryProcessor.OnEnergyMixinSelecting(ev);
        }

        public void OnEnergyMixinClicking(EnergyMixinClickingEventArgs ev)
        {
            Vehicle.BatteryProcessor.OnEnergyMixinClicking(ev);
        }

        public void OnExosuitJumping(ExosuitJumpingEventArgs ev)
        {
            Vehicle.ExosuitJumpProcessor.OnExosuitJumping(ev);
        }

        public void OnEnergyMixinClosed(EnergyMixinClosedEventArgs ev)
        {
            General.InteractProcessor.OnEnergyMixinClosed(ev);
        }

        public void OnDeployableStorageDeploying(DeployableStorageDeployingEventArgs ev)
        {
            Items.DeployableStorageProcessor.OnDeployableStorageDeploying(ev);
        }

        public void OnLEDLightDeploying(LEDLightDeployingEventArgs ev)
        {
            Items.LEDLightProcessor.OnLEDLightDeploying(ev);
        }

        public void OnBeaconDeploying(BeaconDeployingEventArgs ev)
        {
            Items.BeaconProcessor.OnBeaconDeploying(ev);
        }

        public void OnBeaconLabelChanged(BeaconLabelChangedEventArgs ev)
        {
            Items.BeaconProcessor.OnBeaconLabelChanged(ev);
        }

        public void OnSpyPenguinDeploying(SpyPenguinDeployingEventArgs ev)
        {
            Items.SpyPenguinProcessor.OnSpyPenguinDeploying(ev);
        }

        public void OnSpyPenguinItemPickedUp(SpyPenguinItemPickedUpEventArgs ev)
        {
            Items.SpyPenguinProcessor.OnSpyPenguinItemPickedUp(ev);
        }

        public void OnSpyPenguinSnowStalkerInteracting(SpyPenguinSnowStalkerInteractingEventArgs ev)
        {
            Items.SpyPenguinProcessor.OnSpyPenguinSnowStalkerInteracting(ev);
        }

        public void OnSpyPenguinItemGrabing(SpyPenguinItemGrabingEventArgs ev)
        {
            Vehicle.UpdatedProcessor.OnSpyPenguinItemGrabing(ev);
        }

        public void OnFlareDeploying(FlareDeployingEventArgs ev)
        {
            Items.FlareProcessor.OnFlareDeploying(ev);
        }

        public void OnThumperDeploying(ThumperDeployingEventArgs ev)
        {
            Items.ThumperProcessor.OnThumperDeploying(ev);
        }

        public void OnTeleportationToolUsed(TeleportationToolUsedEventArgs ev)
        {
            Items.TeleportationToolProcessor.OnTeleportationToolUsed(ev);
        }
        public void OnVehicleLightChanged(LightChangedEventArgs ev)
        {
            Vehicle.LightProcessor.OnVehicleLightChanged(ev);
        }

        public void OnVehicleInteriorToggle(VehicleInteriorToggleEventArgs ev)
        {
            Vehicle.InteriorProcessor.OnVehicleInteriorToggle(ev);
        }

        public void OnSeaTruckConnecting(SeaTruckConnectingEventArgs ev)
        {
            Vehicle.SeaTruckConnectionProcessor.OnSeaTruckConnecting(ev);
        }

        public void OnSeaTruckDetaching(SeaTruckDetachingEventArgs ev)
        {
            Vehicle.SeaTruckConnectionProcessor.OnSeaTruckDetaching(ev);
        }

        public void OnExosuitItemPickedUp(ExosuitItemPickedUpEventArgs ev)
        {
            Vehicle.ExosuitStorageProcessor.OnExosuitItemPickedUp(ev);
        }

        public void OnExosuitDrilling(ExosuitDrillingEventArgs ev)
        {
            Vehicle.ExosuitDrillProcessor.OnExosuitDrilling(ev);
        }

        public void OnVehicleDocking(VehicleDockingEventArgs ev)
        {
            Metadata.MoonpoolProcessor.OnVehicleDocking(ev);
            Vehicle.SeaTruckDockingModuleProcessor.OnVehicleDocking(ev);
        }

        public void OnVehicleUndocking(VehicleUndockingEventArgs ev)
        {
            Metadata.MoonpoolProcessor.OnVehicleUndocking(ev);
            Vehicle.SeaTruckDockingModuleProcessor.OnVehicleUndocking(ev);
        }

        public void OnSeaTruckPictureFrameOpening(SeaTruckPictureFrameOpeningEventArgs ev)
        {
            Vehicle.SeaTruckSleeperModuleProcessor.OnSeaTruckPictureFrameOpening(ev);
        }

        public void OnSeaTruckPictureFrameImageSelecting(SeaTruckPictureFrameImageSelectingEventArgs ev)
        {
            Vehicle.SeaTruckSleeperModuleProcessor.OnSeaTruckPictureFrameImageSelecting(ev);
        }

        public void OnMapRoomCameraDocking(MapRoomCameraDockingEventArgs ev)
        {
            Metadata.BaseMapRoomProcessor.OnMapRoomCameraDocking(ev);
        }

        public void OnSeaTruckModuleInitialized(SeaTruckModuleInitializedEventArgs ev)
        {
            Vehicle.SeaTruckSleeperModuleProcessor.OnSeaTruckModuleInitialized(ev);
            Vehicle.SeaTruckDockingModuleProcessor.OnSeaTruckModuleInitialized(ev);
        }

        public void OnBreakableResourceBreaking(BreakableResourceBreakingEventArgs ev)
        {
            World.EntitySlotSpawnProcessor.OnBreakableResourceBreaking(ev);
        }

        public void OnBridgeFluidClicking(BridgeFluidClickingEventArgs ev)
        {
            Story.BridgeProcessor.OnBridgeFluidClicking(ev);
        }

        public void OnBridgeTerminalClicking(BridgeTerminalClickingEventArgs ev)
        {
            Story.BridgeProcessor.OnBridgeTerminalClicking(ev);
        }

        public void OnBridgeInitialized(BridgeInitializedEventArgs ev)
        {
            Story.BridgeProcessor.OnBridgeInitialized(ev);
        }

        public void OnRadioTowerTOMUsing(RadioTowerTOMUsingEventArgs ev)
        {
            Story.RadioTowerProcessor.OnRadioTowerTOMUsing(ev);
        }

        public void OnStoryGoalTriggering(StoryGoalTriggeringEventArgs ev)
        {
            Story.TriggerProcessor.OnStoryGoalTriggering(ev);
        }

        public void OnStorySignalSpawning(StorySignalSpawningEventArgs ev)
        {
            Story.SignalProcessor.OnStorySignalSpawning(ev);
        }

        public void OnCinematicTriggering(CinematicTriggeringEventArgs ev)
        {
            Story.CinematicProcessor.OnCinematicTriggering(ev);
        }

        public void OnStoryCalling(StoryCallingEventArgs ev)
        {
            Story.CallProcessor.OnStoryCalling(ev);
        }

        public void OnStoryHandClicking(StoryHandClickingEventArgs ev)
        {
            Story.InteractProcessor.OnStoryHandClicking(ev);
        }

        public void OnStoryCinematicStarted(StoryCinematicStartedEventArgs ev)
        {
            Story.PlayerVisibilityProcessor.OnStoryCinematicStarted(ev);
        }

        public void OnStoryCinematicCompleted(StoryCinematicCompletedEventArgs ev)
        {
            Story.PlayerVisibilityProcessor.OnStoryCinematicCompleted(ev);
        }

        public void OnMobileExtractorMachineInitialized()
        {
            Story.FrozenCreatureProcessor.OnMobileExtractorMachineInitialized();
        }

        public void OnMobileExtractorMachineSampleAdding(MobileExtractorMachineSampleAddingEventArgs ev)
        {
            Story.FrozenCreatureProcessor.OnMobileExtractorMachineSampleAdding(ev);
        }

        public void OnMobileExtractorConsoleUsing(MobileExtractorConsoleUsingEventArgs ev)
        {
            Story.FrozenCreatureProcessor.OnMobileExtractorConsoleUsing(ev);
        }

        public void OnShieldBaseEnterTriggering(ShieldBaseEnterTriggeringEventArgs ev)
        {
            Story.ShieldBaseProcessor.OnShieldBaseEnterTriggering(ev);
        }

        public void OnLaserCutterUsing(LaserCutterEventArgs ev)
        {
            WorldEntities.LaserCutterProcessor.OnLaserCutterUsing(ev);
        }

        public void OnSealedInitialized(SealedInitializedEventArgs ev)
        {
            WorldEntities.LaserCutterProcessor.OnSealedInitialized(ev);
        }

        public void OnElevatorCalling(ElevatorCallingEventArgs ev)
        {
            WorldEntities.ElevatorProcessor.OnElevatorCalling(ev);
        }

        public void OnSpawnOnKilling(SpawnOnKillingEventArgs ev)
        {
            World.SpawnOnKillProcessor.OnSpawnOnKilling(ev);
        }

        public void OnWeatherProfileChanged(WeatherProfileChangedEventArgs ev)
        {
            World.WeatherProcessor.OnWeatherProfileChanged(ev);
        }

        public void OnCrushDamaging(CrushDamagingEventArgs ev)
        {
            Vehicle.HealthProcessor.OnCrushDamaging(ev);
        }

        public void OnCosmeticItemPlacing(CosmeticItemPlacingEventArgs ev)
        {
            World.CosmeticItemProcessor.OnCosmeticItemPlacing(ev);
        }

        public void OnPlayerFreezed(PlayerFreezedEventArgs ev)
        {
            Player.FreezeProcessor.OnPlayerFreezed(ev);
        }

        public void OnPlayerUnfreezed()
        {
            Player.FreezeProcessor.OnPlayerUnfreezed();
        }

        public void OnCreatureAnimationChanged(CreatureAnimationChangedEventArgs ev)
        {
            Creatures.AnimationChangedProcessor.OnCreatureAnimationChanged(ev);
        }

        public void OnGlowWhaleRideStarting(GlowWhaleRideStartingEventArgs ev)
        {
            Creatures.GlowWhaleProcessor.OnGlowWhaleRideStarting(ev);
        }

        public void OnGlowWhaleRideStoped(GlowWhaleRideStopedEventArgs ev)
        {
            Creatures.GlowWhaleProcessor.OnGlowWhaleRideStoped(ev);
        }

        public void OnGlowWhaleEyeCinematicStarting(GlowWhaleEyeCinematicStartingEventArgs ev)
        {
            Creatures.GlowWhaleProcessor.OnGlowWhaleEyeCinematicStarting(ev);
        }

        public void OnGlowWhaleSFXTriggered(GlowWhaleSFXTriggeredEventArgs ev)
        {
            Creatures.GlowWhaleProcessor.OnGlowWhaleSFXTriggered(ev);
        }

        public void OnCrashFishInflating(CrashFishInflatingEventArgs ev)
        {
            Creatures.CrashFishProcessor.OnCrashFishInflating(ev);
        }

        public void OnLilyPaddlerHypnotizeStarting(LilyPaddlerHypnotizeStartingEventArgs ev)
        {
            Creatures.LilyPaddlerProcessor.OnLilyPaddlerHypnotizeStarting(ev);
        }

        public void OnFreezing(CreatureFreezingEventArgs ev)
        {
            Creatures.FreezeProcessor.OnFreezing(ev);
        }

        public void OnCallSoundTriggering(CreatureCallSoundTriggeringEventArgs ev)
        {
            Creatures.CallSoundProcessor.OnCallSoundTriggering(ev);
        }

        public void OnCreatureAttackLastTargetStarting(CreatureAttackLastTargetStartingEventArgs ev)
        {
            Creatures.AttackLastTargetProcessor.OnCreatureAttackLastTargetStarting(ev);
        }

        public void OnCreatureAttackLastTargetStopped(CreatureAttackLastTargetStoppedEventArgs ev)
        {
            Creatures.AttackLastTargetProcessor.OnCreatureAttackLastTargetStopped(ev);
        }

        public void OnLeviathanMeleeAttacking(CreatureLeviathanMeleeAttackingEventArgs ev)
        {
            Creatures.LeviathanMeleeAttackProcessor.OnLeviathanMeleeAttacking(ev);
        }

        public void OnMeleeAttacking(CreatureMeleeAttackingEventArgs ev)
        {
            Creatures.MeleeAttackProcessor.OnMeleeAttacking(ev);
        }

        public void OnCreatureEnabled(CreatureEnabledEventArgs ev)
        {
            Creatures.VoidLeviathanProcessor.OnCreatureEnabled(ev);
        }

        public void OnCreatureDisabled(CreatureDisabledEventArgs ev)
        {
            Creatures.VoidLeviathanProcessor.OnCreatureDisabled(ev);
        }
    }
}