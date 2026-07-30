using UnityEngine;

// csharpier-ignore
internal static class GizmoColors
{
    private const float FAINT_ALPHA    = 0.2f;
    private const float STRONG_ALPHA   = 0.8f;

    internal static Color None         = new(0f, 0f, 0f, 0f);

    internal static Color FaintRed     = new(1f, 0f, 0f, FAINT_ALPHA);
    internal static Color FaintGreen   = new(0f, 1f, 0f, FAINT_ALPHA);
    internal static Color FaintBlue    = new(0f, 0f, 1f, FAINT_ALPHA);
    internal static Color FaintCyan    = new(0f, 1f, 1f, FAINT_ALPHA);
    internal static Color FaintMagenta = new(1f, 0f, 1f, FAINT_ALPHA);
    internal static Color FaintYellow  = new(1f, 1f, 0f, FAINT_ALPHA);
    internal static Color FaintOrange  = new(1f, 0.5f, 0f, FAINT_ALPHA);
    internal static Color FaintPurple  = new(0.5f, 0f, 1f, FAINT_ALPHA);
    internal static Color FaintWhite   = new(1f, 1f, 1f, FAINT_ALPHA);
    internal static Color FaintBlack   = new(0f, 0f, 0f, FAINT_ALPHA);
    internal static Color FaintGray    = new(0.5f, 0.5f, 0.5f, FAINT_ALPHA);

    internal static Color Red          = new(1f, 0f, 0f, STRONG_ALPHA);
    internal static Color Green        = new(0f, 1f, 0f, STRONG_ALPHA);
    internal static Color Blue         = new(0f, 0f, 1f, STRONG_ALPHA);
    internal static Color Cyan         = new(0f, 1f, 1f, STRONG_ALPHA);
    internal static Color Magenta      = new(1f, 0f, 1f, STRONG_ALPHA);
    internal static Color Yellow       = new(1f, 1f, 0f, STRONG_ALPHA);
    internal static Color Orange       = new(1f, 0.5f, 0f, STRONG_ALPHA);
    internal static Color Purple       = new(0.5f, 0f, 1f, STRONG_ALPHA);
    internal static Color White        = new(1f, 1f, 1f, STRONG_ALPHA);
    internal static Color Black        = new(0f, 0f, 0f, STRONG_ALPHA);
    internal static Color Gray         = new(0.5f, 0.5f, 0.5f, STRONG_ALPHA);
}
