using UnityEngine;
using UnityEngine.Rendering;

public class DeferRenderPipeline : RenderPipeline
{
    RenderTexture gdepth;   // 深度缓存
    RenderTexture[] gbuffers = new RenderTexture[4];
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4];

    public DeferRenderPipeline()
    {
        // R    G   B   A
        // color
        // normal
        // world Position
        // metallic roughness ao

        gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);

        for(int i = 0; i < 4; ++i)
        {
            gbufferID[i] = gbuffers[i];
        }
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        Shader.SetGlobalTexture("_gdepth", gdepth);
        for(int i = 0; i < 4; ++i)
            Shader.SetGlobalTexture("_GT" + i, gbuffers[i]);

        Camera camera = cameras[0];

        GbufferPass(context, camera);

        LightPass(context, camera);

        context.DrawSkybox(camera);

        context.Submit();
    }

    void GbufferPass(ScriptableRenderContext context, Camera camera)
    {
        context.SetupCameraProperties(camera);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";

        cmd.SetRenderTarget(gbufferID, gdepth);
        // clear
        cmd.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(cmd);

        // culling
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

        // config
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");   // 使用lightmode为gbuffer的shader
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        context.Submit();
    }

    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        Material mat = new Material(Shader.Find("HeRP/lightpass"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
    }
}
