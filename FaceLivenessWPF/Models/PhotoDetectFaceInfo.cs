using System;
using SeetaFace6Sharp;

namespace FaceLivenessWPF.Models
{
    public class PhotoDetectFaceInfo
    {
        /// <summary>
        /// 人脸信息
        /// </summary>
        public FaceInfo FaceInfo { get; set; }

        /// <summary>
        /// 口罩检测
        /// </summary>
        public PlotMaskResult MaskResult { get; set; }

        /// <summary>
        /// 年龄
        /// </summary>
        public int? Age { get; set; }

        /// <summary>
        /// 识别
        /// </summary>
        public Gender? Gender { get; set; }

        /// <summary>
        /// 活体检测结果
        /// </summary>
        public AntiSpoofingResult AntiSpoofing { get; set; }

        /// <summary>
        /// 质量检测结果
        /// </summary>
        public QualityResult Quality { get; set; }

        /// <summary>
        /// 眼睛状态
        /// </summary>
        public EyeStateResult EyeState { get; set; }
    }
}
