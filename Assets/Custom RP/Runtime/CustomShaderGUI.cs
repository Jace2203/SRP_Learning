using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private Object[] materials;
    private MaterialProperty[] properties;

    private bool Clipping           { set => SetProperty("_Clipping", "_CLIPPING", value); }
    private bool PremultiplyAlpha   { set => SetProperty("_PremulAlpha", "_PREMULTIPLY_ALPHA", value); }
    private BlendMode SrcBlend      { set => SetProperty("_SrcBlend", (float)value); }
    private BlendMode DstBlend      { set => SetProperty("_DstBlend", (float)value); }
    private bool ZWrite             { set => SetProperty("_ZWrite", value ? 1f : 0f); }
    private RenderQueue RenderQueue
    {
        set
        {
            foreach (Material m in materials)
            {
                m.renderQueue = (int)value;
            }
        }
    }

    private bool HasProperty(string name) => FindProperty(name, properties, false) != null;
    private bool HasPremultiplyAlpha => HasProperty("_PremulAlpha");

    private bool showPresets;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        base.OnGUI(materialEditor, properties);
        editor = materialEditor;
        materials = materialEditor.targets;
        this.properties = properties;

        EditorGUILayout.Space();
        showPresets = EditorGUILayout.Foldout(showPresets, "Show Presets", true);
        if (showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }
    }

    private void OpaquePreset()
    {
        if (PresetButton("Opaque"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    private void ClipPreset()
    {
        if (PresetButton("Clip"))
        {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }
    
    private void FadePreset()
    {
        if (PresetButton("Fade"))
        {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    private void TransparentPreset()
    {
        if (HasPremultiplyAlpha && PresetButton("Transparent"))
        {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }


    private bool SetProperty(string name, float value)
    {
        MaterialProperty property = FindProperty(name, properties, false);
        if (property != null)
        {
            property.floatValue = value;
            return true;        
        }
        return false;
    }

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (Material m in materials)
                m.EnableKeyword(keyword);
        }
        else
        {
            foreach (Material m in materials)
                m.DisableKeyword(keyword);
        }
    }

    private void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyword(keyword, value);
        }
    }

    private bool PresetButton(string name)
    {
        if (GUILayout.Button(name))
        {
            editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
}