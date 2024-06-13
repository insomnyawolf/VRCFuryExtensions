using UnityEditor;
using UnityEngine;

namespace VF.Feature
{
    // https://docs.unity3d.com/Manual/ClassIDReference.html

    //VRCFuryAssetDatabase.SaveAsset(packed, AvatarManager.tmpDir, packed.name);
    //AssetDatabase.Refresh();

    public static class RandomExtensions
    {
        public static Rect UvToCanvasSection(this Texture2D texture2D, Rect UV)
        {
            var textureWidth = texture2D.width;
            var textureHeight = texture2D.height;
            var result = new Rect()
            {
                x = UV.x * textureWidth,
                width = UV.width * textureWidth,
                y = UV.y * textureHeight,
                height = UV.height * textureHeight,
            };

            return result;
        }

        public static string GetRelativePath(this UnityEngine.Component component)
        {
            var temp = component.transform;

            while (temp?.parent?.transform is Transform parentTransform)
            {
                temp = parentTransform;
            }

            var relativePath = AnimationUtility.CalculateTransformPath(component.transform, temp);

            return relativePath;
        }
    }
}