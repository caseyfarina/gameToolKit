using TMPro;
using UnityEngine;

/// <summary>
/// Shared world-space TMP label helper for every EGTK example-scene builder. Replaces the
/// copy-pasted per-builder `Label`/`Caption` helpers that all implemented the same recipe
/// with slightly different magic numbers.
///
/// The recipe: a small TextMeshPro fontSize combined with a scaled-down GameObject keeps
/// glyphs crisp at station-board camera distances instead of blurry and tiny, and a
/// top-pivoted RectTransform keeps the label from overlapping what it labels (world-space
/// TMP is centred on the transform by default).
///
/// Harness-only tooling; never shipped.
/// </summary>
public static class SceneLabel
{
    /// <summary>
    /// The single knob for label size. This is the "2.4f" term from the original per-builder
    /// recipes, multiplied by 2.5x per reviewer feedback (labels were too small to read).
    /// Tune future sizing here — nowhere else.
    /// </summary>
    public const float FontSizeFactor = 6.0f;

    private const float ScaleFactor = 0.35f;
    private const float HeightFactor = 3.4f;

    /// <summary>
    /// Creates a world-space TMP label at <paramref name="pos"/>. <paramref name="width"/>
    /// and <paramref name="size"/> are the same per-call arguments every builder already
    /// used, so callers do not need to change their station layouts to adopt this helper.
    /// </summary>
    public static TextMeshPro Create(
        TMP_FontAsset font,
        string text,
        Vector3 pos,
        float width,
        float size,
        Color color,
        TextAlignmentOptions align = TextAlignmentOptions.Top,
        Quaternion? rotation = null)
    {
        var go = new GameObject("Label");
        go.transform.position = pos;
        if (rotation.HasValue) go.transform.rotation = rotation.Value;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.alignment = align;
        tmp.color = color;
        if (font != null) tmp.font = font;

        RectTransform rt = tmp.rectTransform;
        rt.sizeDelta = new Vector2(width / ScaleFactor, HeightFactor / ScaleFactor);
        rt.pivot = new Vector2(0.5f, 1f); // top-aligned: grows downward from the placed position
        go.transform.localScale = Vector3.one * ScaleFactor;
        tmp.fontSize = size / ScaleFactor * FontSizeFactor;
        return tmp;
    }
}
