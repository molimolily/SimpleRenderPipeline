using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class SimpleRenderer
{
    partial void DrawGizmos(ScriptableRenderContext context, Camera camera);
    partial void DrawUnsupportedShaders(ScriptableRenderContext context, Camera camera);
    partial void PrepareForSceneWindow(Camera camera);
    partial void PrepareBuffer(Camera camera);

#if UNITY_EDITOR
    static ShaderTagId[] s_LegacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    static Material s_ErrorMaterial;
    
    string m_SampleName { get; set; }

    partial void DrawGizmos(ScriptableRenderContext context, Camera camera)
    {
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawUnsupportedShaders(ScriptableRenderContext context, Camera camera)
    {
        if(s_ErrorMaterial == null)
        {
            s_ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        DrawingSettings drawingSettings = new DrawingSettings(s_LegacyShaderTagIds[0], new SortingSettings(camera))
        {
            overrideMaterial = s_ErrorMaterial
        };

        for (int i = 1; i < s_LegacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, s_LegacyShaderTagIds[i]);
        }

        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        context.DrawRenderers(m_CullingResults, ref drawingSettings, ref filteringSettings);
    }

    partial void PrepareForSceneWindow(Camera camera)
    {
        if (camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
        }
    }

    partial void PrepareBuffer(Camera camera)
    {
        m_CommandBuffer.name = m_SampleName = camera.name;
    }

#else
    const string m_SampleName = m_BufferName;
#endif
}
