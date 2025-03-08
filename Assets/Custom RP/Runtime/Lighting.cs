using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string BUFFER_NAME = "Lighting";

    private const int MAX_DIR_LIGHT_COUNT = 4;

    private static int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections"),
        dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private static Vector4[]
        dirLightColors = new Vector4[MAX_DIR_LIGHT_COUNT],
        dirLightDirections = new Vector4[MAX_DIR_LIGHT_COUNT],
        dirLightShadowData = new Vector4[MAX_DIR_LIGHT_COUNT];

    private CullingResults cullingResults;
    private Shadows shadows = new Shadows();        

    private CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME,
    };

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(BUFFER_NAME);
            shadows.Setup(context, cullingResults, shadowSettings);
            SetupLights();
            shadows.Render();
        buffer.EndSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void Cleanup()
    {
        shadows.Cleanup();
    }

    private void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            switch (visibleLight.lightType)
            {
                case LightType.Directional:
                    SetupDirectionalLight(dirLightCount++, ref visibleLight);
                    break;
                default:
                    break;
            }

            if (dirLightCount >= MAX_DIR_LIGHT_COUNT) break;
        }

        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
        buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
    }
    
    private void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
        dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
    }
}