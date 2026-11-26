# YMH219 - Yüz Algılama ve Tanıma Uygulaması (FaceRecognition)

Nesne Tabanlı Programlama (YMH 219) dersi kapsamında .NET 8.0 Windows Forms ve `OpenCvSharp4` kullanılarak geliştirilmiş, Haar Cascade ile yüz tespiti ve histogram karşılaştırması ile basit yüz tanıma gerçekleştiren eğitsel masaüstü uygulamasıdır.

## Öğrenci Bilgileri
- **Ad Soyad:** Selahattin Ali Kılıç
- **Öğrenci No:** 250541036
- **Ders:** YMH 219 - Nesne Tabanlı Programlama

## Kullanılan Teknoloji / NuGet Paketleri ve OOP Mimarisi
- **Platform:** .NET 8.0 Windows Forms (`net8.0-windows`)
- **NuGet Paketleri:** `OpenCvSharp4`, `OpenCvSharp4.Windows`
- **Cascade Dosyası:** `haarcascade_frontalface_default.xml` (proje `Resources/` dizininde gömülü)
- **OOP Mimarisi:**
  - **Interfaces:** `IFaceDetectorService` ve `IFaceRecognizerService` arayüzleri algılama ve tanıma bileşenlerini soyutlar.
  - **Models:** `FaceDetectionResult` (sınır kutusu, etiket, güven skoru) model sınıfı.
  - **Services & Encapsulation:** `FaceDetectorService` (`CascadeClassifier` sarmalayıcı) ve `SimpleFaceRecognizer` (histogram karşılaştırması ile etiket ve güven skoru hesaplama).
  - **Custom EventArgs:** `FaceDetectionEventArgs` ve `DetectionStatusEventArgs`.

## Kurulum ve Çalıştırma Adımları
```bash
cd YMH219-FaceRecognition
dotnet restore
dotnet build
dotnet run
```

## Yerel Veri Kümesi Oluşturma Kılavuzu
1. Uygulamayı açın ve kameranızı başlatın (veya bir görüntü dosyası seçin).
2. **Etiket Adı** kutusuna kişinin adını yazın (ör. `Kisi_1`).
3. **"Mevcut Yüzü Örnek Kümesine Ekle"** butonuna basın.
4. Farklı açılardan birkaç kare ekleyerek veri kümesini zenginleştirin.
5. Uygulama yeniden başlatıldığında `Dataset/Kisi_1/` klasöründeki görüntüler otomatik yüklenir.

## Uygulamanın Temel Özellikleri
- **Kamera Başlat / Durdur:** Gerçek zamanlı web kamerası akışında yüz tespiti (~30 fps).
- **Görüntü Seç:** `OpenFileDialog` ile statik görüntüde yüz algılama.
- **Yeşil Dikdörtgen ve Etiket:** `PictureBox` üzerinde algılanan yüzler yeşil dikdörtgen içine alınır, tahmin edilen etiket ve güven skoru (`Kisi_1 (%87.4)`) yazılır.
- **Mevcut Yüzü Örnek Kümesine Ekle:** Algılanan yüzü etiket adıyla `Dataset/` klasörüne kaydeder.
- **Algılanan Yüzü Kaydet:** İlk algılanan yüzü PNG/JPG olarak dışa aktarır.
- **Hata Koruması:** Kamera veya cascade dosyası bulunamadığında `try-catch` ile çökmeden uyarı verilir.

## Ekran Görüntüleri
![Ekran 1](screenshots/ekran1.png)
![Ekran 2](screenshots/ekran2.png)

## Güvenlik Sınırı ve Etik Kapsam
Bu uygulama yalnızca yerel örnek küme ve kullanıcının seçtiği görüntü/kamerada çalışan eğitsel bir demonstrasyon projesidir. Herhangi bir uzaktan yüz verisi toplama veya bulut tabanlı yüz tanıma servisi içermez.

## Bilinen Sınırlamalar / Geliştirme Notları
- Tanıma algoritması histogram korelasyonuna dayalı basit bir eğitsel yöntemdir; üretim düzeyinde doğruluk beklenmemelidir.
- Daha gelişmiş tanıma için LBPH, Eigenface veya derin öğrenme tabanlı yöntemlere geçiş düşünülebilir.
