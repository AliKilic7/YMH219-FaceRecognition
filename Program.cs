using System;
using System.Windows.Forms;

namespace YMH219.FaceRecognition
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}
