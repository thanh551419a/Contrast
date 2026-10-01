namespace Contrast.Color
{
    public static class ColorClassifier
    {
        // Ngưỡng phân loại rawGrayscaleColor (0–255). Sửa cho khớp luật màu của bạn.
        public const float GrayMin = 85f;
        public const float WhiteMin = 170f;

        public static LogicalColor Classify(float raw)
            => raw < GrayMin ? LogicalColor.Black
             : raw < WhiteMin ? LogicalColor.Gray
             : LogicalColor.White;

        public static float ToValue(LogicalColor c)
            => c == LogicalColor.Black ? 0.1f : c == LogicalColor.Gray ? 0.5f : 1f;
    }
}