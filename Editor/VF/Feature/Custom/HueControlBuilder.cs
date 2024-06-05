using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VF.Feature.Base;
using VF.Injector;
using VF.Inspector;
using VF.Service;
using VF.Utils;
using VF.Utils.Controller;
using VF.Model.Feature;

namespace VF.Feature
{
    internal class HueControlBuilder : FeatureBuilder<HueControl>
    {
        public const string EditorTitle = "Custom " + nameof(HueControl);

        private const bool UsePrefixOnParam = true;
        private const bool HasIcon = false;
        private const bool IsSliderInactiveAtZero = false;
        private const bool IsLogicInverted = false;

        private static int HueControlInternalId = 0;

        public HueControlBuilder()
        {
            HueControlInternalId++;
        }

        public override string GetEditorTitle()
        {
            return EditorTitle;
        }

        private string GetInternalIdString()
        {
            return $"{EditorTitle} #{HueControlInternalId}";
        }

        // Apply on upload
        [VFAutowired] private readonly RestingStateService RestingStateService;

        // Toggle
        [VFAutowired] private readonly ClipRewriteService ClipRewriteService;
        [VFAutowired] private readonly FixWriteDefaultsBuilder FixWriteDefaultsBuilder;
        [VFAutowired] private readonly ClipFactoryService ClipFactoryService;

        // Apply on upload & Toggle
        [VFAutowired] private readonly ActionClipService ActionClipService;

        private readonly Dictionary<string, ToggleMaterialPropertyCache> GroupedMaterialPropertyCache = new Dictionary<string, ToggleMaterialPropertyCache>();
        private readonly List<BeforeUploadMaterialPropertyCache> BeforeUploadMaterialPropertyCache = new List<BeforeUploadMaterialPropertyCache>();

        [FeatureBuilderAction(FeatureOrder.ApplyDuringUpload)]
        public void ApplyApplyDuringUpload()
        {
            CacheMaterialProperties();

            var state = new Model.State();

            foreach (var item in BeforeUploadMaterialPropertyCache)
            {
                var property = item.GetActions();

                foreach (var renderer in property)
                {
                    state.actions.Add(renderer);
                }
            }

            CurrentParamName = $"{GetInternalIdString()} PreConfig #{uniqueModelNum}";

            var clip = ActionClipService.LoadState("applyDuringUpload", state);

            RestingStateService.ApplyClipToRestingState(clip);
        }

        private void CacheMaterialProperties()
        {
            var data = model;

            foreach (var material in data.Materials)
            {
                foreach (var prop in data.Configs)
                {
                    var path = $"{data.MenuPath}/{prop.OptionName ?? prop.PropertyName}";

                    var temp = new BeforeUploadMaterialPropertyCache()
                    {
                        Min = prop.Min,
                        PropertyName = prop.PropertyName,
                    };

                    BeforeUploadMaterialPropertyCache.Add(temp);

                    if (!GroupedMaterialPropertyCache.TryGetValue(path, out var value))
                    {
                        value = new ToggleMaterialPropertyCache()
                        {
                            Max = prop.Max,
                            Min = prop.Min,
                            Default = prop.GetDefaultValueCalculated(),
                            MenuFullPath = path,
                            PropertyName = prop.PropertyName,
                        };

                        GroupedMaterialPropertyCache.Add(path, value);
                    }

                    var (render, type) = ActionClipService.MatPropLookup(
                       allRenderers: true,
                       singleRenderer: null as Renderer,
                       avatarObject: avatarObject,
                       propName: temp.PropertyName
                   );

                    temp.Renderers = render;

                    for (int i = 0; i < render.Count; i++)
                    {
                        var renderer = render[i];
                        value.TryAddRenderer(renderer);
                    }
                }
            }
        }

        [FeatureBuilderAction(FeatureOrder.Default)]
        public void ApplyToggles()
        {
            foreach (var toggle in GroupedMaterialPropertyCache)
            {
                uniqueModelNum++;

                ApplyOneToggle(toggle.Value);
            }
        }

        private void ApplyOneToggle(ToggleMaterialPropertyCache MaterialPropertyCache)
        {
            var fx = GetFx();

            var paramName = MaterialPropertyCache.MenuFullPath;

            var hasTitle = !string.IsNullOrEmpty(paramName);

            var addMenuItem = (hasTitle || HasIcon); //&& model.addMenuItem;

            CurrentParamName = $"{GetInternalIdString()} Menu #{uniqueModelNum}"; ;

            var def = MaterialPropertyCache.Default;

            var param = fx.NewFloat(
                paramName,
                synced: true,
                def: def,
                saved: true,
                usePrefix: UsePrefixOnParam
            );

            VFCondition isOn;
            bool defaultOn;

            if (IsSliderInactiveAtZero)
            {
                //drive = (state, on) => { if (!on) state.Drives(param, 0); };
                defaultOn = def > 0;
                isOn = param.IsGreaterThan(0);
            }
            else
            {
                defaultOn = true;
                isOn = fx.Always();
            }

            if (addMenuItem)
            {
                manager.GetMenu().NewMenuSlider(
                    paramName,
                    param,
                    icon: null
                //icon: hasIcon ? MaterialPropertyCache.icon?.Get() : null
                );
            }

            var layerName = paramName;

            if (string.IsNullOrEmpty(layerName))
            {
                layerName = "Toggle";
            }

            var layer = fx.NewLayer(layerName);
            var off = layer.NewState("Off");

            var elements = MaterialPropertyCache.GetActions();

            var state = new Model.State();

            foreach (var item in elements)
            {
                state.actions.Add(item);
            }

            ApplyOneToggleInternal(layer, off, "On", isOn, defaultOn, state, param, def);
        }

        private void ApplyOneToggleInternal(
            VFLayer layer,
            VFState off,
            string onName,
            VFCondition onCase,
            bool defaultOn,
            Model.State state,
            VFAFloat weight,
            float defaultValue
        )
        {
            var clip = ActionClipService.LoadState(onName, state);

            VFState onState = layer.NewState(onName);

            if (clip.IsStatic())
            {
                var motionClip = clipBuilder.MergeSingleFrameClips(
                    (0, ClipFactoryService.GetEmptyClip()),
                    (1, clip)
                );
                motionClip.UseLinearTangents();
                motionClip.name = clip.name;
                clip = motionClip;
            }

            clip.SetLooping(false);

            onState.WithAnimation(clip).MotionTime(weight);

            onState.TransitionsToExit().When(onCase.Not());


            off.TransitionsTo(onState).When(onCase);

            if (defaultOn)
            {
                layer.GetRawStateMachine().defaultState = onState.GetRaw();
                off.TransitionsFromEntry().When();
            }

            AnimationClip restingClip = clip.Evaluate(defaultValue * clip.GetLengthInSeconds());

            AnimationClip savedRestingClip = restingClip.Clone();
            ClipRewriteService.AddAdditionalManagedClip(savedRestingClip);

            RestingStateCacheList.Add(new RestingStateCache()
            {
                Value = weight,
                AnimationClip = savedRestingClip,
            });
        }

        private string CurrentParamName;

        public override string GetClipPrefix()
        {
            if (CurrentParamName == null)
            {
                return EditorTitle;
            }
            return CurrentParamName.Replace('/', '_');
        }

        private readonly List<RestingStateCache> RestingStateCacheList = new List<RestingStateCache>();

        internal class RestingStateCache
        {
            public VFAFloat Value;
            public AnimationClip AnimationClip;
        }

        [FeatureBuilderAction(FeatureOrder.ApplyToggleRestingState)]
        public void ApplyRestingState()
        {
            foreach (var item in RestingStateCacheList)
            {
                var anim = item.AnimationClip;
                var value = item.Value;

                bool includeInRest = IsSliderInactiveAtZero ? value.GetDefault() > 0 : true;

                if (IsLogicInverted)
                {
                    //XOR Magic possible?
                    includeInRest = !includeInRest;
                }

                if (!includeInRest)
                {
                    return;
                }

                if (!anim.IsStatic())
                {
                    return;
                }

                foreach (var b in anim.GetFloatBindings())
                {
                    FixWriteDefaultsBuilder.RecordDefaultNow(b, true, true);
                }

                foreach (var b in anim.GetObjectBindings())
                {
                    FixWriteDefaultsBuilder.RecordDefaultNow(b, false, true);
                }

                RestingStateService.ApplyClipToRestingState(anim);
            }
        }

        public override VisualElement CreateEditor(SerializedProperty prop)
        {
            var content = new VisualElement();

            var flex = new VisualElement().Row();

            content.Add(flex);

            flex.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(HueControl.MenuPath)), "Menu Path", tooltip: ToggleBuilder.menuPathTooltip).FlexGrow(1));

            content.Add(VRCFuryEditorUtils.WrappedLabel("Materials to control:"));
            content.Add(VRCFuryEditorUtils.List(prop.FindPropertyRelative(nameof(HueControl.Materials))));

            content.Add(VRCFuryEditorUtils.WrappedLabel("PropertySettings:"));
            content.Add(VRCFuryEditorUtils.List(prop.FindPropertyRelative(nameof(HueControl.Configs))));

            return content;
        }

        // var row = new VisualElement().Row();
        // var propField = VRCFuryEditorUtils.Prop(propertyNameProp, "Property").FlexGrow(1);
        // propField.RegisterCallback<ChangeEvent<string>>(e => UpdateValueType());
        //             row.Add(propField);
        //             row.Add(new Button(SearchClick) { text = "Search" }.Margin(0));
        //             content.Add(row);

        // void SearchClick()
        // {
        //     var searchWindow = new VrcfSearchWindow("Material Properties");
        //     GetTreeEntries(searchWindow);
        //     searchWindow.Open(value =>
        //     {
        //         propertyNameProp.stringValue = value;
        //         Apply();
        //     });
        // }

    }
}
