using System;
using System.Collections.Generic;
using UnityEngine;
using VF.Model.StateAction;

namespace VF.Model.Feature
{
    [Serializable]
    internal class EmojiControl : NewFeatureModel
    {
        public string MenuPath = "";
        public Texture2D Icon;
        public ParticleSystem ParticleSystem;
        public float Duration;
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

    internal class EmojiHelper
    {
        public Sprite[] Sprites { get; set; }
        public Texture2D CombinedTexture { get; set; }
    }
}