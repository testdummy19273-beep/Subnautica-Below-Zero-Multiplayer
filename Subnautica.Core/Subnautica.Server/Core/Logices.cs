namespace Subnautica.Server.Core
{
    using Subnautica.API.Extensions;
    using Subnautica.API.Features;
    using Subnautica.Server.Abstracts;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Timers;
    using UnityEngine;

    public class Logices : MonoBehaviour
    {
        private List<BaseLogic> StartList { get; set; } = new List<BaseLogic>();

        private List<BaseLogic> UpdateList { get; set; } = new List<BaseLogic>();

        private List<BaseLogic> AsyncUpdateList { get; set; } = new List<BaseLogic>();

        private List<BaseLogic> FixedUpdateList { get; set; } = new List<BaseLogic>();

        private List<BaseLogic> UnscaledFixedUpdateList { get; set; } = new List<BaseLogic>();

        private WaitForSecondsRealtime UnscaledFixedRealTime { get; set; } = new WaitForSecondsRealtime(0.1f);

        private Timer Timer { get; set; }


        public void Awake()
        {
            this.hideFlags = HideFlags.HideAndDontSave;

            this.Timer = new Timer();
            this.Timer.Elapsed += this.OnAsyncUpdate;
            this.Timer.Interval = 250;
            this.Timer.Start();

            DontDestroyOnLoad(this);

            foreach (var property in this.GetType().GetProperties())
            {
                var logic = property.GetValue(this, null) as BaseLogic;
                if (logic != null)
                {
                    var assemblyType = logic.GetType();
                    if (assemblyType.GetMethod("OnStart").IsOverride())
                    {
                        this.StartList.Add(logic);
                    }

                    if (assemblyType.GetMethod("OnUpdate").IsOverride())
                    {
                        this.UpdateList.Add(logic);
                    }

                    if (assemblyType.GetMethod("OnAsyncUpdate").IsOverride())
                    {
                        this.AsyncUpdateList.Add(logic);
                    }

                    if (assemblyType.GetMethod("OnFixedUpdate").IsOverride())
                    {
                        this.FixedUpdateList.Add(logic);
                    }

                    if (assemblyType.GetMethod("OnUnscaledFixedUpdate").IsOverride())
                    {
                        this.UnscaledFixedUpdateList.Add(logic);
                    }
                }
            }

            this.StartCoroutine(this.UnscaledFixedUpdate());
        }

        private void OnAsyncUpdate(object sender, ElapsedEventArgs e)
        {
            try
            {
                foreach (var logic in this.AsyncUpdateList)
                {
                    logic.OnAsyncUpdate();
                }
            }
            catch (Exception ex)
            {
                Log.Info($"Logices.Timer_Elapsed Exception: {ex}");
            }
        }

        public void Start()
        {
            try
            {
                foreach (var logic in this.StartList)
                {
                    logic.OnStart();
                }
            }
            catch (Exception e)
            {
                Log.Info($"Logices.Start Exception: {e}");
            }
        }

        public void Update()
        {
            try
            {
                foreach (var logic in this.UpdateList)
                {
                    logic.OnUpdate(Time.deltaTime);
                }
            }
            catch (Exception e)
            {
                Log.Info($"Logices.Update Exception: {e}");
            }
        }

        public void FixedUpdate()
        {
            try
            {
                foreach (var logic in this.FixedUpdateList)
                {
                    logic.OnFixedUpdate(Time.fixedDeltaTime);
                }
            }
            catch (Exception e)
            {
                Log.Info($"Logices.FixedUpdate Exception: {e}");
            }
        }

        public IEnumerator UnscaledFixedUpdate()
        {
            while (true)
            {
                yield return this.UnscaledFixedRealTime;

                try
                {
                    foreach (var logic in this.UnscaledFixedUpdateList)
                    {
                        logic.OnUnscaledFixedUpdate(Time.fixedUnscaledDeltaTime);
                    }
                }
                catch (Exception e)
                {
                    Log.Info($"Logices.FixedUpdate Exception: {e}");
                }
            }
        }

        public void OnDestroy()
        {
            this.StartList.Clear();
            this.UpdateList.Clear();
            this.AsyncUpdateList.Clear();
            this.FixedUpdateList.Clear();
            this.UnscaledFixedUpdateList.Clear();

            this.Timer.Dispose();
            this.Timer = null;
            this.Storage = null;
            this.AutoSave = null;
            this.World = null;
            this.Interact = null;
            this.CreatureWatcher = null;
            this.EnergyTransmission = null;
            this.PowerConsumer = null;
            this.BaseHullStrength = null;
            this.WorldStreamer = null;
            this.EntityWatcher = null;
            this.VehicleEnergyTransmission = null;
            this.EnergyMixinTransmission = null;
            this.SeaTruckAquarium = null;
            this.Bed = null;
            this.Bench = null;
            this.Jukebox = null;
            this.BatteryCharger = null;
            this.CoffeeVendingMachine = null;
            this.Fridge = null;
            this.FiltrationMachine = null;
            this.SpotLight = null;
            this.TechLight = null;
            this.Crafter = null;
            this.Hoverpad = null;
            this.Moonpool = null;
            this.BaseMapRoom = null;
            this.StoryTrigger = null;
            this.PlayerJoin = null;
            this.Weather = null;
            this.Timing = null;
            this.VoidLeviathan = null;
        }

        public Logic.Storage Storage { get; set; } = new Logic.Storage();

        public Logic.AutoSave AutoSave { get; set; } = new Logic.AutoSave();

        public Logic.World World { get; set; } = new Logic.World();

        public Logic.Interact Interact { get; set; } = new Logic.Interact();

        public Logic.CreatureWatcher CreatureWatcher { get; set; } = new Logic.CreatureWatcher();

        public Logic.EnergyTransmission EnergyTransmission { get; set; } = new Logic.EnergyTransmission();

        public Logic.PowerConsumer PowerConsumer { get; set; } = new Logic.PowerConsumer();

        public Logic.BaseHullStrength BaseHullStrength { get; set; } = new Logic.BaseHullStrength();

        public Logic.WorldStreamer WorldStreamer { get; set; } = new Logic.WorldStreamer();

        public Logic.EntityWatcher EntityWatcher { get; set; } = new Logic.EntityWatcher();

        public Logic.VehicleEnergyTransmission VehicleEnergyTransmission { get; set; } = new Logic.VehicleEnergyTransmission();

        public Logic.EnergyMixinTransmission EnergyMixinTransmission { get; set; } = new Logic.EnergyMixinTransmission();

        public Logic.SeaTruckAquarium SeaTruckAquarium { get; set; } = new Logic.SeaTruckAquarium();

        public Logic.Furnitures.Bed Bed { get; set; } = new Logic.Furnitures.Bed();

        public Logic.Furnitures.Bench Bench { get; set; } = new Logic.Furnitures.Bench();

        public Logic.Furnitures.Jukebox Jukebox { get; set; } = new Logic.Furnitures.Jukebox();

        public Logic.Furnitures.BatteryCharger BatteryCharger { get; set; } = new Logic.Furnitures.BatteryCharger();

        public Logic.Furnitures.CoffeeVendingMachine CoffeeVendingMachine { get; set; } = new Logic.Furnitures.CoffeeVendingMachine();

        public Logic.Furnitures.Fridge Fridge { get; set; } = new Logic.Furnitures.Fridge();

        public Logic.Furnitures.FiltrationMachine FiltrationMachine { get; set; } = new Logic.Furnitures.FiltrationMachine();

        public Logic.Furnitures.BaseWaterPark BaseWaterPark { get; set; } = new();

        public Logic.Furnitures.SpotLight SpotLight { get; set; } = new Logic.Furnitures.SpotLight();

        public Logic.Furnitures.TechLight TechLight { get; set; } = new Logic.Furnitures.TechLight();

        public Logic.Furnitures.Crafter Crafter { get; set; } = new Logic.Furnitures.Crafter();

        public Logic.Furnitures.Hoverpad Hoverpad { get; set; } = new Logic.Furnitures.Hoverpad();

        public Logic.Furnitures.Moonpool Moonpool { get; set; } = new Logic.Furnitures.Moonpool();

        public Logic.Furnitures.BaseMapRoom BaseMapRoom { get; set; } = new Logic.Furnitures.BaseMapRoom();

        public Logic.StoryTrigger StoryTrigger { get; set; } = new Logic.StoryTrigger();

        public Logic.PlayerJoin PlayerJoin { get; set; } = new Logic.PlayerJoin();

        public Logic.Weather Weather { get; set; } = new Logic.Weather();

        public Logic.Timing Timing { get; set; } = new Logic.Timing();


        public Logic.VoidLeviathan VoidLeviathan { get; set; } = new Logic.VoidLeviathan();
    }
}
