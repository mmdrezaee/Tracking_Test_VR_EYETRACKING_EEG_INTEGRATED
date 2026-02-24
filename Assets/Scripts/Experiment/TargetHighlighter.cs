using System.Collections.Generic;
using UnityEngine;

public class TargetHighlighter : MonoBehaviour
{
    public List<Renderer> renderers = new List<Renderer>();
    public bool includeInactive = true;

    [Header("Highlight")]
    public Color highlightColor = Color.green;

    [Tooltip("HDRP Lit commonly uses _BaseColor. Built-in Standard uses _Color.")]
    public string colorProperty = "_BaseColor";

    private MaterialPropertyBlock _mpb;
    private int _propId;

    private readonly Dictionary<Renderer, Color> _original = new Dictionary<Renderer, Color>();

    void Awake()
    {
        _mpb = new MaterialPropertyBlock();
        _propId = Shader.PropertyToID(colorProperty);

        if (renderers.Count == 0)
            AutoCollectRenderers();

        CacheOriginalColors();
    }

    public void AutoCollectRenderers()
    {
        renderers.Clear();
        var rs = GetComponentsInChildren<Renderer>(includeInactive);
        renderers.AddRange(rs);
    }

    private void CacheOriginalColors()
    {
        _original.Clear();

        foreach (var r in renderers)
        {
            if (r == null) continue;

            // Try to read existing property from the material
            Color baseCol = Color.white;
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(_propId))
                baseCol = r.sharedMaterial.GetColor(_propId);

            _original[r] = baseCol;
        }
    }

    public void SetHighlighted(bool on)
    {
        if (_mpb == null) _mpb = new MaterialPropertyBlock();

        foreach (var r in renderers)
        {
            if (r == null) continue;

            r.GetPropertyBlock(_mpb);

            Color col = highlightColor;
            if (!on && _original.TryGetValue(r, out Color orig))
                col = orig;

            _mpb.SetColor(_propId, col);
            r.SetPropertyBlock(_mpb);
        }
    }
}
