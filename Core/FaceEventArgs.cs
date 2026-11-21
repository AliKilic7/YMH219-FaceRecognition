using System;

namespace YMH219.FaceRecognition.Core
{
    public sealed class FaceDetectionEventArgs : EventArgs
    {
        public int FaceCount { get; }

        public FaceDetectionEventArgs(int faceCount)
        {
            FaceCount = faceCount;
        }
    }

    public sealed class DetectionStatusEventArgs : EventArgs
    {
        public string StatusText { get; }
        public bool IsError { get; }

        public DetectionStatusEventArgs(string statusText, bool isError = false)
        {
            StatusText = statusText;
            IsError = isError;
        }
    }
}
