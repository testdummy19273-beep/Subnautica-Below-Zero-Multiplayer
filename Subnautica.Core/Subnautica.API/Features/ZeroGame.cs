namespace Subnautica.API.Features
{
    using System;
    using System.Collections;
    using System.IO;

    using TMPro;

    using UnityEngine;

    using UWE;

    public class ZeroGame
    {
        private static GameObject ErrorMessageObject { get; set; }

        public static void QuitToMainMenu()
        {
            UWE.CoroutineHost.StartCoroutine(ZeroGame.QuitToMainMenuAsync());
        }

        private static IEnumerator QuitToMainMenuAsync()
        {
            UWE.Utils.lockCursor = true;

            uGUI_LegendBar.ClearButtons();

            if (IngameMenu.main)
            {
                IngameMenu.main.isQuitting = true;
                IngameMenu.main.Close();
            }

            yield return CoroutineUtils.waitForNextFrame;

            UWE.Utils.lockCursor = false;

            yield return IngameMenu.QuitToMainMenuAsync();
        }

        public static void RunInBackgroundChange(bool isActive)
        {
            MiscSettings.runInBackground = isActive;
            MiscSettings.pdaPause = !isActive;
            ApplicationFocus.OnRunInBackgroundChanged();
        }

        private static WaitScreen.ManualWaitItem LoadingItem { get; set; }

        public static void ShowLoadingScreen()
        {
            if (LoadingItem == null)
            {
                LoadingStage.durations["CreateServer"] = 15f;

                LoadingItem = WaitScreen.Add("CreateServer");
                LoadingItem?.SetProgress(0.4f);
            }
        }

        public static void StopLoadingScreen()
        {
            if (LoadingItem != null)
            {
                WaitScreen.Remove(LoadingItem);
                LoadingItem = null;
            }
        }

        public static void FreezeGame()
        {
            FreezeTime.Set(FreezeTime.Id.IngameMenu, 1f);
        }

        public static void UnFreezeGame()
        {
            FreezeTime.Set(FreezeTime.Id.IngameMenu, 0.0f);
        }

        public static Vector3 FindDropPosition(Vector3 dropPosition)
        {
            return Pickupable.FindDropPosition(global::MainCamera.camera.transform.position, dropPosition);
        }

        public static bool IsPlayerPiloting()
        {
            if (ZeroPlayer.CurrentPlayer == null || ZeroPlayer.CurrentPlayer.FreecamController.GetActive())
            {
                return false;
            }

            if (global::Player.main.isPiloting)
            {
                return true;
            }

            if (global::Player.main.mode == global::Player.Mode.LockedPiloting)
            {
                return true;
            }

            return false;
        }

        public static string GetSeaTruckColoredLabelUniqueId(string uniqueId, bool returnToDefaultKey = false)
        {
            if (returnToDefaultKey)
            {
                return "_ZeroLabel";
            }

            return string.Format("{0}_ZeroLabel", uniqueId);
        }

        public static string GetVehicleBatteryLabelUniqueId(string uniqueId, bool returnToDefaultKey = false)
        {
            if (returnToDefaultKey)
            {
                return "_ZeroBattery";
            }

            return string.Format("{0}_ZeroBattery", uniqueId);
        }

        public static bool ToggleClickSwitchOn(ToggleOnClick toggle, bool isSilence = false)
        {
            if (toggle.activeState)
            {
                return false;
            }

            toggle.activeState = true;
            toggle.timeLastClick = Time.time;

            if (!isSilence)
            {
                if (toggle.switchOnSound != null)
                {
                    toggle.switchOnSound.Play();
                }

                if (toggle.workSound != null)
                {
                    toggle.workSound.Play();
                }

                if (toggle.maxWorkDuration > 0.0f)
                {
                    UWE.CoroutineHost.StartCoroutine(DelayedToggleClickSwitchOff(toggle, toggle.maxWorkDuration));
                }
            }

            if (toggle.animator != null && !string.IsNullOrEmpty(toggle.workAnimation))
            {
                toggle.animator.SetBool(toggle.workAnimation, true);
            }

            if (toggle.workVfx != null)
            {
                toggle.workVfx.Play();
            }

            toggle.onSwitchedOn.Invoke();
            return true;
        }

        private static IEnumerator DelayedToggleClickSwitchOff(ToggleOnClick toggle, float delayTime)
        {
            yield return new WaitForSecondsRealtime(delayTime);

            if (toggle != null)
            {
                ZeroGame.ToggleClickSwitchOff(toggle);
            }
        }

        public static bool ToggleClickSwitchOff(ToggleOnClick toggle, bool isSilence = false)
        {
            if (!toggle.activeState)
            {
                return false;
            }

            toggle.activeState = false;
            toggle.timeLastClick = Time.time;
            toggle.CancelInvoke(nameof(global::ToggleOnClick.SwitchOff));

            if (!isSilence)
            {
                if (toggle.switchOffSound != null)
                {
                    toggle.switchOffSound.Play();
                }

                if (toggle.workSound != null)
                {
                    toggle.workSound.Stop();
                }
            }

            if (toggle.animator != null && !string.IsNullOrEmpty(toggle.workAnimation))
            {
                toggle.animator.SetBool(toggle.workAnimation, false);
            }

            if (toggle.workVfx != null)
            {
                toggle.workVfx.Stop();
            }

            toggle.onSwitchedOff.Invoke();
            return true;
        }

        public static void SaveThumbnailToFile(string filePath, byte[] imageData)
        {
            Texture2D texture = ScreenshotManager.CreateScreenshotTextureFromBytes(imageData);
            texture = MathExtensions.ScaleTexture(texture, 256, false, ScreenshotManager.forceLinearTexture);

            File.WriteAllBytes(filePath, texture.EncodeToJPG());

            UnityEngine.Object.Destroy(texture);
        }

        public static ScreenshotManager.Thumbnail LoadThumbnailFromFile(string filePath)
        {
            ScreenshotManager.Thumbnail thumbnail = new ScreenshotManager.Thumbnail();
            thumbnail.lastWriteTimeUtc = DateTime.UtcNow;
            thumbnail.texture = ScreenshotManager.CreateScreenshotTextureFromBytes(File.ReadAllBytes(filePath));
            thumbnail.texture.name = "ScreenshotThumbnail";
            thumbnail.texture.Compress(true);

            return thumbnail;
        }

        public static bool Craft(global::GhostCrafter ghostCrafter, TechType techType, float startingTime, float duration, bool isMine)
        {
            if (ghostCrafter == null)
            {
                return false;
            }

            LogicCraft(ghostCrafter, techType, startingTime, duration);

            if (isMine)
            {
                CrafterLogic.ConsumeResources(techType);
            }

            ghostCrafter.state = true;
            ghostCrafter.OnCraftingBegin(techType, duration);

            if (DayNightCycle.main.timePassedAsFloat >= ghostCrafter.logic.timeCraftingEnd)
            {
                ghostCrafter.logic.NotifyEnd();
            }

            return true;
        }

        public static void LogicCraft(global::GhostCrafter ghostCrafter, TechType techType, float startingTime, float duration)
        {
            ghostCrafter.logic.timeCraftingBegin = startingTime;
            ghostCrafter.logic.timeCraftingEnd = ghostCrafter.logic.timeCraftingBegin + duration + 0.1f;
            ghostCrafter.logic.craftingTechType = techType;
            ghostCrafter.logic.linkedIndex = -1;
            ghostCrafter.logic.numCrafted = TechData.GetCraftAmount(techType);
            ghostCrafter.logic.NotifyChanged(ghostCrafter.logic.craftingTechType);
            ghostCrafter.logic.NotifyProgress(0f);
        }

        public static void SetLightsActive(ToggleLights toggleLights, bool isActive, bool infinityEnergy = false)
        {
            if (toggleLights.lightsActive != isActive)
            {
                if (infinityEnergy)
                {
                    if (toggleLights.energyMixin)
                    {
                        toggleLights.energyMixin = null;
                    }
                }

                toggleLights.IsToggleEnabled = true;
                toggleLights.lightsActive = isActive;
                toggleLights.lightsParent.SetActive(isActive);
            }
        }

        public static void SetLightsActive(SeaTruckLights seaTruckLights, bool isActive)
        {
            seaTruckLights.lightsActive = isActive;
        }

        public static void AddScreenErrorMessage(string message)
        {
            ClearScreenErrorMessage();

            ErrorMessageObject = GameObject.Instantiate<GameObject>(ErrorMessage.main.prefabMessage);

            TextMeshProUGUI component = ErrorMessageObject.GetComponent<TextMeshProUGUI>();
            component.rectTransform.SetParent(ErrorMessage.main.messageCanvas, false);
            ErrorMessageObject.SetActive(true);

            Vector3 position = component.rectTransform.localPosition;

            Rect rect = ErrorMessage.main.messageCanvas.rect;
            position.y = -(rect.y) - 25f;
            position.x = (rect.x) + 25f;

            component.text = string.Format("<color=red>{0}</color>", message);
        }

        public static void ClearScreenErrorMessage()
        {
            if (ErrorMessageObject != null)
            {
                GameObject.Destroy(ErrorMessageObject);
                ErrorMessageObject = null;
            }
        }
    }
}
