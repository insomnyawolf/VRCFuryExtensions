using UnityEditor;
using UnityEngine.UIElements;
using VF.Feature.Base;
using VF.Inspector;
using VF.Utils;
using VF.Utils.Controller;
using VF.Model.Feature;
using System;
using VF.Service;
using VF.Injector;
using UnityEngine;
using VF.Builder;
using System.Security.Policy;

namespace VF.Feature
{
    internal class EmojiControlBuilder : CustomFeatureBuilder<EmojiControl>
    {
        [VFAutowired] private readonly AvatarManager AvatarManager;

        // Create Togle State Layers
        [VFAutowired] private readonly ActionClipService ActionClipService;

        // Rewrite the menues
        [VFAutowired] private readonly MenuChangesService MenuChangesService;

        // Apply on upload
        [VFAutowired] private readonly RestingStateService RestingStateService;


        private EditorCurveBinding ChangeSpriteBinding;

        private EditorCurveBinding EnableBinding;

        private AnimationCurve EnableCurve;

        private EmojiHelper EmojiHelper;

        private ParticleSystemRenderer ParticleSystemRenderer;

        [FeatureBuilderAction(FeatureOrder.ApplyDuringUpload)]
        public void DisableParticleSystemByDefault()
        {
            if (model.ParticleSystem is not ParticleSystem ParticleSystem)
            {
                // Error gordo aqui
                throw new Exception();
            }

            CurrentParamName = $"{GetInternalIdString()} PreConfig #{uniqueModelNum}";

            var relativePath = ParticleSystem.GetRelativePath();

            ChangeSpriteBinding = new EditorCurveBinding()
            {
                path = relativePath,
                propertyName = "UVModule.startFrame.scalar",
                type = typeof(ParticleSystem)
            };

            EnableBinding = new EditorCurveBinding()
            {
                path = relativePath,
                propertyName = "m_IsActive",
                type = typeof(GameObject)
            };

            var clip = new AnimationClip()
            {
                hideFlags = HideFlags.None,
                name = $"{model.MenuPath}_DefaultState",
                legacy = false,
            };

            EnableCurve = AnimationCurve.Constant(0, model.Duration, 1);

            var disabledCurve = AnimationCurve.Constant(0, 0, 0);

            clip.SetCurve(EnableBinding, disabledCurve);

            RestingStateService.ApplyClipToRestingState(clip);
        }

        [FeatureBuilderAction(FeatureOrder.Default)]
        public void ApplyToggles()
        {
            UpdateMenuIcon();

            PrepareParticleSystem();

            foreach (var item in model.Emojis)
            {
                uniqueModelNum++;

                ApplyOneToggle(item);
            }
        }

        public void PrepareParticleSystem()
        {
            var ps = model.ParticleSystem;

            var main = ps.main;

            main.stopAction = ParticleSystemStopAction.Disable;

            main.duration = 0;
            main.startDelay = 0;
            main.startLifetime = model.Duration;

            var tsa = ps.textureSheetAnimation;

            tsa.mode = ParticleSystemAnimationMode.Sprites;
            tsa.frameOverTime = 0;

            EmojiHelper = GetPackedEmojisAndCalculateUV(model);

            var sprites = EmojiHelper.Sprites;

            for (var i = 0; i < sprites.Length; i++)
            {
                var current = sprites[i];
                tsa.AddSprite(current);
            }

            CreateAnimationClips(model, sprites);

            ParticleSystemRenderer = ps.GetComponent<ParticleSystemRenderer>();

            ParticleSystemRenderer.material.mainTexture = EmojiHelper.CombinedTexture;
        }

        //[FeatureBuilderAction(FeatureOrder.RemoveJunkAnimators)]
        //// Weird Bypass to avoid saving the renderer material twice...
        //public void CleanupRenderer()
        //{
        //    ParticleSystemRenderer.material.mainTexture = null;
        //}

        //[FeatureBuilderAction(FeatureOrder.ResetAnimatorAfter)]
        //// Weird Bypass to avoid saving the renderer material twice...
        //public void RestoreRenderer()
        //{
        //    ParticleSystemRenderer.material.mainTexture = EmojiHelper.CombinedTexture;
        //}

        internal EmojiHelper GetPackedEmojisAndCalculateUV(EmojiControl emojiControl)
        {
            var emojis = emojiControl.Emojis;
            var count = emojis.Count;

            var originals = new Texture2D[count];

            for (int i = 0; i < count; i++)
            {
                var em = emojis[i];

                var texture = em.Icon;

                texture.ForceReadable();

                originals[i] = texture;
            }

            var packed = new Texture2D(0, 0)
            {
                name = $"TEMP_{GetInternalIdString()}_{emojiControl.MenuPath}_Main",
            };

            var uvs = packed.PackTextures(textures: originals, padding: 0, maximumAtlasSize: 4096, makeNoLongerReadable: false);

            // That's needed for live builds, else it won't e able to play the requiered sprite

            VRCFuryAssetDatabase.SaveAsset(packed, AvatarManager.tmpDir, packed.name);

            var workaroundCount = count;

            while (workaroundCount % 10 != 0)
            {
                workaroundCount++;
            }

            var sprites = new Sprite[workaroundCount];

            

            var middle = Vector2.one / 2;

            Sprite lastForWorkaround = null;

            for (int i = 0; i < count; i++)
            {
                var uv = uvs[i];

                var em = emojis[i];

                var section = packed.UvToCanvasSection(uv);

                var sprite = Sprite.Create(texture: packed, rect: section, pivot: middle);

                sprite.name = $"{GetInternalIdString()}_{emojiControl.MenuPath}_{em.Name}";

                // That's needed for live builds, else it won't
                // be able to create the requiered sprites
                VRCFuryAssetDatabase.SaveAsset(sprite, AvatarManager.tmpDir, sprite.name);

                em.Sprite = sprite;
                sprites[i] = sprite;
                lastForWorkaround = sprite;
            }

            for (int i = count; i < sprites.Length; i++)
            {
                sprites[i] = lastForWorkaround;
            }

            //AssetDatabase.Refresh();

            var result = new EmojiHelper()
            {
                CombinedTexture = packed,
                Sprites = sprites,
            };

            return result;

        }

        internal void CreateAnimationClips(EmojiControl emojiControl, Sprite[] sprites)
        {
            var emojis = emojiControl.Emojis;

            var frameIndexVariation = 1f / sprites.Length;

            var currentFrameIndex = 0f;

            for (int i = 0; i < emojis.Count; i++)
            {
                Emoji emoji = emojis[i];

                var clip = new AnimationClip()
                {
                    hideFlags = HideFlags.None,
                    name = $"{emojiControl.MenuPath}/{emoji.Name}",
                    legacy = false,
                };

                emoji.CalculatedAnimation = clip;

                // Change mapped sprite

                var spriteRef = AnimationCurve.Constant(0, 0, currentFrameIndex);

                currentFrameIndex += frameIndexVariation;

                //var spriteRef = new ObjectReferenceKeyframe[]
                //{
                //    new ObjectReferenceKeyframe
                //    {
                //        time = 0,
                //        value = emoji.Sprite,
                //    }
                //};

                clip.SetCurve(ChangeSpriteBinding, spriteRef);

                clip.SetCurve(EnableBinding, EnableCurve);
            }
        }

        public void UpdateMenuIcon()
        {
            if (model.Icon is not null)
            {
                // Changes the menu icon if one was provided
                var newIcon = new SetIcon()
                {
                    path = model.MenuPath,
                    icon = model.Icon,
                };

                MenuChangesService.AddExtraAction(newIcon);
            }
        }

        private void ApplyOneToggle(Emoji emoji)
        {
            var fx = GetFx();

            var fullMenuPath = $"{model.MenuPath}/{emoji.Name}";

            var defaultOn = false;

            CurrentParamName = $"{GetInternalIdString()} {fullMenuPath} #{uniqueModelNum}";

            var param = fx.NewBool(
                fullMenuPath,
                synced: true,
                saved: false,
                def: defaultOn,
                usePrefix: UsePrefixOnParam
                );

            VFCondition onCase = param.IsTrue();

            Action<VFState, bool> drive = (state, on) => state.Drives(param, on ? 1 : 0);

            manager.GetMenu().NewMenuButton(
                    path: fullMenuPath,
                    param: param,
                    icon: emoji.Icon
                );

            var layer = fx.NewLayer(fullMenuPath);
            var off = layer.NewState("Off");

            var state = new Model.State();

            state.actions.Add(emoji.GetAction());

            ApplyOneToggleInternal(layer, off, "On", onCase, state);
        }

        private void ApplyOneToggleInternal(
            VFLayer layer,
            VFState off,
            string onName,
            VFCondition onCase,
            Model.State state
        )
        {
            var clip = ActionClipService.LoadState(onName, state);

            VFState on = layer.NewState(onName).WithAnimation(clip);

            off.TransitionsTo(on).When(onCase);

            on.TransitionsTo(off).When(onCase.Not()).WithTransitionExitTime(1);
        }

        public override VisualElement CreateEditor(SerializedProperty prop)
        {
            var content = new VisualElement();

            //var flex0 = new VisualElement().Row();
            //content.Add(flex0);
            //flex0.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.DebugSprite)), "DebugSprite", tooltip: null).FlexGrow(1));

            var flex1 = new VisualElement().Row();
            content.Add(flex1);
            flex1.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.MenuPath)), "Menu Path", tooltip: ToggleBuilder.menuPathTooltip).FlexGrow(1));

            var flex3 = new VisualElement().Row();
            content.Add(flex3);
            flex3.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.Icon)), "Main Icon", tooltip: null).FlexGrow(1));

            var flex2 = new VisualElement().Row();
            content.Add(flex2);
            flex2.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.ParticleSystem)), "Particle System", tooltip: null).FlexGrow(1));

            var flex4 = new VisualElement().Row();
            content.Add(flex4);
            flex4.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.Duration)), "Duration", tooltip: "How much in seconds will the emoji appear").FlexGrow(1));

            content.Add(VRCFuryEditorUtils.WrappedLabel("Emojis:"));
            content.Add(VRCFuryEditorUtils.List(prop.FindPropertyRelative(nameof(EmojiControl.Emojis))));

            return content;
        }
    }
}