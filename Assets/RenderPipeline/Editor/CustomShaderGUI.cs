using System;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI {


    MaterialEditor materialEditor;
    MaterialProperty[] properties;
    Material material;

    static GUIContent staticLabel = new GUIContent();

    public enum SurfaceType
    {
        Opaque,
        Transparent
    }

    public enum BlendMode
    {
        Alpha,
        Premultiply,
        Additive,
        Multiply
    }

    MaterialProperty mainTex;
    MaterialProperty baseColor;
    MaterialProperty clipping;
    MaterialProperty cutoff;
    MaterialProperty surfaceType;
    MaterialProperty blendMode;

    MaterialProperty metallic;
    MaterialProperty smoothness;

    SurfaceType surfaceTypeValue;
    BlendMode blendModeValue;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Init(materialEditor, properties);

        DrawAlbedoGUI();

        DrawEnum<SurfaceType>(MakeLabel(surfaceType), surfaceType, ref surfaceTypeValue);

        DrawSurfaceOptions();

        DrawClipping();

        if (material.HasProperty("_Metallic"))
        {
            metallic.floatValue = materialEditor.RangeProperty(metallic, "Metallic");
        }

        if (material.HasProperty("_Smoothness"))
        {
            smoothness.floatValue = materialEditor.RangeProperty(smoothness, "Smoothness");
        }
    }

    MaterialProperty FindProperty(string name)
    {
        if (material == null) return null;

        if (material.HasProperty(name))
        {
            return ShaderGUI.FindProperty(name, properties);
        }

        return null;
    }

    void FindAllProperties()
    {
        mainTex = FindProperty("_MainTex");
        baseColor = FindProperty("_BaseColor");
        clipping = FindProperty("_Clipping");
        cutoff = FindProperty("_Cutoff");
        surfaceType = FindProperty("_SurfaceType");
        blendMode = FindProperty("_BlendMode");

        metallic = FindProperty("_Metallic");
        smoothness = FindProperty("_Smoothness");
    }
    static GUIContent MakeLabel(string text, string tooltip = null)
    {
        staticLabel.text = text;
        staticLabel.tooltip = tooltip;
        return staticLabel;
    }

    static GUIContent MakeLabel(MaterialProperty property, string tooltip = null)
    {
        return MakeLabel(property.displayName, tooltip);
    }

    void SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode srcBlend, UnityEngine.Rendering.BlendMode dstBlend)
    {
        if (material.HasProperty("_SrcBlend"))
            material.SetFloat("_SrcBlend", (float)srcBlend);

        if (material.HasProperty("_DstBlend"))
            material.SetFloat("_DstBlend", (float)dstBlend);
    }

    void Init(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.materialEditor = materialEditor;
        this.properties = properties;
        material = materialEditor.target as Material;

        FindAllProperties();

        surfaceTypeValue = (SurfaceType)surfaceType.floatValue;
        blendModeValue = (BlendMode)blendMode.floatValue;
    }

    void DrawAlbedoGUI()
    {
        materialEditor.TexturePropertySingleLine(MakeLabel("Albedo", "Set texture and base color."), mainTex, baseColor);
        materialEditor.TextureScaleOffsetProperty(mainTex);
    }

    void DrawEnum<T>(GUIContent label, MaterialProperty prop, ref T enumValue) where T : Enum
    {
        if (prop != null)
        {
            enumValue = (T)Enum.ToObject(typeof(T), (int)prop.floatValue);
            materialEditor.PopupShaderProperty(prop, label, Enum.GetNames(typeof(T)));
        }
    }

    void DrawSurfaceOptions()
    {
        material.SetOverrideTag("RenderType", "");      // clear override tag

        switch (surfaceTypeValue)
        {
            case SurfaceType.Opaque:
                material.SetFloat("_ZWrite", 1.0f);
                SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.Zero);
                material.renderQueue = (int)RenderQueue.Geometry;
                material.SetOverrideTag("RenderType", "Opaque");
                material.DisableKeyword("ENABLE_CLIPPING");
                break;
            case SurfaceType.Transparent:
                if (surfaceTypeValue == SurfaceType.Transparent)
                {
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)RenderQueue.Transparent;
                    material.DisableKeyword("ENABLE_CLIPPING");
                    material.SetFloat("_ZWrite", 0.0f);

                    DrawEnum<BlendMode>(MakeLabel(blendMode), blendMode, ref blendModeValue);

                    switch (blendModeValue)
                    {
                        case BlendMode.Alpha:
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case BlendMode.Premultiply:
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.One, UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                            break;
                        case BlendMode.Additive:
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.SrcAlpha, UnityEngine.Rendering.BlendMode.One);
                            break;
                        case BlendMode.Multiply:
                            SetMaterialSrcDstBlendProperties(UnityEngine.Rendering.BlendMode.DstColor, UnityEngine.Rendering.BlendMode.Zero);
                            break;
                    }
                }
                break;
        }
    }

    void DrawFloatToggleProperty(GUIContent styles, MaterialProperty prop)
    {
        if (prop == null)
            return;

        EditorGUI.BeginChangeCheck();
        EditorGUI.showMixedValue = prop.hasMixedValue;
        bool newValue = EditorGUILayout.Toggle(styles, prop.floatValue == 1);
        if (EditorGUI.EndChangeCheck())
            prop.floatValue = newValue ? 1.0f : 0.0f;
        EditorGUI.showMixedValue = false;
    }

    void DrawClipping()
    {
        DrawFloatToggleProperty(MakeLabel(clipping), clipping);
        if (clipping.floatValue == 1.0f)
        {
            cutoff.floatValue = materialEditor.RangeProperty(cutoff, "Cutoff");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.SetOverrideTag("RenderType", "TransparentCutout");
            CoreUtils.SetKeyword(material, "ENABLE_CLIPPING", clipping.floatValue == 1.0f);
        }
    }
}