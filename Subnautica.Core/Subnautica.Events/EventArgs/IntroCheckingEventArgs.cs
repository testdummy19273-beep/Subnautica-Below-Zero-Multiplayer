namespace Subnautica.Events.EventArgs
{
    using System;
    using System.Collections;

    public class IntroCheckingEventArgs : EventArgs
    {
        public IntroCheckingEventArgs(bool isAllowed = true)
        {
            this.IsAllowed = isAllowed;
        }

        public IEnumerator WaitingMethod { get; set; }

        public bool IsPlaying { get; set; }

        public ExpansionIntroManager IntroManager { get; set; }

        public Action OnIntroDone { get; set; }

        public uGUI_SceneIntro Gui { get; set; }

        public bool IsAllowed { get; set; }
    }
}
