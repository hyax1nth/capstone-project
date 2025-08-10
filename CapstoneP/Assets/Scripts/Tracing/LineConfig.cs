using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LineConfig : MonoBehaviour
{
    [Header("Line Appearance")]
    [SerializeField] private Material lineMaterial;
    [SerializeField] private float width = 0.1f;
    [SerializeField] private AnimationCurve widthCurve;

    private void Awake()
    {
        LineRenderer lr = GetComponent<LineRenderer>();

        // Apply Material
        if (lineMaterial != null)
            lr.material = lineMaterial;

        // Width setup
        lr.widthMultiplier = width;

        // Optional width taper
        if (widthCurve != null && widthCurve.length > 0)
            lr.widthCurve = widthCurve;

        // Recommended settings for clear strokes
        lr.useWorldSpace = true;
        lr.alignment = LineAlignment.View;
        lr.sortingLayerName = "Default";
        lr.sortingOrder = 10;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
    }
}
