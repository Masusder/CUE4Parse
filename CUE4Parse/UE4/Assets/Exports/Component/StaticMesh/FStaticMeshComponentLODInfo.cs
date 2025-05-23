using CUE4Parse.UE4.Assets.Exports.BuildData;
using CUE4Parse.UE4.Assets.Readers;
using CUE4Parse.UE4.Objects.Core.Math;
using CUE4Parse.UE4.Objects.Core.Misc;
using CUE4Parse.UE4.Objects.Engine;
using CUE4Parse.UE4.Objects.Meshes;
using CUE4Parse.UE4.Objects.RenderCore;
using CUE4Parse.UE4.Readers;
using CUE4Parse.UE4.Versions;
using Newtonsoft.Json;

namespace CUE4Parse.UE4.Assets.Exports.Component.StaticMesh;

public struct FPaintedVertex
{
    public FVector Position;
    public FVector4 Normal;
    public FColor Color;

    public FPaintedVertex(FArchive Ar)
    {
        Position = Ar.Read<FVector>();

        if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.IncreaseNormalPrecision)
        {
            Normal = new FPackedNormal(Ar);
        }
        else
        {
            Normal = Ar.Read<FVector4>();
        }

        Color = Ar.Read<FColor>();
    }
}

[JsonConverter(typeof(FStaticMeshComponentLODInfoConverter))]
public class FStaticMeshComponentLODInfo
{
    private const byte OverrideColorsStripFlag = 1;
    public readonly FGuid OriginalMapBuildDataId;
    public readonly FGuid MapBuildDataId;
    public readonly FPaintedVertex[]? PaintedVertices;
    public readonly FColorVertexBuffer? OverrideVertexColors;

    public FStaticMeshComponentLODInfo(FAssetArchive Ar)
    {
        var stripFlags = new FStripDataFlags(Ar);
        if (!stripFlags.IsAudioVisualDataStripped())
        {
            if (FRenderingObjectVersion.Get(Ar) < FRenderingObjectVersion.Type.MapBuildDataSeparatePackage)
            {
                FLightMap? lightMap = Ar.Read<ELightMapType>() switch
                {
                    ELightMapType.LMT_1D => new FLegacyLightMap1D(Ar),
                    ELightMapType.LMT_2D => new FLightMap2D(Ar),
                    _ => null
                };

                var shadowMap = Ar.Read<EShadowMapType>() switch
                {
                    EShadowMapType.SMT_2D => new FShadowMap2D(Ar),
                    _ => null
                };
            }
            else
            {
                MapBuildDataId = Ar.Read<FGuid>();
            }

            if (Ar.Game >= EGame.GAME_UE5_5)
            {
                OriginalMapBuildDataId = Ar.Read<FGuid>();
            }
        }

        if (!stripFlags.IsClassDataStripped(OverrideColorsStripFlag))
        {
            var bLoadVertexColorData = Ar.Read<byte>();
            if (bLoadVertexColorData == 1)
            {
                OverrideVertexColors = new FColorVertexBuffer(Ar);
            }
        }

        if (!stripFlags.IsEditorDataStripped())
        {
            PaintedVertices = Ar.ReadArray(() => new FPaintedVertex(Ar));
        }

        if (Ar.Game == EGame.GAME_StarWarsJediSurvivor) Ar.Position += 20;
    }
}
