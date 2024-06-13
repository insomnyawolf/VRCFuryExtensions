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
using System.Collections.Generic;

namespace VF.Feature
{
    internal class EmojiControlBuilder : CustomFeatureBuilder<EmojiControl>
    {
        [VFAutowired] private readonly AvatarManager AvatarManager;
        [VFAutowired] private readonly ActionClipService ActionClipService;
        [VFAutowired] private readonly MenuChangesService MenuChangesService;

        //[FeatureBuilderAction(FeatureOrder.ApplyDuringUpload)]
        public void PrepareParticleSystem()
        {
            if (model.ParticleSystem is null)
            {
                // Error gordo aqui
            }

            var ps = model.ParticleSystem;

            var tsa = ps.textureSheetAnimation;

            var helper = GetPackedEmojisAndCalculateUV(model);

            var sprites = helper.Sprites;

            for (var i = 0; i < sprites.Length; i++)
            {
                var current = sprites[i];
                tsa.AddSprite(current);
            }

            tsa.mode = ParticleSystemAnimationMode.Sprites;

            tsa.startFrame = 0;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();

            renderer.material.mainTexture = helper.CombinedTexture;

            var temp = ps.transform;

            while (temp?.parent?.transform is Transform parentTransform)
            {
                temp = parentTransform;
            }

            var relativePath = AnimationUtility.CalculateTransformPath(ps.transform, temp);

            CreateAnimationClips(model, ps.main.duration, relativePath);
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

            var flex1 = new VisualElement().Row();
            content.Add(flex1);
            flex1.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.MenuPath)), "Menu Path", tooltip: ToggleBuilder.menuPathTooltip).FlexGrow(1));

            var flex2 = new VisualElement().Row();
            content.Add(flex2);
            flex2.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.ParticleSystem)), "Particle System", tooltip: null).FlexGrow(1));

            var flex3 = new VisualElement().Row();
            content.Add(flex3);
            flex3.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.Icon)), "Main Icon", tooltip: null).FlexGrow(1));

            content.Add(VRCFuryEditorUtils.WrappedLabel("Emojis:"));
            content.Add(VRCFuryEditorUtils.List(prop.FindPropertyRelative(nameof(EmojiControl.Emojis))));

            return content;
        }

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

            var packed = new Texture2D(0, 0);

            var uvs = packed.PackTextures(textures: originals, padding: 1, maximumAtlasSize: 2048, makeNoLongerReadable: false);

            packed.ForceReadable();

            var sprites = new Sprite[count];

            var middle = Vector2.one / 2;

            for (int i = 0; i < count; i++)
            {
                var uv = uvs[i];
                var em = emojis[i];

                //(X = left, Y = bottom, Z = right, W = top).

                //#pragma warning disable CS0618 // Type or member is obsolete
                //                var border = new Vector4()
                //                {
                //                    w = uv.top,
                //                    x = uv.left,
                //                    y = uv.bottom,
                //                    z = uv.right,
                //                };
                //#pragma warning restore CS0618 // Type or member is obsolete

                //var border = Vector4.zero;

                // var sprite = Sprite.Create(texture: packed, rect: uv, pivot: middle, pixelsPerUnit: 100.0f, extrude: 0, meshType: SpriteMeshType.FullRect, border);
                var sprite = Sprite.Create(texture: packed, rect: uv, pivot: middle);

                sprite.name = $"{GetInternalIdString()}_{emojiControl.MenuPath}_{em.Name}";

                sprites[i] = sprite;
            }

            var result = new EmojiHelper()
            {
                CombinedTexture = packed,
                Sprites = sprites,
            };

            return result;
        }

        // https://qiita.com/RyotaMurohoshi/items/5cb865c23a50055cf92f
        internal void CreateAnimationClips(EmojiControl emojiControl, float length, string relativePath)
        {
            var emojis = emojiControl.Emojis;

            for (int i = 0; i < emojis.Count; i++)
            {
                var emoji = emojis[i];

                var clip = new AnimationClip()
                {
                    hideFlags = HideFlags.None, //HideFlags.NotEditable
                    name = $"{emojiControl.MenuPath}/{emoji.Name}",
                    legacy = false,
                };

                emoji.CalculatedAnimation = clip;

                var list = new List<(EditorCurveBinding, FloatOrObjectCurve)>(2);

                // Change mapped sprite
                var changeSpriteCurve = AnimationCurve.Constant(0, length, i);

                var changeSpriteBinding = new EditorCurveBinding()
                {
                    path = relativePath,
                    propertyName = "UVModule.startFrame.scalar",
                    type = typeof(ParticleSystemRenderer)
                };

                list.Add((changeSpriteBinding, changeSpriteCurve));

                // Turn On/ Off

                var enableBinding = new EditorCurveBinding()
                {
                    path = relativePath,
                    propertyName = "m_IsActive",
                    type = typeof(GameObject)
                };

                var keyframes = new Keyframe[3]
                {
                    new Keyframe(0, 1),
                    new Keyframe(length - 0.01f, 1),
                    new Keyframe(length, 0),
                };

                var enablePsCurve = new AnimationCurve(keyframes);

                list.Add((enableBinding, enablePsCurve));

                clip.SetCurves(list);
            }
        }
    }

    class EmojiHelper
    {
        public Sprite[] Sprites { get; set; }
        public Texture2D CombinedTexture { get; set; }
    }

}