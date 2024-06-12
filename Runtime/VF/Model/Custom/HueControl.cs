using System;
using System.Collections.Generic;
using UnityEngine;
using VF.Model.StateAction;

namespace VF.Model.Feature
{
    // Material Property
    internal class ToggleMaterialPropertyCache
    {
        public string MenuFullPath;
        public string PropertyName;
        public float Min = 0;
        public float Max = 0;
        public float Default = 0;
        public List<Renderer> Renderers = new List<Renderer>();

        public void TryAddRenderer(Renderer Renderer)
        {
            var candidateId = Renderer.GetInstanceID();

            for (int i = 0; i < Renderers.Count; i++)
            {
                var id = Renderers[i].GetInstanceID();

                if (id == candidateId)
                {
                    return;
                }
            }

            Renderers.Add(Renderer);
        }

        public IEnumerable<MaterialPropertyAction> GetActions()
        {
            foreach (var render in Renderers)
            {
                var temp = new MaterialPropertyAction()
                {
                    affectAllMeshes = true,
                    androidActive = false,
                    desktopActive = true,
                    propertyName = PropertyName,
                    renderer = render,
                    value = Max,
                };
                yield return temp;
            }

        }
    }

    internal class BeforeUploadMaterialPropertyCache
    {
        public string PropertyName;
        public float Min = 0;
        public IList<Renderer> Renderers;

        public IEnumerable<MaterialPropertyAction> GetActions()
        {
            foreach (var render in Renderers)
            {
                var temp = new MaterialPropertyAction()
                {
                    affectAllMeshes = true,
                    androidActive = false,
                    desktopActive = true,
                    propertyName = PropertyName,
                    renderer = render,
                    value = Min,
                };
                yield return temp;
            }

        }
    }

    [Serializable]
    internal class HueControl : NewFeatureModel
    {
        public string MenuPath = "";
        public List<Material> Materials;
        public List<MaterialPropertyConfiguration> Configs;
    }

    [Serializable]
    internal class MaterialPropertyConfiguration
    {
        public string OptionName = "";
        public string PropertyName = "";
        public float Min = 0;
        public float Max = 0;
        public float DefaultPercentage = 0;

        public float GetDefaultValueCalculated()
        {
            var final = DefaultPercentage / 100;

            return final;
        }
    }
}