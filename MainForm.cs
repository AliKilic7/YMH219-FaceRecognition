using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using OpenCvSharp;
using YMH219.FaceRecognition.Interfaces;
using YMH219.FaceRecognition.Models;
using YMH219.FaceRecognition.Services;

namespace YMH219.FaceRecognition
{
    /// <summary>
    /// Kamera / görüntü yüz tespiti ve tanıma arayüzü.
    /// </summary>
    public sealed class MainForm : Form
    {
        private IFaceDetectorService? _detector;
        private readonly SimpleFaceRecognizer _recognizer = new();
        private VideoCapture? _capture;
        private readonly System.Windows.Forms.Timer _cameraTimer;
        private Mat? _currentFrame;
        private readonly string _datasetPath;

        private readonly PictureBox _picDisplay;
        private readonly Button _btnStartCamera;
        private readonly Button _btnStopCamera;
        private readonly Button _btnSelectImage;
        private readonly Button _btnAddToDataset;
        private readonly Button _btnSaveFace;
        private readonly TextBox _txtLabel;
        private readonly Label _lblStatus;
        private readonly Label _lblDetectionInfo;
        private readonly Panel _statusBarPanel;

        public MainForm()
        {
            Text = "YMH 219 - Yüz Algılama ve Tanıma (OpenCvSharp4)";
            System.Drawing.Size formSize = new System.Drawing.Size(900, 650);
            ClientSize = formSize;
            MinimumSize = new System.Drawing.Size(800, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(245, 247, 250);

            _datasetPath = Path.Combine(AppContext.BaseDirectory, "Dataset");
            Directory.CreateDirectory(_datasetPath);

            // Üst kontrol paneli
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 100,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 8)
            };

            _btnStartCamera = CreateButton("Kamera Başlat", 14, 10, 120, Color.FromArgb(22, 163, 74));
            _btnStopCamera = CreateButton("Kamera Durdur", 142, 10, 120, Color.FromArgb(220, 38, 38));
            _btnStopCamera.Enabled = false;
            _btnSelectImage = CreateButton("Görüntü Seç", 270, 10, 110, Color.FromArgb(37, 99, 235));
            _btnSaveFace = CreateButton("Algılanan Yüzü Kaydet", 388, 10, 170, Color.FromArgb(100, 116, 139));

            topPanel.Controls.Add(_btnStartCamera);
            topPanel.Controls.Add(_btnStopCamera);
            topPanel.Controls.Add(_btnSelectImage);
            topPanel.Controls.Add(_btnSaveFace);

            var lblLabelPrompt = new Label
            {
                Text = "Etiket Adı:",
                Location = new System.Drawing.Point(14, 54),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            topPanel.Controls.Add(lblLabelPrompt);

            _txtLabel = new TextBox
            {
                Location = new System.Drawing.Point(92, 52),
                Width = 160,
                Font = new Font("Segoe UI", 9.5f),
                PlaceholderText = "ör. Kisi_1"
            };
            topPanel.Controls.Add(_txtLabel);

            _btnAddToDataset = CreateButton("Mevcut Yüzü Örnek Kümesine Ekle", 262, 50, 230, Color.FromArgb(15, 118, 110));
            topPanel.Controls.Add(_btnAddToDataset);

            _lblDetectionInfo = new Label
            {
                Text = "Algılama: Hazır",
                Location = new System.Drawing.Point(510, 54),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };
            topPanel.Controls.Add(_lblDetectionInfo);

            // Alt durum çubuğu
            _statusBarPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36,
                BackColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(14, 6, 14, 6)
            };

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Durum: Cascade sınıflandırıcı yükleniyor...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft
            };
            _statusBarPanel.Controls.Add(_lblStatus);

            // Merkez resim kutusu
            _picDisplay = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(15, 23, 42),
                BorderStyle = BorderStyle.FixedSingle
            };

            Controls.Add(_picDisplay);
            Controls.Add(_statusBarPanel);
            Controls.Add(topPanel);

            InitializeDetector();
            LoadDataset();

            _cameraTimer = new System.Windows.Forms.Timer { Interval = 33 }; // ~30 fps
            _cameraTimer.Tick += CameraTimer_Tick;

            _btnStartCamera.Click += BtnStartCamera_Click;
            _btnStopCamera.Click += (_, _) => StopCamera();
            _btnSelectImage.Click += BtnSelectImage_Click;
            _btnAddToDataset.Click += BtnAddToDataset_Click;
            _btnSaveFace.Click += BtnSaveFace_Click;
        }

        private static Button CreateButton(string text, int x, int y, int width, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Location = new System.Drawing.Point(x, y),
                Size = new System.Drawing.Size(width, 32),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void InitializeDetector()
        {
            try
            {
                _detector = new FaceDetectorService();
                _lblStatus.Text = "Durum: Hazır - Cascade sınıflandırıcı yüklendi.";
            }
            catch (FileNotFoundException ex)
            {
                _lblStatus.Text = "Durum: HATA - Cascade dosyası bulunamadı.";
                MessageBox.Show(
                    $"Yüz algılama cascade dosyası bulunamadı:\n{ex.Message}\n\n" +
                    "Lütfen Resources/haarcascade_frontalface_default.xml dosyasının proje dizininde olduğunu doğrulayın.",
                    "Cascade Dosyası Bulunamadı",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Durum: HATA - Algılayıcı başlatılamadı.";
                MessageBox.Show(
                    $"Yüz algılayıcı başlatılırken hata oluştu:\n{ex.Message}",
                    "Başlatma Hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadDataset()
        {
            try
            {
                _recognizer.LoadDataset(_datasetPath);
                if (_recognizer.LabelCount > 0)
                {
                    _lblDetectionInfo.Text = $"Veri Kümesi: {_recognizer.LabelCount} etiket yüklendi";
                }
            }
            catch (Exception ex)
            {
                _lblDetectionInfo.Text = "Veri kümesi yüklenemedi";
                MessageBox.Show($"Veri kümesi yüklenirken hata:\n{ex.Message}", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnStartCamera_Click(object? sender, EventArgs e)
        {
            if (_detector == null || !_detector.IsReady)
            {
                MessageBox.Show("Yüz algılayıcı hazır değil. Lütfen cascade dosyasını kontrol edin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                _capture = new VideoCapture(0);
                if (!_capture.IsOpened())
                {
                    _capture.Dispose();
                    _capture = null;
                    MessageBox.Show(
                        "Kamera açılamadı. Lütfen bilgisayarınıza bir kameranın bağlı ve erişilebilir olduğundan emin olun.",
                        "Kamera Bulunamadı",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _btnStartCamera.Enabled = false;
                _btnStopCamera.Enabled = true;
                _lblStatus.Text = "Durum: Kamera aktif - Yüz tespiti çalışıyor...";
                _statusBarPanel.BackColor = Color.FromArgb(22, 163, 74);
                _cameraTimer.Start();
            }
            catch (Exception ex)
            {
                StopCamera();
                MessageBox.Show($"Kamera başlatılırken hata oluştu:\n{ex.Message}", "Kamera Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopCamera()
        {
            _cameraTimer.Stop();
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
            _btnStartCamera.Enabled = true;
            _btnStopCamera.Enabled = false;
            _lblStatus.Text = "Durum: Kamera durduruldu.";
            _statusBarPanel.BackColor = Color.FromArgb(30, 41, 59);
        }

        private void CameraTimer_Tick(object? sender, EventArgs e)
        {
            if (_capture == null || !_capture.IsOpened() || _detector == null)
            {
                return;
            }

            try
            {
                _currentFrame?.Dispose();
                _currentFrame = new Mat();
                _capture.Read(_currentFrame);

                if (_currentFrame.Empty())
                {
                    return;
                }

                ProcessAndDisplay(_currentFrame);
            }
            catch (Exception ex)
            {
                _lblDetectionInfo.Text = $"Kare işleme hatası: {ex.Message}";
            }
        }

        private void BtnSelectImage_Click(object? sender, EventArgs e)
        {
            if (_detector == null || !_detector.IsReady)
            {
                MessageBox.Show("Yüz algılayıcı hazır değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var dialog = new OpenFileDialog
            {
                Filter = "Resim Dosyaları (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|Tüm Dosyalar|*.*",
                Title = "Yüz Algılama İçin Görüntü Seç"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                StopCamera();
                _currentFrame?.Dispose();
                _currentFrame = Cv2.ImRead(dialog.FileName);
                if (_currentFrame.Empty())
                {
                    MessageBox.Show("Görüntü dosyası okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _lblStatus.Text = $"Durum: Görüntü yüklendi - {Path.GetFileName(dialog.FileName)}";
                ProcessAndDisplay(_currentFrame);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Görüntü yüklenirken hata oluştu:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessAndDisplay(Mat frame)
        {
            if (_detector == null)
            {
                return;
            }

            IReadOnlyList<FaceDetectionResult> detections = _detector.Detect(frame);

            using var annotated = frame.Clone();
            foreach (FaceDetectionResult det in detections)
            {
                var cvRect = new Rect(det.BoundingBox.X, det.BoundingBox.Y, det.BoundingBox.Width, det.BoundingBox.Height);

                // Tanıma denemesi
                if (_recognizer.LabelCount > 0)
                {
                    try
                    {
                        using var faceRoi = new Mat(frame, cvRect);
                        (string label, double confidence) = _recognizer.Predict(faceRoi);
                        det.Label = label;
                        det.Confidence = confidence;
                    }
                    catch
                    {
                        // ROI sınır taşması olursa varsayılan etiket kalır.
                    }
                }

                // Yeşil dikdörtgen
                Cv2.Rectangle(annotated, cvRect, Scalar.FromRgb(34, 197, 94), 2);

                // Etiket ve güven skoru
                string displayText = det.ToDisplayString();
                int baseline;
                var textSize = Cv2.GetTextSize(displayText, HersheyFonts.HersheySimplex, 0.6, 2, out baseline);
                int textY = Math.Max(cvRect.Y - 10, textSize.Height + 4);

                Cv2.PutText(annotated, displayText,
                    new OpenCvSharp.Point(cvRect.X, textY),
                    HersheyFonts.HersheySimplex,
                    0.6,
                    Scalar.FromRgb(34, 197, 94),
                    2);
            }

            _lblDetectionInfo.Text = $"Algılama: {detections.Count} yüz tespit edildi" +
                                     (_recognizer.LabelCount > 0 ? $" | Veri Kümesi: {_recognizer.LabelCount} etiket" : "");

            Bitmap bmp = MatToBitmap(annotated);
            Image? old = _picDisplay.Image;
            _picDisplay.Image = bmp;
            old?.Dispose();
        }

        private void BtnAddToDataset_Click(object? sender, EventArgs e)
        {
            if (_currentFrame == null || _currentFrame.Empty())
            {
                MessageBox.Show("Önce bir kamera görüntüsü veya dosya yükleyin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(_txtLabel.Text))
            {
                MessageBox.Show("Lütfen bir etiket adı giriniz (ör. Kisi_1).", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_detector == null)
            {
                return;
            }

            try
            {
                IReadOnlyList<FaceDetectionResult> faces = _detector.Detect(_currentFrame);
                if (faces.Count == 0)
                {
                    MessageBox.Show("Mevcut karede yüz algılanamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string label = _txtLabel.Text.Trim();
                string personDir = Path.Combine(_datasetPath, label);
                Directory.CreateDirectory(personDir);

                int saved = 0;
                foreach (FaceDetectionResult face in faces)
                {
                    var cvRect = new Rect(face.BoundingBox.X, face.BoundingBox.Y, face.BoundingBox.Width, face.BoundingBox.Height);
                    using var faceRoi = new Mat(_currentFrame, cvRect);
                    string fileName = $"{label}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
                    Cv2.ImWrite(Path.Combine(personDir, fileName), faceRoi);
                    _recognizer.AddSample(label, faceRoi);
                    saved++;
                }

                _lblDetectionInfo.Text = $"{saved} yüz '{label}' etiketiyle veri kümesine eklendi.";
                MessageBox.Show($"{saved} yüz görüntüsü '{label}' klasörüne kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Veri kümesine ekleme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSaveFace_Click(object? sender, EventArgs e)
        {
            if (_currentFrame == null || _currentFrame.Empty() || _detector == null)
            {
                MessageBox.Show("Kaydedilecek bir kare bulunmuyor.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            IReadOnlyList<FaceDetectionResult> faces = _detector.Detect(_currentFrame);
            if (faces.Count == 0)
            {
                MessageBox.Show("Mevcut karede yüz algılanamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = "PNG Dosyası (*.png)|*.png|JPEG Dosyası (*.jpg)|*.jpg",
                FileName = $"yuz_{DateTime.Now:yyyyMMdd_HHmmss}.png",
                Title = "Algılanan Yüzü Kaydet"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                var cvRect = new Rect(
                    faces[0].BoundingBox.X,
                    faces[0].BoundingBox.Y,
                    faces[0].BoundingBox.Width,
                    faces[0].BoundingBox.Height);
                using var faceRoi = new Mat(_currentFrame, cvRect);
                Cv2.ImWrite(dialog.FileName, faceRoi);
                MessageBox.Show("Yüz görüntüsü başarıyla kaydedildi.", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydetme hatası:\n{ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static Bitmap MatToBitmap(Mat mat)
        {
            using var temp = new Mat();
            if (mat.Channels() == 1)
            {
                Cv2.CvtColor(mat, temp, ColorConversionCodes.GRAY2BGR);
            }
            else
            {
                mat.CopyTo(temp);
            }

            int width = temp.Cols;
            int height = temp.Rows;
            int stride = (int)temp.Step();
            var bitmap = new Bitmap(width, height, stride, PixelFormat.Format24bppRgb, temp.Data);
            return new Bitmap(bitmap); // Kopya oluşturarak Mat'ten bağımsız hale getir
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            StopCamera();
            _currentFrame?.Dispose();
            _detector?.Dispose();
            base.OnFormClosing(e);
        }
    }
}
