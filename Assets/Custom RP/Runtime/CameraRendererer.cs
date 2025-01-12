using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    private const string BUFFER_NAME = "Render Camera";
    private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

    private CommandBuffer buffer = new CommandBuffer()
    {
        name = BUFFER_NAME,
    };

    private ScriptableRenderContext context;
    private CullingResults cullingResults;
    private Camera camera;

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        if (!Cull()) return;

        Setup();
        DrawVisibleGeometry();
        Submit();
    }

    private void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(BUFFER_NAME);
        ExecuteBuffer();
    }

    private void DrawVisibleGeometry()
    {
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        // Draw Opaque Objects
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // Draw Skybox
        context.DrawSkybox(camera);

        // Draw Transparent Objects
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    private void Submit()
    {
        buffer.EndSample(BUFFER_NAME);
        ExecuteBuffer();
        context.Submit();
    }

    private void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    private bool Cull()
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
}