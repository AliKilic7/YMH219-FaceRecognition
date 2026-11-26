using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenCvSharp;
using YMH219.FaceRecognition.Interfaces;

namespace YMH219.FaceRecognition.Services
{
    /// <summary>
    /// Yerel Dataset/ klasöründeki etiketli yüz görüntülerinden histogram tabanlı
    /// karşılaştırma ile basit yüz tanıma gerçekleştiren eğitsel sınıf.
    /// </summary>
    public sealed class SimpleFaceRecognizer : IFaceRecognizerService, IDisposable
    {
        private sealed class LabeledHistogram : IDisposable
        {
            public string Label { get; }
            public Mat Histogram { get; }

            public LabeledHistogram(string label, Mat histogram)
            {
                Label = label;
                Histogram = histogram;
            }

            public void Dispose()
            {
                Histogram.Dispose();
            }
        }

        private readonly List<LabeledHistogram> _samples = new();

        public int LabelCount => _samples.Select(s => s.Label).Distinct(StringComparer.OrdinalIgnoreCase).Count();

        public void LoadDataset(string datasetPath)
        {
            if (!Directory.Exists(datasetPath))
            {
                Directory.CreateDirectory(datasetPath);
                return;
            }

            foreach (string personDir in Directory.GetDirectories(datasetPath))
            {
                string label = Path.GetFileName(personDir);
                string[] imageFiles = Directory.GetFiles(personDir, "*.*")
                    .Where(f => IsImageFile(f))
                    .ToArray();

                foreach (string imageFile in imageFiles)
                {
                    try
                    {
                        using var img = Cv2.ImRead(imageFile, ImreadModes.Grayscale);
                        if (img.Empty())
                        {
                            continue;
                        }

                        using var resized = new Mat();
                        Cv2.Resize(img, resized, new OpenCvSharp.Size(100, 100));
                        Cv2.EqualizeHist(resized, resized);
                        Mat histogram = ComputeHistogram(resized);
                        _samples.Add(new LabeledHistogram(label, histogram));
                    }
                    catch
                    {
                        // Okunamayan görüntü dosyaları atlanır.
                    }
                }
            }
        }

        public void AddSample(string label, Mat faceImage)
        {
            if (string.IsNullOrWhiteSpace(label) || faceImage == null || faceImage.Empty())
            {
                return;
            }

            using var gray = new Mat();
            if (faceImage.Channels() > 1)
            {
                Cv2.CvtColor(faceImage, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                faceImage.CopyTo(gray);
            }

            using var resized = new Mat();
            Cv2.Resize(gray, resized, new OpenCvSharp.Size(100, 100));
            Cv2.EqualizeHist(resized, resized);
            Mat histogram = ComputeHistogram(resized);
            _samples.Add(new LabeledHistogram(label.Trim(), histogram));
        }

        public (string Label, double Confidence) Predict(Mat faceImage)
        {
            if (_samples.Count == 0)
            {
                return ("Bilinmeyen", 0.0);
            }

            using var gray = new Mat();
            if (faceImage.Channels() > 1)
            {
                Cv2.CvtColor(faceImage, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                faceImage.CopyTo(gray);
            }

            using var resized = new Mat();
            Cv2.Resize(gray, resized, new OpenCvSharp.Size(100, 100));
            Cv2.EqualizeHist(resized, resized);
            using Mat queryHist = ComputeHistogram(resized);

            string bestLabel = "Bilinmeyen";
            double bestScore = -1.0;

            foreach (LabeledHistogram sample in _samples)
            {
                double correlation = Cv2.CompareHist(queryHist, sample.Histogram, HistCompMethods.Correl);
                if (correlation > bestScore)
                {
                    bestScore = correlation;
                    bestLabel = sample.Label;
                }
            }

            // Korelasyon [-1, 1] → güven [0%, 100%] dönüşümü
            double confidence = Math.Clamp((bestScore + 1.0) / 2.0 * 100.0, 0.0, 100.0);
            return (bestLabel, confidence);
        }

        private static Mat ComputeHistogram(Mat grayImage)
        {
            Mat hist = new();
            int[] channels = { 0 };
            int[] histSize = { 256 };
            Rangef[] ranges = { new Rangef(0f, 256f) };
            Cv2.CalcHist(new[] { grayImage }, channels, null, hist, 1, histSize, ranges);
            Cv2.Normalize(hist, hist, 0, 1, NormTypes.MinMax);
            return hist;
        }

        private static bool IsImageFile(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext is ".jpg" or ".jpeg" or ".png" or ".bmp" or ".webp";
        }

        public void Dispose()
        {
            foreach (LabeledHistogram sample in _samples)
            {
                sample.Dispose();
            }
            _samples.Clear();
        }
    }
}
