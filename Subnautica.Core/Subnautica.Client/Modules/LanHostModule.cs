namespace Subnautica.Client.Modules
{
    using Subnautica.Client.Core;
    using Subnautica.API.Features;
    using Subnautica.Events.EventArgs;

    using TMPro;

    using UnityEngine;
    using UnityEngine.UI;

    public static class LanHostModule
    {
        /**
         *
         * Oyun içi menü açılırken tetiklenir.
         * Host için, arkadaşların bağlanacağı IP adreslerini gösteren butonu ekler.
         *
         */
        public static void OnInGameMenuOpened(InGameMenuOpenedEventArgs ev)
        {
            if (Network.IsMultiplayerActive && Network.IsHost)
            {
                var feedbackBtn = IngameMenu.main.transform.Find("Main/ButtonLayout/ButtonFeedback").gameObject;
                if (feedbackBtn.activeSelf)
                {
                    CreateServerAddressButtons();

                    IngameMenu.main.feedbackButton.gameObject.SetActive(false);
                    IngameMenu.main.transform.Find("Main/ButtonLayout/ButtonFeedback").gameObject.SetActive(false);
                }

                foreach (var item in IngameMenu.main.helpButton.transform.parent.gameObject.GetComponentsInChildren<Button>())
                {
                    if (item.name.Contains("ServerAddressTextButton"))
                    {
                        item.GetComponentInChildren<TextMeshProUGUI>().text = string.Empty;
                    }
                }
            }
        }

        /**
         *
         * Sunucu adresi butonlarını oluşturur.
         *
         */
        private static void CreateServerAddressButtons()
        {
            var showButton = GameObject.Instantiate(IngameMenu.main.helpButton.gameObject, IngameMenu.main.helpButton.transform.parent);
            var textButton = GameObject.Instantiate(IngameMenu.main.helpButton.gameObject, IngameMenu.main.helpButton.transform.parent);
            showButton.gameObject.name = "ServerAddressShowButton";
            textButton.gameObject.name = "ServerAddressTextButton";

            showButton.SetActive(true);
            textButton.SetActive(true);

            showButton.GetComponentInChildren<TextMeshProUGUI>().text = ZeroLanguage.Get("GAME_SHOW_SERVER_IP", "Show Server IP");
            textButton.GetComponentInChildren<TextMeshProUGUI>().text = "";

            textButton.GetComponent<RectTransform>().SetAsFirstSibling();
            showButton.GetComponent<RectTransform>().SetAsFirstSibling();

            if (textButton.TryGetComponent<Button>(out var textBtn))
            {
                textBtn.enabled = false;
                textBtn.GetComponent<Image>().enabled = false;
            }

            if (showButton.TryGetComponent<Button>(out var showBtn))
            {
                showBtn.onClick = new Button.ButtonClickedEvent();
                showBtn.onClick.AddListener(() => {
                    textButton.GetComponentInChildren<TextMeshProUGUI>().text = LanHost.GetJoinAddressText(NetworkServer.DefaultPort);
                });
            }
        }
    }
}
