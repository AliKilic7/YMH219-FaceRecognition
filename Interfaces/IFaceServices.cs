using System;
using System.Collections.Generic;
using YMH219.FaceRecognition.Models;

namespace YMH219.FaceRecognition.Interfaces
{
    public interface IFaceDetectorService : IDisposable
    {
        bool IsReady { get; }
        IReadOnlyList<FaceDetectionResult> Detect(OpenCvSharp.Mat frame);
    }

    public interface IFaceRecognizerService
    {
        int LabelCount { get; }
        void LoadDataset(string datasetPath);
        void AddSample(string label, OpenCvSharp.Mat faceImage);
        (string Label, double Confidence) Predict(OpenCvSharp.Mat faceImage);
    }
}
