using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/SimpleRenderPipelineAsset")]
public class SimpleRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField] bool useSRPBatcher = true;
    protected override RenderPipeline CreatePipeline()
    {
        return new SimpleRenderPipeline(useSRPBatcher);
    }
}
