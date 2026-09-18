using System.Numerics;
using System.Runtime.InteropServices;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Timing;

namespace Content.Client._Starlight.Nutrition.Overlays;

/// <summary>
/// Jagged teeth vignette shown to the starving creature - red at the tips,
/// fading to black at their base. Directly analogous to LatchVignetteOverlay
/// </summary>
public sealed partial class StarvationVignetteOverlay : Robust.Client.Graphics.Overlay
{
    [Dependency] private IGameTiming _timing = default!;
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    private const int ToothCount = 16;
    private const float ToothLengthFraction = 0.25f;

    private static readonly Color _tipColor = Color.FromSrgb(new Color(0.7f, 0f, 0f, 0.7f));
    private static readonly Color _baseColor = Color.FromSrgb(new Color(0.2f, 0f, 0.1f, 0.9f));

    // Deterministic per-tooth length variance so the row reads as jagged
    // rather than a perfectly even sawtooth.
    private static readonly float[] _toothJitter =
    {
        1f, 0.6f, 0.3f, 0.1f, -0.1f, 0.4f, -0.3f, -0.4f, -0.4f, -0.3f, 0.4f, -0.1f, 0.1f, 0.3f, 0.6f, 1f,
    };

    private readonly List<DrawVertexUV2DColor> _toothVerts = [];

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);


    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var viewport = args.ViewportBounds;
        var width = (float) viewport.Width;
        var height = (float)viewport.Height;

        var toothLength = height * ToothLengthFraction;

        _toothVerts.Clear();
        BuildToothRow(width, toothLength, fromTop: true, screenHeight: height);
        BuildToothRow(width, toothLength, fromTop: false, screenHeight: height);
        args.ScreenHandle.DrawPrimitives(DrawPrimitiveTopology.TriangleList, Texture.White, CollectionsMarshal.AsSpan(_toothVerts));
    }

    private void BuildToothRow(float width, float toothLength, bool fromTop, float screenHeight)
    {
        var toothWidth = width / ToothCount;

        for (var i = 0; i < ToothCount; i++)
        {
            var jitteredLength = toothLength * (1f + _toothJitter[i % _toothJitter.Length]);
            var xLeft = i * toothWidth;
            var xRight = xLeft + toothWidth;
            var xMid = (xLeft + xRight) / 2f;

            var baseDepth = 0;
            var tipDepth = jitteredLength;

            if (fromTop)
            {
                _toothVerts.Add(new DrawVertexUV2DColor(new Vector2(xLeft, baseDepth), Vector2.Zero, _baseColor));
                _toothVerts.Add(new DrawVertexUV2DColor(new Vector2(xRight, baseDepth), Vector2.Zero, _baseColor));
                _toothVerts.Add(new DrawVertexUV2DColor(new Vector2(xMid, tipDepth), Vector2.Zero, _tipColor));
            }
            else
            {
                _toothVerts.Add(new DrawVertexUV2DColor(new Vector2(xLeft, screenHeight - baseDepth), Vector2.Zero, _baseColor));
                _toothVerts.Add(new DrawVertexUV2DColor(new Vector2(xRight, screenHeight - baseDepth), Vector2.Zero, _baseColor));
                _toothVerts.Add(new DrawVertexUV2DColor(new Vector2(xMid, screenHeight - tipDepth), Vector2.Zero, _tipColor));
            }
        }
    }
}
