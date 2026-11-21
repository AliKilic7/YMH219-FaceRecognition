using System.Drawing;

namespace YMH219.FaceRecognition.Models
{
    /// <summary>
    /// Tek bir algılanan yüzün sınır kutusu, etiket tahmini ve güven skorunu tutar.
    /// </summary>
    public sealed class FaceDetectionResult
    {
        public Rectangle BoundingBox { get; }
        public string Label { get; set; }
        public double Confidence { get; set; }

        public FaceDetectionResult(Rectangle boundingBox, string label = "Bilinmeyen", double confidence = 0.0)
        {
            BoundingBox = boundingBox;
            Label = label;
            Confidence = confidence;
        }

        public string ToDisplayString()
        {
            return $"{Label} (%{Confidence:F1})";
        }
    }
}
