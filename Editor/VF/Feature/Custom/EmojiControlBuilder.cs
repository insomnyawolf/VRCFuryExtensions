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

        public string ExclusiveId;

        [FeatureBuilderAction(FeatureOrder.Default)]
        public void ApplyToggles()
        {
            ExclusiveId = GetInternalIdString();

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

            var hasTitle = !string.IsNullOrEmpty(fullMenuPath);

            var defaultOn = false;

            CurrentParamName = $"{ExclusiveId} {fullMenuPath} #{uniqueModelNum}";

            var param = fx.NewBool(
                fullMenuPath,
                synced: true,
                saved: false,
                def: defaultOn,
                usePrefix: UsePrefixOnParam
                );

            VFCondition onCase = param.IsTrue();

            emoji.IsOn = onCase;

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

            VFState inState = layer.NewState(onName).WithAnimation(clip);
            VFState onState = inState;

            onState.TransitionsToExit().When(onCase.Not()).WithTransitionExitTime(1);

            off.TransitionsTo(inState).When(onCase);
        }

        //[FeatureBuilderAction(FeatureOrder.CollectToggleExclusiveTags)]
        //public void ApplyExclusiveTags()
        //{
        //    var fx = GetFx();

        //    List<VFCondition> allConditions = new List<VFCondition>();

        //    var builders = allBuildersInRun.OfType<EmojiControlBuilder>().ToList();

        //    var allExclusiveEmojiTags = builders.Select(b => b.ExclusiveId).Distinct().ToList();

        //    for (int builderIndex = 0; builderIndex < builders.Count; builderIndex++)
        //    {
        //        EmojiControlBuilder builder = builders[builderIndex];
        //        var model = builder.model.Emojis;

        //        for (int emojiIndex = 0; emojiIndex < model.Count; emojiIndex++)
        //        {
        //            Emoji emoji = model[emojiIndex];

        //            if (emoji.IsOn is not VFCondition isOn)
        //            {
        //                continue;
        //            }

        //            var temp = isOn.Not();

        //            emoji.Cond = temp;

        //            allConditions.Add(temp);
        //        }
        //    }

        //    var emojis = model.Emojis;

        //    for (int emojiIndex = 0; emojiIndex < emojis.Count; emojiIndex++)
        //    {
        //        Emoji emoji = emojis[emojiIndex];

        //        if (emoji.Cond is not VFCondition cond)
        //        {
        //            continue;
        //        }

        //        if (emoji.IsOn is not VFCondition isOn)
        //        {
        //            continue;
        //        }

        //        var allexceptSelf = allConditions.Where(item => item != cond);

        //        var allOthersOffCondition = fx.Always();

        //        foreach (var item in allexceptSelf)
        //        {
        //            allOthersOffCondition.And(item);
        //        }

        //        var layer = fx.NewLayer($"{model.MenuPath} - Off Trigger");

        //        var off = layer.NewState("Idle");

        //        var on = layer.NewState("Trigger");

        //        on.TransitionsTo(off).When(allOthersOffCondition.Not().Or(isOn.Not()));

        //        // Do not auto turn on
        //        //off.TransitionsTo(on).When(allOthersOffCondition);
        //    }
        //}

        public override VisualElement CreateEditor(SerializedProperty prop)
        {
            var content = new VisualElement();

            var flex1 = new VisualElement().Row();
            content.Add(flex1);
            flex1.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.MenuPath)), "Menu Path", tooltip: ToggleBuilder.menuPathTooltip).FlexGrow(1));

            var flex2 = new VisualElement().Row();
            content.Add(flex2);
            flex2.Add(VRCFuryEditorUtils.Prop(prop.FindPropertyRelative(nameof(EmojiControl.Icon)), "Main Icon", tooltip: ToggleBuilder.menuPathTooltip).FlexGrow(1));

            content.Add(VRCFuryEditorUtils.WrappedLabel("Emojis:"));
            content.Add(VRCFuryEditorUtils.List(prop.FindPropertyRelative(nameof(EmojiControl.Emojis))));

            return content;
        }
    }
}
