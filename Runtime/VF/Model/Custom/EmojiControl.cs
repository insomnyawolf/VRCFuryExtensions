using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VF.Model.StateAction;

namespace VF.Model.Feature
{
    [Serializable]
    internal class EmojiControl : NewFeatureModel
    {
        public string MenuPath = "";
        public ParticleSystem ParticleSystem;
        public Texture2D Icon;
        public List<Emoji> Emojis;
    }

    

    [Serializable]
    internal class Emoji
    {
        public string Name = "";
        public Texture2D Icon;
        [NonSerialized]
        public Sprite Sprite;
        [NonSerialized]
        public AnimationClip CalculatedAnimation;

        public AnimationClipAction GetAction()
        {
            return new AnimationClipAction()
            {
                androidActive = true,
                desktopActive = true,
                clip = CalculatedAnimation,
            };
        }
    }
}