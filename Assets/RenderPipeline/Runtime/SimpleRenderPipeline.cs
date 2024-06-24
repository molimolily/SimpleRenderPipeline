using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class SimpleRenderPipeline : RenderPipeline
{
    SimpleRenderer renderer = new SimpleRenderer();

    public SimpleRenderPipeline(bool useSRPBatcher)
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    protected override void Render(ScriptableRenderContext context, Camera[] cameras) { }

    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        foreach(Camera camera in cameras)
        {
           renderer.Render(context, camera);
        }
    }
}
