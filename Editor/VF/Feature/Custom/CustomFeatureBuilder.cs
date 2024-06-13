using UnityEditor;
using UnityEngine.UIElements;
using VF.Feature.Base;
using VF.Model.Feature;

namespace VF.Feature
{
    internal abstract class CustomFeatureBuilder<ModelType> : FeatureBuilder<ModelType> where ModelType : FeatureModel 
    {
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
