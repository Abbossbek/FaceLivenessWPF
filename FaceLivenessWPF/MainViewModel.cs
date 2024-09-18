using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using CommunityToolkit.Mvvm.ComponentModel;

using FlashCap;

using FaceLivenessWPF.Models;
using System.Windows.Media;
using SeetaFace6Sharp.Extension.DependencyInjection;
using SeetaFace6Sharp;
using SkiaSharp;
using static System.Net.Mime.MediaTypeNames;

namespace FaceLivenessWPF
{
    public partial class MainViewModel : ObservableObject
    {
        private SeetaFace6SharpFactory _faceFactory;
        private CaptureDevice camera;
        CaptureDeviceDescriptor descriptor;
        private VideoCharacteristics characteristics;
        private Timer faceTimer;
        private byte[] lastImageBytes;
        [ObservableProperty]
        private BitmapSource source;
        public Canvas canvas;

        public MainViewModel()
        {
        }
        public async void Loaded()
        {
            // Capture device enumeration:
            var devices = new CaptureDevices();
            var descriptors = devices.EnumerateDescriptors();
            if (descriptors.Count() == 0)
            {
                MessageBox.Show("No camera detected.");
                return;
            }
            descriptor = descriptors.FirstOrDefault(x => x.Characteristics.Length > 0);
            characteristics = descriptor.Characteristics.FirstOrDefault(x => x.PixelFormat != FlashCap.PixelFormats.Unknown);

            _faceFactory = new SeetaFace6SharpFactory(characteristics.Width, characteristics.Height);

            //Console.WriteLine($"Characteristics: {characteristics}");
            camera = await descriptor.OpenAsync(
                characteristics,
                async bufferScope =>
                {
                    // Captured into a pixel buffer from an argument.

                    // Get image data (Maybe DIB/JPEG/PNG):            
                    lastImageBytes = bufferScope.Buffer.ExtractImage();
                    //Source?.Dispose();
                    Source = ToBitmapSource(lastImageBytes);

                    // ...
                });
            await camera.StartAsync();

            faceTimer = new(faceTimerCallback, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(300));
        }
        private async void faceTimerCallback(object state)
        {
            GC.Collect();
            if (lastImageBytes == null) return;
            using SKBitmap bitmap = SKBitmap.Decode(lastImageBytes);
            using FaceImage img = bitmap.ToFaceImage();
            var faceInfos = _faceFactory.Get<FaceDetector>()?.Detect(img);
            if (faceInfos.Length == 0)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    canvas.Children.Clear();
                });
                return;
            }
            var facesInfos = new List<PhotoDetectFaceInfo>();
            foreach (var face in faceInfos)
            {
                FaceMarkPoint[] points = _faceFactory.Get<FaceLandmarker>()?.Mark(img, face);
                PhotoDetectFaceInfo pdfi = new() {
                    FaceInfo = face,
                    MaskResult = _faceFactory.Get<MaskDetector>()?.Detect(img, face),
                    Age = _faceFactory.Get<AgePredictor>()?.PredictAgeWithCrop(img, points),
                    AntiSpoofing = _faceFactory.Get<FaceAntiSpoofing>()?.Predict(img, face, points),
                    Quality = _faceFactory.Get<FaceQuality>()?.Detect(img, face, points, QualityType.Clarity),
                    EyeState = _faceFactory.Get<EyeStateDetector>()?.Detect(img, points),
                    Gender = _faceFactory.Get<GenderPredictor>()?.PredictGenderWithCrop(img, points)
                };
                facesInfos.Add(pdfi);
            }
            // Draw rectangles on the canvas
            App.Current.Dispatcher.Invoke(() =>
            {
                canvas.Children.Clear();
                foreach (var face in facesInfos)
                {

                   System.Windows.Shapes.Rectangle rect = new()
                    {
                        Width = (int)face.FaceInfo.Location.Width * (canvas.Width / img.Width),
                        Height = (int)face.FaceInfo.Location.Height * (canvas.Height / img.Height),
                        Stroke =  new SolidColorBrush(face.AntiSpoofing?.Status == AntiSpoofingStatus.Real ? Colors.Green: Colors.Red),
                        StrokeThickness = 4,
                        RadiusX = 5,
                        Opacity = 0.5,
                        RadiusY = 5,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };
                    Canvas.SetLeft(rect, (int)face.FaceInfo.Location.X * (canvas.Width / img.Width));
                    Canvas.SetTop(rect, (int)face.FaceInfo.Location.Y * (canvas.Height / img.Height));
                    canvas.Children.Add(rect);
                    TextBlock textBlock = new()
                    {
                        Text = $"Liveness: {face.AntiSpoofing.Reality.ToString()}",
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 16
                    };
                    Canvas.SetLeft(textBlock, (int)face.FaceInfo.Location.X * (canvas.Width / img.Width));
                    Canvas.SetTop(textBlock, (int)face.FaceInfo.Location.Y * (canvas.Height / img.Height));
                    canvas.Children.Add(textBlock);
                }

            });

          
        }
        public static BitmapSource ToBitmapSource(byte[] bytes)
        {
            using (var stream = new MemoryStream(bytes))
            {
                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }
    }
}
