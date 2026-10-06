namespace Contrast.Color
{
    public static class ColorClassifier
    {
        // Color ranges:
        // White: 0 -> 84 (chunk size 84)
        // Gray: 85 -> 169 (chunk size 84)
        // Black: 170 -> 254 (chunk size 84)
        // Everything / Universal: 255 (value of everything, any player can stand)
        public const float WhiteMin = 0f;
        public const float WhiteMax = 84f;
        public const float GrayMin = 85f;
        public const float GrayMax = 169f;
        public const float BlackMin = 170f;
        public const float BlackMax = 254f;
        public const float UniversalValue = 255f;
        public const float ChunkSpan = 84f;

        public static LogicalColor Classify(float raw)
        {
            if (raw >= 254.5f)
                return LogicalColor.Universal;
            if (raw <= WhiteMax + 0.5f)
                return LogicalColor.White;
            if (raw <= GrayMax + 0.5f)
                return LogicalColor.Gray;
            return LogicalColor.Black;
        }

        public static float DefaultValue(LogicalColor c) => c switch
        {
            LogicalColor.White => 0f,
            LogicalColor.Gray => 127f,
            LogicalColor.Black => 212f,
            LogicalColor.Universal => 255f,
            _ => 0f
        };

        public static float ToValue(LogicalColor c) => c switch
        {
            LogicalColor.White => 1f,
            LogicalColor.Gray => 0.5f,
            LogicalColor.Black => 0.1f,
            LogicalColor.Universal => 0.85f,
            _ => 1f
        };

        public static UnityEngine.Color GetPlatformVisualColor(float raw)
        {
            LogicalColor logical = Classify(raw);
            switch (logical)
            {
                case LogicalColor.Universal:
                    // Distinct Gold/Amber tint for Universal (255) so players immediately
                    // see that any color can stand on this platform
                    return new UnityEngine.Color(0.96f, 0.78f, 0.22f, 1f);

                case LogicalColor.White:
                    return UnityEngine.Color.white;

                case LogicalColor.Gray:
                    return new UnityEngine.Color(0.55f, 0.55f, 0.55f, 1f);

                case LogicalColor.Black:
                default:
                    return new UnityEngine.Color(0.12f, 0.12f, 0.12f, 1f);
            }
        }

        public static string GetTypeName(LogicalColor c) => c switch
        {
            LogicalColor.White => "White (0..84)",
            LogicalColor.Gray => "Gray (85..169)",
            LogicalColor.Black => "Black (170..254)",
            LogicalColor.Universal => "Everything (255)",
            _ => "Unknown"
        };
    }
}