using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace yolov5net_cvdet
{
    public struct DetParam
    {
        public int modelWidth;
        public int modelHeight;
        public double confidenceThreshold;
        public bool outResultBmp;
    }
    public struct DetResult
    {
        public int index;
        public string label;
        public double confidence;
        public Rect box;
    }
    public class CvDet
    {
        private Net net;
        public List<string> classNames = new List<string>();
        int inpHeight = 320;
        int inpWidth = 320;
        public bool ReadModel(string modelPath, string classesPath)
        {
            try
            {
                net = CvDnn.ReadNetFromOnnx(modelPath);
                StreamReader sr = new StreamReader(classesPath);
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    classNames.Add(line);
                }
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
        }
        public List<DetResult> Detect(Bitmap inputBmp, DetParam detParam, out Bitmap resultBmp)
        {
            if(net == null)
            {
                MessageBox.Show("模型未加载，请重新加载模型！");
                resultBmp = null;
                return new List<DetResult>();
            }
            Mat srcImage = BitmapConverter.ToMat(inputBmp);
            if(srcImage.Channels() == 1)
            {
                Cv2.CvtColor(srcImage, srcImage, ColorConversionCodes.GRAY2BGR);
            }
            int newHeight = 0, newWidth = 0, paddingHeight = 0, paddingWidth = 0;
            Mat resizeMat = ResizeImage(srcImage, out newHeight, out newWidth, out paddingHeight, out paddingWidth);
            Mat blob = CvDnn.BlobFromImage(resizeMat, 1 / 255.0, new OpenCvSharp.Size(detParam.modelWidth, detParam.modelHeight), new Scalar(0, 0, 0), true, false);
            net.SetInput(blob);
            Mat[] outBlobs = new Mat[3] { new Mat(), new Mat(), new Mat() };
            string[] outBlobNames = net.GetUnconnectedOutLayersNames().ToArray();
            net.Forward(outBlobs, outBlobNames);
            int numProposal = outBlobs[0].Size(0);
            int nout = outBlobs[0].Size(1);
            if (outBlobs[0].Dims > 2)
            {
                numProposal = outBlobs[0].Size(1);
                nout = outBlobs[0].Size(2);
                outBlobs[0] = outBlobs[0].Reshape(0, numProposal);
            }
            float ratioh = 1.0f * srcImage.Rows / detParam.modelHeight, ratiow = 1.0f * srcImage.Cols / detParam.modelWidth;
            List<Rect> boxes = new List<Rect>();
            List<float> confidences = new List<float>();
            List<int> classIds = new List<int>();
            unsafe
            {
                float* pdata = (float*)outBlobs[0].Data;
                int rowIndex = 0;
                for (int n = 0; n < numProposal; n++)
                {
                    float boxScore = pdata[4];
                    if (boxScore >= detParam.confidenceThreshold)
                    {
                        Mat scores = outBlobs[0].Row(rowIndex).ColRange(5, nout);
                        double minVal, maxVal;
                        OpenCvSharp.Point minLoc, maxLoc;
                        Cv2.MinMaxLoc(scores, out minVal, out maxVal, out minLoc, out maxLoc);
                        maxVal = maxVal * boxScore;
                        if (maxVal >= detParam.confidenceThreshold)
                        {
                            int classIndex = maxLoc.X;
                            float cx = (pdata[0] - paddingWidth) * Math.Max(ratiow, ratioh); 
                            float cy = (pdata[1] - paddingHeight) * Math.Max(ratiow, ratioh); 
                            float w = pdata[2] * Math.Max(ratiow, ratioh);
                            float h = pdata[3] * Math.Max(ratiow, ratioh);
                            int left = (int)(cx - 0.5 * w);
                            int top = (int)(cy - 0.5 * h);
                            confidences.Add((float)maxVal);
                            boxes.Add(new Rect(left, top, (int)w, (int)h));
                            classIds.Add(classIndex);
                        }
                    }
                    rowIndex++;
                    pdata += nout;
                }
            }
            int[] indices;
            float nmsThreshold = 0.5f;
            CvDnn.NMSBoxes(boxes, confidences, (float)detParam.confidenceThreshold, nmsThreshold, out indices);
            Mat resultImage = srcImage.Clone();
            List<string> ret = new List<string>();
            List<DetResult> detResults = new List<DetResult>();
            for (int i = 0; i < indices.Length; i++)
            {
                
                int idx = indices[i];
                DetResult detRet = new DetResult();
                detRet.index = classIds[idx];
                detRet.label = classNames[classIds[idx]];
                detRet.confidence = confidences[idx];
                detRet.box = boxes[idx];
                if(detParam.outResultBmp)
                {
                    Cv2.Rectangle(resultImage, new OpenCvSharp.Point(boxes[idx].X, boxes[idx].Y), new OpenCvSharp.Point(boxes[idx].X + boxes[idx].Width, boxes[idx].Y + boxes[idx].Height), new Scalar(0, 255, 0), 1);
                    Cv2.PutText(resultImage, classNames[classIds[idx]] + ":" + confidences[idx].ToString("0.00"), new OpenCvSharp.Point(boxes[idx].X, boxes[idx].Y - 5), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 2);
                }
                detResults.Add(detRet);
            }
            resultBmp = detParam.outResultBmp ? BitmapConverter.ToBitmap(resultImage) : inputBmp;
            return detResults;
        }



        public List<DetResult> DetectVedio(Mat srcImage, DetParam detParam, out Mat resultImage)
        {
            if (net == null)
            {
                MessageBox.Show("模型未加载，请重新加载模型！");
                resultImage = null;
                return new List<DetResult>();
            }

            if (srcImage.Channels() == 1)
            {
                Cv2.CvtColor(srcImage, srcImage, ColorConversionCodes.GRAY2BGR);
            }
            int newHeight = 0, newWidth = 0, paddingHeight = 0, paddingWidth = 0;
            Mat resizeMat = ResizeImage(srcImage, out newHeight, out newWidth, out paddingHeight, out paddingWidth);
            Mat blob = CvDnn.BlobFromImage(resizeMat, 1 / 255.0, new OpenCvSharp.Size(detParam.modelWidth, detParam.modelHeight), new Scalar(0, 0, 0), true, false);
            net.SetInput(blob);
            Mat[] outBlobs = new Mat[3] { new Mat(), new Mat(), new Mat() };
            string[] outBlobNames = net.GetUnconnectedOutLayersNames().ToArray();
            net.Forward(outBlobs, outBlobNames);
            int numProposal = outBlobs[0].Size(0);
            int nout = outBlobs[0].Size(1);
            if (outBlobs[0].Dims > 2)
            {
                numProposal = outBlobs[0].Size(1);
                nout = outBlobs[0].Size(2);
                outBlobs[0] = outBlobs[0].Reshape(0, numProposal);
            }
            float ratioh = 1.0f * srcImage.Rows / detParam.modelHeight, ratiow = 1.0f * srcImage.Cols / detParam.modelWidth;
            List<Rect> boxes = new List<Rect>();
            List<float> confidences = new List<float>();
            List<int> classIds = new List<int>();
            unsafe
            {
                float* pdata = (float*)outBlobs[0].Data;
                int rowIndex = 0;
                for (int n = 0; n < numProposal; n++)
                {
                    float boxScore = pdata[4];
                    if (boxScore >= detParam.confidenceThreshold)
                    {
                        Mat scores = outBlobs[0].Row(rowIndex).ColRange(5, nout);
                        double minVal, maxVal;
                        OpenCvSharp.Point minLoc, maxLoc;
                        Cv2.MinMaxLoc(scores, out minVal, out maxVal, out minLoc, out maxLoc);
                        maxVal = maxVal * boxScore;
                        if (maxVal >= detParam.confidenceThreshold)
                        {
                            int classIndex = maxLoc.X;
                            float cx = (pdata[0] - paddingWidth) * Math.Max(ratiow, ratioh);
                            float cy = (pdata[1] - paddingHeight) * Math.Max(ratiow, ratioh);
                            float w = pdata[2] * Math.Max(ratiow, ratioh);
                            float h = pdata[3] * Math.Max(ratiow, ratioh);
                            int left = (int)(cx - 0.5 * w);
                            int top = (int)(cy - 0.5 * h);
                            confidences.Add((float)maxVal);
                            boxes.Add(new Rect(left, top, (int)w, (int)h));
                            classIds.Add(classIndex);
                        }
                    }
                    rowIndex++;
                    pdata += nout;
                }
            }
            int[] indices;
            float nmsThreshold = 0.5f;
            CvDnn.NMSBoxes(boxes, confidences, (float)detParam.confidenceThreshold, nmsThreshold, out indices);
            resultImage = srcImage.Clone();
            List<string> ret = new List<string>();
            List<DetResult> detResults = new List<DetResult>();
            for (int i = 0; i < indices.Length; i++)
            {

                int idx = indices[i];
                DetResult detRet = new DetResult();
                detRet.index = classIds[idx];
                detRet.label = classNames[classIds[idx]];
                detRet.confidence = confidences[idx];
                detRet.box = boxes[idx];
                if (detParam.outResultBmp)
                {
                    Cv2.Rectangle(resultImage, new OpenCvSharp.Point(boxes[idx].X, boxes[idx].Y), new OpenCvSharp.Point(boxes[idx].X + boxes[idx].Width, boxes[idx].Y + boxes[idx].Height), new Scalar(0, 255, 0), 1);
                    Cv2.PutText(resultImage, classNames[classIds[idx]] + ":" + confidences[idx].ToString("0.00"), new OpenCvSharp.Point(boxes[idx].X, boxes[idx].Y - 5), HersheyFonts.HersheySimplex, 1, new Scalar(0, 0, 255), 3);
                }
                detResults.Add(detRet);
            }
            return detResults;
        }



        public Mat ResizeImage(Mat srcimg, out int newh, out int neww, out int top, out int left)
        {
            int srch = srcimg.Rows, srcw = srcimg.Cols;
            top = 0;
            left = 0;
            newh = inpHeight;
            neww = inpWidth;
            Mat dstimg = new Mat();
            if (srch != srcw)
            {
                float hw_scale = (float)srch / srcw;
                if (hw_scale > 1)
                {
                    newh = inpHeight;
                    neww = (int)(inpWidth / hw_scale);
                    Cv2.Resize(srcimg, dstimg, new OpenCvSharp.Size(neww, newh), 0, 0, InterpolationFlags.Area);
                    left = (int)((inpWidth - neww) * 0.5);
                    Cv2.CopyMakeBorder(dstimg, dstimg, 0, 0, left, inpWidth - neww - left, BorderTypes.Constant);
                }
                else
                {
                    newh = (int)(inpHeight * hw_scale);
                    neww = inpWidth;
                    Cv2.Resize(srcimg, dstimg, new OpenCvSharp.Size(neww, newh), 0, 0, InterpolationFlags.Area);
                    top = (int)((inpHeight - newh) * 0.5);
                    Cv2.CopyMakeBorder(dstimg, dstimg, top, inpHeight - newh - top, 0, 0, BorderTypes.Constant);
                }
            }
            else
            {
                Cv2.Resize(srcimg, dstimg, new OpenCvSharp.Size(neww, newh));
            }
            return dstimg;
        }
    }
}
