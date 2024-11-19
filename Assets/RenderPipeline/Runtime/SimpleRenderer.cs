using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class SimpleRenderer
{
    const string m_BufferName = "SimpleRenderer";

    static ShaderTagId s_unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId s_litShaderTagId = new ShaderTagId("CustomLit");

    CommandBuffer m_CommandBuffer = new CommandBuffer() { name = m_BufferName };
    CullingResults m_CullingResults;
    Lighting m_Lighting = new Lighting();

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        PrepareBuffer(camera);
        PrepareForSceneWindow(camera);

        if(!TryCull(context, camera)) return;

        Setup(context, camera);
        m_Lighting.Setup(context, m_CullingResults);
        DrawVisibleGeometry(context, camera);
        DrawUnsupportedShaders(context, camera);
        DrawGizmos(context, camera);
        Submit(context);
    }

    bool TryCull(ScriptableRenderContext context, Camera camera)
    {
        if(camera.TryGetCullingParameters(out ScriptableCullingParameters cullingParameters))
        {
            m_CullingResults = context.Cull(ref cullingParameters);
            return true;
        }

        return false;
    }

    void Setup(ScriptableRenderContext context, Camera camera)
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;
        m_CommandBuffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags <= CameraClearFlags.Color,
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        m_CommandBuffer.BeginSample(m_SampleName);
        ExecuteBuffer(context);
    }

    void Submit(ScriptableRenderContext context)
    {
        m_CommandBuffer.EndSample(m_SampleName);
        ExecuteBuffer(context);
        context.Submit();
    }

    void ExecuteBuffer(ScriptableRenderContext context)
    {
        context.ExecuteCommandBuffer(m_CommandBuffer);
        m_CommandBuffer.Clear();
    }

    void DrawVisibleGeometry(ScriptableRenderContext context, Camera camera)
    {
        // 不透明オブジェクトの描画
        SortingSettings sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        DrawingSettings drawingSettings = new DrawingSettings(s_unlitShaderTagId, sortingSettings);
        drawingSettings.SetShaderPassName(1, s_litShaderTagId);
        drawingSettings.perObjectData = PerObjectData.LightProbe;

        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);

        // スカイボックスの描画
        context.DrawSkybox(camera);

        // 透明オブジェクトの描画
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }
}
