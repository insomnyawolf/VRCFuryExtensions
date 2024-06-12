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

namespace VF.Feature
{
    internal class EmojiControlBuilder : CustomFeatureBuilder<EmojiControl>
    {
        [VFAutowired] private readonly ActionClipService ActionClipService;
        [VFAutowired] private readonly MenuChangesService MenuChangesService;

        [FeatureBuilderAction(FeatureOrder.Default)]
        public void ApplyToggles()
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

            foreach (var toggle in model.Emojis)
            {
                uniqueModelNum++;

                ApplyOneToggle(toggle);
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
                    icon: emoji.Icon?.Get()
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
            flex2.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.Icon)), "Main Icon", tooltip: null).FlexGrow(1));

            content.Add(VRCFuryEditorUtils.WrappedLabel("Emojis:"));
            content.Add(VRCFuryEditorUtils.List(prop.FindPropertyRelative(nameof(EmojiControl.Emojis))));

            return content;
        }
    }
}
