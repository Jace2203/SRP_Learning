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

    private static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.settings = shadowSettings;

        shadowedDirectionalLightCount = 0;
    }

    public void ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (shadowedDirectionalLightCount >= MAX_SHADOW_DIRECTIONAL_LIGHT_COUNT) return;
        if (light.shadows == LightShadows.None) return;
        if (light.shadowStrength <= 0f) return;
        if (!cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b)) return;

        shadowedDirectionalLights[shadowedDirectionalLightCount++] = new ShadowedDirectionalLight
        {
            visibleLightIndex = visibleLightIndex
        };
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
        SetTileViewport(index, split, tileSize);
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    private void SetTileViewport(int index, int split, float tileSize)
    {
        Vector2 offset = new Vector2(index % split, index / split);
        buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));
    }
}