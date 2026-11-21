using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenCvSharp;
using YMH219.FaceRecognition.Interfaces;
using YMH219.FaceRecognition.Models;

namespace YMH219.FaceRecognition.Services
{
    /// <summary>
    /// Haar Cascade XML ile yüz tespiti yapan servis.
    /// XML dosyasını proje Resources/ klasöründen veya çalışma dizininden otomatik arar.
    /// </summary>
    public sealed class FaceDetectorService : IFaceDetectorService
    {
        private CascadeClassifier? _classifier;
        private bool _disposed;

        public bool IsReady => _classifier != null;

        public FaceDetectorService()
        {
            Initialize();
        }

        private void Initialize()
        {
            string cascadePath = FindCascadeFile();
            if (string.IsNullOrEmpty(cascadePath))
            {
                throw new FileNotFoundException(
                    "haarcascade_frontalface_default.xml dosyası bulunamadı.\n" +
                    "Lütfen Resources/ klasöründe veya çalışma dizininde bulunduğundan emin olun.");
            }

            _classifier = new CascadeClassifier(cascadePath);
            if (_classifier.Empty())
            {
                _classifier.Dispose();
                _classifier = null;
                throw new InvalidOperationException("Cascade classifier yüklenemedi. XML dosyası bozuk olabilir.");
            }
        }

        public IReadOnlyList<FaceDetectionResult> Detect(Mat frame)
        {
            if (_classifier == null)
            {
                throw new InvalidOperationException("Cascade classifier başlatılmadı.");
            }

            var results = new List<FaceDetectionResult>();

            using var gray = new Mat();
            if (frame.Channels() > 1)
            {
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
            }
            else
            {
                frame.CopyTo(gray);
            }

            Cv2.EqualizeHist(gray, gray);

            Rect[] faces = _classifier.DetectMultiScale(
                gray,
                scaleFactor: 1.1,
                minNeighbors: 5,
                flags: HaarDetectionTypes.ScaleImage,
                minSize: new OpenCvSharp.Size(50, 50));

            foreach (Rect face in faces)
            {
                var boundingBox = new Rectangle(face.X, face.Y, face.Width, face.Height);
                results.Add(new FaceDetectionResult(boundingBox));
            }

            return results;
        }

        private static string FindCascadeFile()
        {
            string fileName = "haarcascade_frontalface_default.xml";
            string[] searchPaths =
            {
                Path.Combine(AppContext.BaseDirectory, "Resources", fileName),
                Path.Combine(AppContext.BaseDirectory, fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), fileName)
            };

            foreach (string path in searchPaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return string.Empty;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _classifier?.Dispose();
        }
    }
}
