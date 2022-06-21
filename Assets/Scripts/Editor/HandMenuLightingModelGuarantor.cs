using Nova;
using UnityEditor;

namespace NovaSamples.HandMenu
{
    /// <summary>
    /// This just ensures the correct lighting models for the hand sample in case they are
    /// accidentally overwritten when a user imports Nova.
    /// </summary>
    public class HandMenuLightingModelGuarantor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
        {
            NovaSettings.UIBlock2DLightingModels = LightingModelBuildFlag.Unlit | LightingModelBuildFlag.Standard;
            NovaSettings.TextBlockLightingModels = LightingModelBuildFlag.Unlit | LightingModelBuildFlag.Standard;
            NovaSettings.UIBlock3DLightingModels = LightingModelBuildFlag.Lambert | LightingModelBuildFlag.Standard;
        }
    }
}

