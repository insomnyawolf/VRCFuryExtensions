using System;
using System.Collections.Generic;
using VF.Model.StateAction;

namespace VF.Model.Feature
{
    [Serializable]
    internal class EmojiControl : NewFeatureModel
    {
        public string MenuPath = "";
        public GuidTexture2d Icon;
        public List<Emoji> Emojis;
    }

    [Serializable]
    internal class Emoji
    {
        public string Name = "";
        public GuidTexture2d Icon;
        public GuidAnimationClip Animation;

        public AnimationClipAction GetAction()
        {
            return new AnimationClipAction()
            {
                androidActive = true,
                desktopActive = true,
                clip = Animation,
            };
        }
    }
}