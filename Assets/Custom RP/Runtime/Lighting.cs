using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string BUFFER_NAME = "Lighting";

    private static int
        dirLightColorId = Shader.PropertyToID("_DirectionalLightColor"),
        dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

    private CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME,
    };

    public void Setup(ScriptableRenderContext context)
    {
        buffer.BeginSample(BUFFER_NAME);
        SetupDirectionalLight();
        buffer.EndSample(BUFFER_NAME);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private void SetupDirectionalLight()
    {
        Light light = RenderSettings.sun;
        buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
    }
}