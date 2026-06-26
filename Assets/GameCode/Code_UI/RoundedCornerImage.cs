using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Sprites;

[AddComponentMenu("UI/Rounded Corner Image")]
[RequireComponent(typeof(Image))]
public class RoundedCornerImage : BaseMeshEffect
{
    [System.Serializable]
    public class CornerToggles
    {
        public bool topLeft = true;
        public bool topRight = true;
        public bool bottomLeft = true;
        public bool bottomRight = true;
    }

    [Header("Corners")]
    [SerializeField] private CornerToggles _corners = new CornerToggles();

    [Header("Corner Radius")]
    [Range(0, 200)]
    [SerializeField] private float _cornerRadius = 10f;

    [Header("Quality")]
    [Range(3, 32)]
    [SerializeField] private int _segments = 8;


    private Image _image;

    // ── Corner properties ──────────────────────────────────────

    public bool RoundTopLeft
    {
        get => _corners != null && _corners.topLeft;
        set { if (_corners == null) _corners = new CornerToggles(); _corners.topLeft = value; graphic.SetVerticesDirty(); }
    }

    public bool RoundTopRight
    {
        get => _corners != null && _corners.topRight;
        set { if (_corners == null) _corners = new CornerToggles(); _corners.topRight = value; graphic.SetVerticesDirty(); }
    }

    public bool RoundBottomLeft
    {
        get => _corners != null && _corners.bottomLeft;
        set { if (_corners == null) _corners = new CornerToggles(); _corners.bottomLeft = value; graphic.SetVerticesDirty(); }
    }

    public bool RoundBottomRight
    {
        get => _corners != null && _corners.bottomRight;
        set { if (_corners == null) _corners = new CornerToggles(); _corners.bottomRight = value; graphic.SetVerticesDirty(); }
    }

    public float CornerRadius
    {
        get => _cornerRadius;
        set { _cornerRadius = Mathf.Max(0, value); graphic.SetVerticesDirty(); }
    }

    public int Segments
    {
        get => _segments;
        set { _segments = Mathf.Max(3, value); graphic.SetVerticesDirty(); }
    }

    // ── Unity lifecycle ────────────────────────────────────────

    protected override void Awake()
    {
        base.Awake();
        _image = GetComponent<Image>();
        if (_corners == null)
            _corners = new CornerToggles();
    }

    protected override void OnValidate()
    {
        base.OnValidate();
        _cornerRadius = Mathf.Max(0, _cornerRadius);
        _segments = Mathf.Max(3, _segments);
        if (_corners == null)
            _corners = new CornerToggles();
        var g = GetComponent<Graphic>();
        if (g != null)
            g.SetVerticesDirty();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive())
            return;

        Rect rect = graphic.GetPixelAdjustedRect();
        if (rect.width <= 0f || rect.height <= 0f)
            return;

        GenerateRoundedMesh(vh);
    }

    // ── Mesh generation ────────────────────────────────────────

    private void GenerateRoundedMesh(VertexHelper vh)
    {
        vh.Clear();

        Rect rect = graphic.GetPixelAdjustedRect();
        float maxRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
        float radius = Mathf.Clamp(_cornerRadius, 0f, maxRadius);
        int segs = Mathf.Max(1, _segments);

        // Per-corner radii – null-safe in case serialization hasn't created the object yet
        bool tl = _corners != null && _corners.topLeft;
        bool tr = _corners != null && _corners.topRight;
        bool br = _corners != null && _corners.bottomRight;
        bool bl = _corners != null && _corners.bottomLeft;

        float tlRadius = tl ? radius : 0f;
        float trRadius = tr ? radius : 0f;
        float brRadius = br ? radius : 0f;
        float blRadius = bl ? radius : 0f;

        // Sprite UV source
        if (_image == null)
            _image = GetComponent<Image>();
        Vector4 outerUv = (_image != null && _image.overrideSprite != null)
            ? DataUtility.GetOuterUV(_image.overrideSprite)
            : new Vector4(0, 0, 1, 1);

        Color32 imageColor = graphic.color;

        var perimeter = new List<Vector3>();
        var uvs = new List<Vector2>();
        BuildPerimeter(perimeter, uvs, rect,
            tlRadius, trRadius, brRadius, blRadius, segs, outerUv);

        Vector2 center = rect.center;
        int centerIndex = 0;
        vh.AddVert(new Vector3(center.x, center.y, 0),
            imageColor, GetUv(center, rect, outerUv));

        for (int i = 0; i < perimeter.Count; i++)
            vh.AddVert(perimeter[i], imageColor, uvs[i]);

        for (int i = 0; i < perimeter.Count - 1; i++)
            vh.AddTriangle(centerIndex, i + 1, i + 2);
        if (perimeter.Count >= 3)
            vh.AddTriangle(centerIndex, perimeter.Count, 1);
    }

    // ── Perimeter helpers ──────────────────────────────────────

    private static void BuildPerimeter(
        List<Vector3> verts,
        List<Vector2> uvs,
        Rect rect,
        float tlRadius, float trRadius,
        float brRadius, float blRadius,
        int segments,
        Vector4 outerUv)
    {
        float xMin = rect.xMin;
        float xMax = rect.xMax;
        float yMin = rect.yMin;
        float yMax = rect.yMax;

        Vector2 tlCenter = new Vector2(xMin + tlRadius, yMax - tlRadius);
        Vector2 trCenter = new Vector2(xMax - trRadius, yMax - trRadius);
        Vector2 brCenter = new Vector2(xMax - brRadius, yMin + brRadius);
        Vector2 blCenter = new Vector2(xMin + blRadius, yMin + blRadius);

        AddCorner(verts, uvs, tlCenter, new Vector2(xMin, yMax),
            tlRadius, Mathf.PI, Mathf.PI * 0.5f, segments, rect, outerUv);
        AddCorner(verts, uvs, trCenter, new Vector2(xMax, yMax),
            trRadius, Mathf.PI * 0.5f, 0f, segments, rect, outerUv);
        AddCorner(verts, uvs, brCenter, new Vector2(xMax, yMin),
            brRadius, 0f, -Mathf.PI * 0.5f, segments, rect, outerUv);
        AddCorner(verts, uvs, blCenter, new Vector2(xMin, yMin),
            blRadius, -Mathf.PI * 0.5f, -Mathf.PI, segments, rect, outerUv);
    }

    private static void AddCorner(
        List<Vector3> perimeter,
        List<Vector2> uvs,
        Vector2 center,
        Vector2 sharpCorner,
        float radius,
        float startAngle,
        float endAngle,
        int segments,
        Rect rect,
        Vector4 outerUv)
    {
        if (radius <= 0f)
        {
            AddVertex(perimeter, uvs, sharpCorner, rect, outerUv);
            return;
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            var point = new Vector2(
                center.x + radius * Mathf.Cos(angle),
                center.y + radius * Mathf.Sin(angle));

            AddVertex(perimeter, uvs, point, rect, outerUv);
        }
    }

    private static void AddVertex(
        List<Vector3> perimeter,
        List<Vector2> uvs,
        Vector2 point,
        Rect rect,
        Vector4 outerUv)
    {
        perimeter.Add(new Vector3(point.x, point.y, 0f));
        uvs.Add(GetUv(point, rect, outerUv));
    }

    private static Vector2 GetUv(Vector2 point, Rect rect, Vector4 outerUv)
    {
        float normalizedX = Mathf.InverseLerp(rect.xMin, rect.xMax, point.x);
        float normalizedY = Mathf.InverseLerp(rect.yMin, rect.yMax, point.y);

        return new Vector2(
            Mathf.Lerp(outerUv.x, outerUv.z, normalizedX),
            Mathf.Lerp(outerUv.y, outerUv.w, normalizedY));
    }
}
