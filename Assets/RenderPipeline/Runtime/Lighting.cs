using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string c_BufferName = "Lighting";
    const int c_MaxDirLightCount = 4;

    static int s_DirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    static int s_DirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    static int s_DirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    static Vector4[] s_DirLightColors = new Vector4[c_MaxDirLightCount];
    static Vector4[] s_DirLightDirections = new Vector4[c_MaxDirLightCount];

    CommandBuffer m_buffer = new CommandBuffer
    {
        name = c_BufferName
    };

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        m_buffer.BeginSample(c_BufferName);
        SetupLights(cullingResults);
        m_buffer.EndSample(c_BufferName);
        context.ExecuteCommandBuffer(m_buffer);
        m_buffer.Clear();
    }

    void SetupLights(CullingResults cullingResults)
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;

        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight light = visibleLights[i];
            if (light.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount++, ref light);
                if (dirLightCount >= c_MaxDirLightCount)
                {
                    break;
                }
            }
        }

        m_buffer.SetGlobalInt(s_DirLightCountId, dirLightCount);
        m_buffer.SetGlobalVectorArray(s_DirLightColorsId, s_DirLightColors);
        m_buffer.SetGlobalVectorArray(s_DirLightDirectionsId, s_DirLightDirections);
    }

    void SetupDirectionalLight(int index, ref VisibleLight visibleLight)
    {
        s_DirLightColors[index] = visibleLight.finalColor;
        s_DirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
    }

}
