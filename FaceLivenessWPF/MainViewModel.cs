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

using FaceAiSharp;
using FlashCap;

using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using FaceLivenessWPF.Models;
using System.Windows.Media;

namespace FaceLivenessWPF
{
    public partial class MainViewModel : ObservableObject
    {
        private IFaceDetector det;
        private IFaceEmbeddingsGenerator rec;
        private CaptureDevice camera;
        private LivenessDetection ldec;
        CaptureDeviceDescriptor descriptor;
        private VideoCharacteristics characteristics;
        private Timer faceTimer;
        private byte[] lastImageBytes;
        [ObservableProperty]
        private BitmapSource source;
        public Canvas canvas;

        public MainViewModel()
        {
            det = FaceAiSharpBundleFactory.CreateFaceDetector();
            rec = FaceAiSharpBundleFactory.CreateFaceEmbeddingsGenerator();
            ldec = new LivenessDetection("OULU_Protocol_2_model_0_0.onnx");
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
            using Image<Rgb24> img = SixLabors.ImageSharp.Image.Load<Rgb24>(lastImageBytes);
            var faces = det.DetectFaces(img);
            if (faces.Count == 0)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    canvas.Children.Clear();
                });
                return;
            }
            var facesInfos = new List<FaceInfo>();
            foreach (var face in faces)
            {
                FaceInfo faceInfo = new() { FaceDetectorResult = face };
                var faceImg = img.Clone();
                rec.AlignFaceUsingLandmarks(faceImg, face.Landmarks!);
                faceInfo.LivenessScore = ldec.Invoke(faceImg);
                facesInfos.Add(faceInfo);
            }
            // Draw rectangles on the canvas
            App.Current.Dispatcher.Invoke(() =>
            {
                canvas.Children.Clear();
                foreach (var face in facesInfos)
                {

                   System.Windows.Shapes.Rectangle rect = new()
                    {
                        Width = (int)face.FaceDetectorResult.Box.Width * (canvas.Width / img.Width),
                        Height = (int)face.FaceDetectorResult.Box.Height * (canvas.Height / img.Height),
                        Stroke =  new SolidColorBrush(face.LivenessScore > 0.6 ? Colors.Green: Colors.Red),
                        StrokeThickness = 4,
                        RadiusX = 5,
                        Opacity = 0.5,
                        RadiusY = 5,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };
                    Canvas.SetLeft(rect, (int)face.FaceDetectorResult.Box.X * (canvas.Width / img.Width));
                    Canvas.SetTop(rect, (int)face.FaceDetectorResult.Box.Y * (canvas.Height / img.Height));
                    canvas.Children.Add(rect);
                    TextBlock textBlock = new()
                    {
                        Text = $"Liveness: {face.LivenessScore.ToString()}",
                        Foreground = new SolidColorBrush(Colors.White),
                        FontSize = 16
                    };
                    Canvas.SetLeft(textBlock, (int)face.FaceDetectorResult.Box.X * (canvas.Width / img.Width));
                    Canvas.SetTop(textBlock, (int)face.FaceDetectorResult.Box.Y * (canvas.Height / img.Height));
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
