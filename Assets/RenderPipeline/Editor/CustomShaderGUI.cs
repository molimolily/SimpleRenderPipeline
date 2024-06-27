using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    enum SurfaceType
    {
        Opaque,
        Transparent
    }

    enum BlendType
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    static GUIContent s_Label = new GUIContent();

    MaterialEditor m_MaterialEditor;
    MaterialProperty[] m_Properties;
    Material m_Material;

    // Properties
    MaterialProperty m_MainTex;
    MaterialProperty m_BaseColor;
    MaterialProperty m_Clipping;
    MaterialProperty m_Cutoff;
    MaterialProperty m_SurfaceType;
    MaterialProperty m_BlendType;

    // Lit shader properties
    MaterialProperty m_PreMultiplyAlpha;
    MaterialProperty m_Metallic;
    MaterialProperty m_Smoothness;

    SurfaceType m_SurfaceTypeValue = SurfaceType.Opaque;
    BlendType m_BlendTypeValue = BlendType.Alpha;


    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        m_MaterialEditor = materialEditor;
        m_Properties = properties;
        m_Material = materialEditor.target as Material;
        FindAllProperties();

        DrawAlbedoAndTextureGUI();
        DrawSurfaceOptions();
        DrawClipping();
        DrawMetallic();
        DrawSmoothness();
    }

    static GUIContent MakeLabel(string text, string tooltip = null)
    {
        s_Label.text = text;
        s_Label.tooltip = tooltip;
        return s_Label;
    }

    static GUIContent MakeLabel(MaterialProperty property, string tooltip = null)
    {
        return MakeLabel(property.displayName, tooltip);
    }

    MaterialProperty FindProperty(string name)
    {
        if (m_Material == null) return null;

        if (m_Material.HasProperty(name))
        {
            return ShaderGUI.FindProperty(name, m_Properties);
        }

        return null;
    }

    void FindAllProperties()
    {
        m_MainTex = FindProperty("_MainTex");
        m_BaseColor = FindProperty("_BaseColor");
        m_Clipping = FindProperty("_Clipping");
        m_Cutoff = FindProperty("_Cutoff");
        m_SurfaceType = FindProperty("_SurfaceType");
        m_BlendType = FindProperty("_BlendType");

        m_PreMultiplyAlpha = FindProperty("_PremulAlpha");
        m_Metallic = FindProperty("_Metallic");
        m_Smoothness = FindProperty("_Smoothness");
    }

    void DrawAlbedoAndTextureGUI()
    {
        m_MaterialEditor.TexturePropertySingleLine(MakeLabel("Albedo"), m_MainTex, m_BaseColor);
        m_MaterialEditor.TextureScaleOffsetProperty(m_MainTex);
    }

    void DrawEnumComboBox<T>(GUIContent label, MaterialProperty property, ref T value) where T : Enum
    {
        if (property == null) return;
        value = (T)Enum.ToObject(typeof(T), (int)property.floatValue);
        m_MaterialEditor.PopupShaderProperty(property, label, Enum.GetNames(typeof(T)));
    }

    void SetPremultiplyAlpha(bool value)
    {
        if (!m_Material.HasProperty("_PremulAlpha")) return;

        if (value)
            m_Material.EnableKeyword("PREMULTIPLY_ALPHA");
        else
            m_Material.DisableKeyword("PREMULTIPLY_ALPHA");
    }
    void DrawSurfaceOptions()
    {
        DrawEnumComboBox(MakeLabel("Surface Type"), m_SurfaceType, ref m_SurfaceTypeValue);

        m_Material.SetOverrideTag("RenderType", ""); // Reset the render type
        switch (m_SurfaceTypeValue)
        {
            case SurfaceType.Opaque:
                m_Material.SetOverrideTag("RenderType", "Opaque");
                m_Material.renderQueue = (int)RenderQueue.Geometry;
                SetZWrite(true);
                SetPremultiplyAlpha(false);
                SetBlendProperties(BlendMode.One, BlendMode.Zero);
                
                
                break;
            case SurfaceType.Transparent:
                m_Material.SetOverrideTag("RenderType", "Transparent");
                m_Material.renderQueue = (int)RenderQueue.Transparent;
                SetZWrite(false);
                
                DrawEnumComboBox(MakeLabel("Blend Type"), m_BlendType, ref m_BlendTypeValue);
                switch (m_BlendTypeValue)
                {
                    case BlendType.Alpha:
                        SetPremultiplyAlpha(false);
                        SetBlendProperties(BlendMode.SrcAlpha, BlendMode.OneMinusSrcAlpha);
                        break;
                    case BlendType.Premultiply:
                        SetPremultiplyAlpha(true);
                        SetBlendProperties(BlendMode.One, BlendMode.OneMinusSrcAlpha);
                        break;
                    case BlendType.Additive:
                        SetPremultiplyAlpha(false);
                        SetBlendProperties(BlendMode.SrcAlpha, BlendMode.One);
                        break;
                    case BlendType.Multiply:
                        SetPremultiplyAlpha(false);
                        SetBlendProperties(BlendMode.DstColor, BlendMode.Zero);
                        break;
                }
                break;
        }

    }

    void SetBlendProperties(BlendMode srcBlend, BlendMode dstBlend)
    {
        if (m_Material.HasProperty("_SrcBlend"))
            m_Material.SetFloat("_SrcBlend", (float)srcBlend);

        if (m_Material.HasProperty("_DstBlend"))
            m_Material.SetFloat("_DstBlend", (float)dstBlend);
    }

    void SetZWrite(bool value)
    {
        if (m_Material.HasProperty("_ZWrite"))
            m_Material.SetFloat("_ZWrite", value ? 1 : 0);
    }

    void DrawFloatToggle(GUIContent label, MaterialProperty property)
    {
        if (property == null) return;

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = property.hasMixedValue;
        bool newValue = EditorGUILayout.Toggle(label, property.floatValue == 1);
        if (EditorGUI.EndChangeCheck())
            property.floatValue = newValue ? 1.0f : 0.0f;
        EditorGUI.showMixedValue = false;
    }

    void DrawClipping()
    {
        DrawFloatToggle(MakeLabel(m_Clipping), m_Clipping);

        if (m_Clipping.floatValue == 1f)
        {
            m_Material.SetOverrideTag("RenderType", "TransparentCutout");
            m_Material.EnableKeyword("CLIPPING");
            m_Material.renderQueue = (int)RenderQueue.AlphaTest;
            m_Cutoff.floatValue = m_MaterialEditor.RangeProperty(m_Cutoff, "Cutoff");
            
        }
        else
        {
            m_Material.DisableKeyword("CLIPPING");
        }
    }

    void DrawMetallic()
    {
        if (m_Material.HasProperty("_Metallic"))
        {
            m_Metallic.floatValue = m_MaterialEditor.RangeProperty(m_Metallic, "Metallic");
        }
    }

    void DrawSmoothness()
    {
        if (m_Material.HasProperty("_Smoothness"))
        {
            m_Smoothness.floatValue = m_MaterialEditor.RangeProperty(m_Smoothness, "Smoothness");
        }
    }
}