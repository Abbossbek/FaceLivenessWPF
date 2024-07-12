using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.IO;
using System.Net;

using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Tensor = Microsoft.ML.OnnxRuntime.Tensors.Tensor<float>;
namespace FaceLivenessWPF
{


    public class LivenessDetection
    {
        private InferenceSession deepPix;

        public LivenessDetection(string checkpointPath)
        {
            if (!File.Exists(checkpointPath))
            {
                Console.WriteLine("Downloading the DeepPixBiS onnx checkpoint:");
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadFile(
                        "https://github.com/ffletcherr/face-recognition-liveness/releases/download/v0.1/OULU_Protocol_2_model_0_0.onnx",
                        checkpointPath
                    );
                }
            }

            deepPix = new InferenceSession(checkpointPath, new SessionOptions { ExecutionMode = ExecutionMode.ORT_SEQUENTIAL });
        }

        public float Invoke(Image<Rgb24> image)
        {
            image.Mutate(ctx => ctx.Resize(new ResizeOptions
            {
                Size = new SixLabors.ImageSharp.Size(224, 224),
                Mode = ResizeMode.Stretch
            }));

            var mean = new[] { 0.485f, 0.456f, 0.406f };
            var std = new[] { 0.229f, 0.224f, 0.225f };
            var faceTensor = new DenseTensor<float>(new[] { 1, 3, 224, 224 });

            for (int y = 0; y < 224; y++)
            {
                for (int x = 0; x < 224; x++)
                {
                    var pixel = image[x, y];
                    faceTensor[0, 0, y, x] = (pixel.R / 255.0f - mean[0]) / std[0];
                    faceTensor[0, 1, y, x] = (pixel.G / 255.0f - mean[1]) / std[1];
                    faceTensor[0, 2, y, x] = (pixel.B / 255.0f - mean[2]) / std[2];
                }
            }

            var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input", faceTensor)
        };
            {
                using (var results = deepPix.Run(inputs))
                {
                    var outputPixel = results.First(x => x.Name == "output_pixel").AsTensor<float>().ToArray();
                    var outputBinary = results.First(x => x.Name == "output_binary").AsTensor<float>().ToArray();
                    float livenessScore = (outputPixel.Average() + outputBinary.Average()) / 2.0f;
                    return livenessScore;
                }
            }
        }
    }

}
