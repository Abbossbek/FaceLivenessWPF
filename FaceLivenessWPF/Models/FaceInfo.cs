using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FaceAiSharp;

namespace FaceLivenessWPF.Models
{
    public class FaceInfo
    {
        public FaceDetectorResult FaceDetectorResult { get; set; }
        public float LivenessScore { get; set; }
    }
}
