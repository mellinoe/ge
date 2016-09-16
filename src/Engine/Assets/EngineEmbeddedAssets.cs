using Engine.Graphics;
using ImageProcessorCore;
using System;
using System.Reflection;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Engine.Assets
{
    public class EngineEmbeddedAssets : EmbeddedAssetDatabase
    {
        private static Assembly s_engineAssembly = typeof(EngineEmbeddedAssets).GetTypeInfo().Assembly;

        public static readonly AssetID PlaneModelID = "Internal:PlaneModel";
        public static readonly AssetID SphereModelID = "Internal:SphereModel";
        public static readonly AssetID CubeModelID = "Internal:CubeModel";

        public static readonly AssetID PinkTextureID = "Internal:PinkTexture";

        public static readonly AssetID SkyboxBackID = "Internal:SkyboxBack";
        public static readonly AssetID SkyboxFrontID = "Internal:SkyboxFront";
        public static readonly AssetID SkyboxLeftID = "Internal:SkyboxLeft";
        public static readonly AssetID SkyboxRightID = "Internal:SkyboxRight";
        public static readonly AssetID SkyboxBottomID = "Internal:SkyboxBottom";
        public static readonly AssetID SkyboxTopID = "Internal:SkyboxTop";

        public EngineEmbeddedAssets()
        {
            RegisterAsset(PlaneModelID, PlaneModel.MeshData);
            RegisterAsset(SphereModelID, SphereModel.MeshData);
            RegisterAsset(CubeModelID, CubeModel.MeshData);
            RegisterAsset(PinkTextureID, CreatePinkTexture());
            RegisterSkyboxTextures();
        }

        private TextureData CreatePinkTexture()
        {
            return new RawTextureDataArray<RgbaFloat>(
                new RgbaFloat[] { RgbaFloat.Pink },
                1,
                1,
                RgbaFloat.SizeInBytes,
                PixelFormat.R32_G32_B32_A32_Float);
        }

        private void RegisterSkyboxTextures()
        {
            Lazy<ImageProcessorTexture> skyboxBack = new Lazy<ImageProcessorTexture>(
                () => LoadEmbeddedTexture("Engine.Assets.Textures.cloudtop.cloudtop_bk.png"));
            Lazy<ImageProcessorTexture> skyboxBottom = new Lazy<ImageProcessorTexture>(
                () => LoadEmbeddedTexture("Engine.Assets.Textures.cloudtop.cloudtop_dn.png"));
            Lazy<ImageProcessorTexture> skyboxFront = new Lazy<ImageProcessorTexture>(
                () => LoadEmbeddedTexture("Engine.Assets.Textures.cloudtop.cloudtop_ft.png"));
            Lazy<ImageProcessorTexture> skyboxLeft = new Lazy<ImageProcessorTexture>(
                () => LoadEmbeddedTexture("Engine.Assets.Textures.cloudtop.cloudtop_lf.png"));
            Lazy<ImageProcessorTexture> skyboxRight = new Lazy<ImageProcessorTexture>(
                () => LoadEmbeddedTexture("Engine.Assets.Textures.cloudtop.cloudtop_rt.png"));
            Lazy<ImageProcessorTexture> skyboxTop = new Lazy<ImageProcessorTexture>(
                () => LoadEmbeddedTexture("Engine.Assets.Textures.cloudtop.cloudtop_up.png"));

            RegisterAsset(SkyboxBackID, skyboxBack);
            RegisterAsset(SkyboxFrontID, skyboxFront);
            RegisterAsset(SkyboxLeftID, skyboxLeft);
            RegisterAsset(SkyboxRightID, skyboxRight);
            RegisterAsset(SkyboxBottomID, skyboxBottom);
            RegisterAsset(SkyboxTopID, skyboxTop);
        }

        private static ImageProcessorTexture LoadEmbeddedTexture(string embeddedName)
        {
            using (var stream = s_engineAssembly.GetManifestResourceStream(embeddedName))
            {
                return new ImageProcessorTexture(new Image(stream));
            }
        }
    }
}
