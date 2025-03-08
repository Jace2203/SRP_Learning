using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    private struct ShadowedDirectionalLight
    {
        public int visibleLightIndex;
    }

    private const string BUFFER_NAME = "Shadows";
    private const int MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT = 4;

    private CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME,
    };

    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private ShadowSettings settings;

    private ShadowedDirectionalLight[] shadowedDirectionalLights = new ShadowedDirectionalLight[MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT];
    private int shadowedDirectionalLightCount;

    private static int
        dirShadowAtlasId =Shader.PropertyToID("_DirectionalShadowAtlas"),
        dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

    private static Matrix4x4[]
        dirShadowMatrices = new Matrix4x4[MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = shadowSettings;

        shadowedDirectionalLightCount = 0;
    }

    public Vector2 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount >= MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT) return Vector2.zero;
        if (light.shadows == LightShadows.None) return Vector2.zero;
        if (light.shadowStrength <= 0f) return Vector2.zero;
        if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) return Vector2.zero;

        shadowedDirectionalLights[shadowedDirectionalLightCount] = new ShadowedDirectionalLight
        {
            visibleLightIndex = visibleLightIndex
        };

        return new Vector2(light.shadowStrength, shadowedDirectionalLightCount++);
    }
    
    public void Render()
    {
        if (shadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT
            (
                dirShadowAtlasId, 1, 1, 32,
                FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
        }
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void RenderDirectionalShadows()
    {
        int atlasSize = (int)settings.directional.atlasSize;
        buffer.GetTemporaryRT
        (
            dirShadowAtlasId, atlasSize, atlasSize, 32,
            FilterMode.Bilinear, RenderTextureFormat.Shadowmap
        );
        buffer.SetRenderTarget
        (
            dirShadowAtlasId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
        );
        buffer.ClearRenderTarget(true, false, Color.clear);
        buffer.BeginSample(BUFFER_NAME);
            ExecuteBuffer();

            int split = shadowedDirectionalLightCount <= 1 ? 1 : 2;
            int tileSize = atlasSize / split;

            for (int i = 0; i < shadowedDirectionalLightCount; i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }
        buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
        buffer.EndSample(BUFFER_NAME);
        ExecuteBuffer();
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        ShadowedDirectionalLight light = shadowedDirectionalLights[index];
        ShadowDrawingSettings shadowSettings = new ShadowDrawingSettings
        (
            cullingResults, light.visibleLightIndex,
            BatchCullingProjectionType.Orthographic
        );
        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives
        (
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
            out ShadowSplitData splitData
        );
        shadowSettings.splitData = splitData;
        dirShadowMatrices[index] = ConvertToAtlasMatrix(
            projectionMatrix * viewMatrix,
            SetTileViewport(index, split, tileSize),
            split);
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    private Vector2 SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
        return offset;
    }

    private Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m, Vector2 offset, int split)
    {
        if (SystemInfo.usesReversedZBuffer)
        {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }

        float scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;

        return m;
    }
}