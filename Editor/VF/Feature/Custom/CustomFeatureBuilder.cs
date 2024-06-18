using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VF.Builder;
using VF.Feature.Base;
using VF.Model.Feature;

namespace VF.Feature
{
    internal abstract class CustomFeatureBuilder<ModelType> : FeatureBuilder<ModelType> where ModelType : FeatureModel 
    {
        public const string TagEditorOnly = "EditorOnly";
        public const string TagUntagged = "Untagged";

        protected const bool UsePrefixOnParam = true;
        protected string CurrentParamName;

        private static int InternalId = 0;

        public CustomFeatureBuilder()
        {
            InternalId++;
        }

        private static string EditorTitleCache;

        public override string GetEditorTitle()
        {
            if (EditorTitleCache is null)
            {
                EditorTitleCache = $"Custom {typeof(ModelType).Name}";
            }

            return EditorTitleCache;
        }

        public string GetInternalIdString()
        {
            return $"{GetEditorTitle()} {InternalId}";
        }

        //public string GetSaveName(Object objectToSave, UnityEngine.Component component, string overrideAssetTypeName)
        //{
        //    //asset.GetType().Name

        //    var generatedName = $"VRCFury {overrideAssetTypeName} for {component.owner().name}";

        //    return generatedName;
        //    // Custom EmojiControl 4_Emoji_Main
        //    // VRCFury Material for EmojiSystem_Custom EmojiControl 4_Emoji_Mai
        //    // $"VRCFury {controller.GetType().ToString()} for {manager.AvatarObject.name}",
        //    // $"VRCFury {"Material"} for {component.owner().name}",
        //}

        public override string GetClipPrefix()
        {
            if (CurrentParamName == null)
            {
                return GetEditorTitle();
            }

            return CurrentParamName.Replace('/', '_');
        }

        public override abstract VisualElement CreateEditor(SerializedProperty prop);
    }
}
