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
    internal class HueControlBuilder : CustomFeatureBuilder<HueControl>
    {
        // Apply on upload
        [VFAutowired] private readonly RestingStateService RestingStateService;

        // Toggle
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

            var fullMenuPath = MaterialPropertyCache.MenuFullPath;

            CurrentParamName = $"{GetInternalIdString()} {fullMenuPath} #{uniqueModelNum}";

            var def = MaterialPropertyCache.Default;

            var param = fx.NewFloat(
                fullMenuPath,
                synced: true,
                def: def,
                saved: true,
                usePrefix: UsePrefixOnParam
            );

            VFCondition isOn = fx.Always(); ;
            bool defaultOn = true;

            manager.GetMenu().NewMenuSlider(
                    path: fullMenuPath,
                    param: param,
                    icon: null
                //icon: hasIcon ? MaterialPropertyCache.icon?.Get() : null
                );

            var layer = fx.NewLayer(fullMenuPath);
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
