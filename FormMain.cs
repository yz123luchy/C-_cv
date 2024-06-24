using Ookii.Dialogs.WinForms;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ZXing;
using ZXing.Common;
using ZXing.Multi;
using static System.Net.Mime.MediaTypeNames;
using Point = OpenCvSharp.Point;

namespace yolov5net_cvdet
{
    public partial class FormMain : Form
    {
        Bitmap bmp;
        CvDet det = new CvDet();
        public FormMain()
        {
            InitializeComponent();
        }
        private void FormMain_Load(object sender, EventArgs e)
        {
            tboxModelPath.Text = $"{System.Windows.Forms.Application.StartupPath}/yolov5n.onnx";
            tboxClassPath.Text = $"{System.Windows.Forms.Application.StartupPath}/cocodata.txt";
            if (File.Exists($"{System.Windows.Forms.Application.StartupPath}/bus.jpg"))
            {
                bmp = new Bitmap($"{System.Windows.Forms.Application.StartupPath}/bus.jpg");
                AddPictureBoxesToTableLayoutPanel(2, tableLayoutPanel1);
                foreach (Control control in tableLayoutPanel1.Controls)
                {
                    if (control is PictureBox pictureBox)
                    {
                        pictureBox.Image = bmp;
                        break;
                    }
                }
                //picBoxSrc.Image = bmp;
            }
        }
        private void btnOpenImage_Click(object sender, EventArgs e)
        {
            VistaOpenFileDialog vofd = new VistaOpenFileDialog();
            if (vofd.ShowDialog() == DialogResult.OK)
            {
                bmp = new Bitmap(vofd.FileName);
            }

            foreach (Control control in tableLayoutPanel1.Controls)
            {
                if (control is PictureBox pictureBox)
                {
                    pictureBox.Image = bmp;
                    break;
                }
            }
            //picBoxSrc.Image = bmp;
        }
        private void btnLoadModel_Click(object sender, EventArgs e)
        {
            bool ret = det.ReadModel(tboxModelPath.Text, tboxClassPath.Text);
            rtboxMsg.AppendText(ret ? "模型加载成功\n" : "模型加载失败\n");
        }


        private void tboxModelPath_DoubleClick(object sender, EventArgs e)
        {
            VistaOpenFileDialog vofd = new VistaOpenFileDialog();
            vofd.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            if (vofd.ShowDialog() == DialogResult.OK)
            {
                tboxModelPath.Text = vofd.FileName;
            }
        }

        private void tboxClassPath_DoubleClick(object sender, EventArgs e)
        {
            VistaOpenFileDialog vofd = new VistaOpenFileDialog();
            vofd.InitialDirectory = System.Windows.Forms.Application.StartupPath;
            if (vofd.ShowDialog() == DialogResult.OK)
            {
                tboxModelPath.Text = vofd.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DetParam param = new DetParam
            {
                modelWidth = 320,
                modelHeight = 320,
                confidenceThreshold = 0.5,
                outResultBmp = true
            };
            string VideoFilePath = "";
            VistaOpenFileDialog vofd = new VistaOpenFileDialog();
            if (vofd.ShowDialog() == DialogResult.OK)
            {
                VideoFilePath = vofd.FileName;
            }
            if (VideoFilePath == "")
            {
                return;
            }
            VideoCapture capture = new VideoCapture(VideoFilePath);
            if (!capture.IsOpened())
            {
                Console.WriteLine("video not open!");
                return;
            }
            Mat frame = new Mat();
            var sw = new Stopwatch();
            int fps = 0;
            while (true)
            {

                capture.Read(frame);
                if (frame.Empty())
                {
                    Console.WriteLine("data is empty!");
                    break;
                }
                sw.Start();
                List<DetResult> ret = det.DetectVedio(frame, param, out Mat result);
                sw.Stop();
                if (result == null)
                {
                    return;
                }
                fps = Convert.ToInt32(1 / sw.Elapsed.TotalSeconds);
                Cv2.PutText(result, "FPS=" + fps, new OpenCvSharp.Point(30, 30), HersheyFonts.HersheyComplex, 1.0, new Scalar(255, 0, 0), 3);
                //显示结果
                Cv2.ImShow("Result", result);
                for (int i = 0; i < ret.Count; i++)
                {
                    // 打印左上角和右下角坐标  
                    Point topLeft = new Point(ret[i].box.X, ret[i].box.Y);
                    Point bottomRight = new Point(ret[i].box.X + ret[i].box.Width - 1, ret[i].box.Y + ret[i].box.Height - 1);
                    //Console.WriteLine($"{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}");
                    rtboxMsg.AppendText($"{ret[i].label}:{ret[i].confidence:F4},{" ，像素坐标："}{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}" + "\n");
                }
                rtboxMsg.AppendText($"推理耗时:{sw.ElapsedMilliseconds}ms\n");
                rtboxMsg.ScrollToCaret();
                sw.Reset();
                int key = Cv2.WaitKey(10);
                if (key == 27)
                {
                    Cv2.DestroyWindow("Result");
                    break;
                }
            }

            capture.Release();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            DetParam param = new DetParam
            {
                modelWidth = 320,
                modelHeight = 320,
                confidenceThreshold = 0.5,
                outResultBmp = true
            };

            VideoCapture capture = new VideoCapture(1);
            if (!capture.IsOpened())
            {
                Console.WriteLine("video not open!");
                return;
            }
            Mat frame = new Mat();
            var sw = new Stopwatch();
            int fps = 0;
            while (true)
            {

                capture.Read(frame);
                if (frame.Empty())
                {
                    Console.WriteLine("data is empty!");
                    break;
                }
                sw.Start();
                List<DetResult> ret = det.DetectVedio(frame, param, out Mat result);
                sw.Stop();
                if (result == null)
                {
                    return;
                }
                fps = Convert.ToInt32(1 / sw.Elapsed.TotalSeconds);
                Cv2.PutText(result, "FPS=" + fps, new OpenCvSharp.Point(30, 30), HersheyFonts.HersheyComplex, 1.0, new Scalar(255, 0, 0), 3);
                //显示结果
                Cv2.ImShow("Result", result);
                for (int i = 0; i < ret.Count; i++)
                {
                    // 打印左上角和右下角坐标  
                    Point topLeft = new Point(ret[i].box.X, ret[i].box.Y);
                    Point bottomRight = new Point(ret[i].box.X + ret[i].box.Width - 1, ret[i].box.Y + ret[i].box.Height - 1);
                    //Console.WriteLine($"{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}");
                    rtboxMsg.AppendText($"{ret[i].label}:{ret[i].confidence:F4},{" ，像素坐标："}{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}" + "\n");
                }
                rtboxMsg.AppendText($"推理耗时:{sw.ElapsedMilliseconds}ms\n");
                rtboxMsg.ScrollToCaret();
                sw.Reset();
                int key = Cv2.WaitKey(10);
                if (key == 27)
                {
                    Cv2.DestroyWindow("Result");
                    break;
                }
            }

            capture.Release();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DetParam param = new DetParam
            {
                modelWidth = 320,
                modelHeight = 320,
                confidenceThreshold = 0.5,
                outResultBmp = true
            };
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<DetResult> ret = det.Detect(bmp, param, out Bitmap resultBmp);
            if (resultBmp == null)
            {
                return;
            }
            sw.Stop();
            for (int i = 0; i < ret.Count; i++)
            {
                // 打印左上角和右下角坐标  
                Point topLeft = new Point(ret[i].box.X, ret[i].box.Y);
                Point bottomRight = new Point(ret[i].box.X + ret[i].box.Width - 1, ret[i].box.Y + ret[i].box.Height - 1);
                //Console.WriteLine($"{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}");
                rtboxMsg.AppendText($"{ret[i].label}:{ret[i].confidence:F4},{" ，像素坐标："}{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}" + "\n");
            }
            rtboxMsg.AppendText($"推理耗时:{sw.ElapsedMilliseconds}ms\n");
            rtboxMsg.ScrollToCaret();
            int show = 0;
            foreach (Control control in tableLayoutPanel1.Controls)
            {
                if (control is PictureBox pictureBox)
                {
                    show++;
                    if (show == 2)
                    {
                        pictureBox.Image = resultBmp;
                        break;
                    }
                }
            }

            //picBoxRes.Image = resultBmp;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                // 条码读取器设置
                var reader = new BarcodeReader();
                var results = reader.DecodeMultiple(bmp);

                if (results != null && results.Length > 0)
                {
                    // 绘制矩形框
                    //picBoxRes.Image = DrawRectangles(bmp, results);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }

        }

        private Bitmap DrawRectangles(Bitmap image, Result[] results)
        {
            using (Graphics graphics = Graphics.FromImage(image))
            {
                foreach (var result in results)
                {
                    var points = result.ResultPoints;
                    rtboxMsg.AppendText("points.Length：" + points.Length + Environment.NewLine);
                    for (int i = 0; i < points.Length; i++)
                    {
                        rtboxMsg.AppendText("points[" + i + "]：" + points[i].X + "," + points[i].Y + Environment.NewLine);
                        PointF center = new PointF(points[i].X, points[i].Y);
                        RectangleF circleRect = new RectangleF(center.X, center.Y, Math.Abs(points[1].X - points[0].X) / 10, Math.Abs(points[1].X - points[0].X) / 10);

                        graphics.FillEllipse(Brushes.Green, circleRect);

                        // 标注坐标文字
                        string pointText = $"({points[i].X}, {points[i].Y})";
                        Font font = new Font(SystemFonts.DefaultFont.FontFamily, 50);
                        graphics.DrawString(pointText, font, Brushes.Red, center.X + 10, center.Y - 10);

                    }
                    // 显示条码文本
                    rtboxMsg.AppendText("条码类型：" + result.BarcodeFormat + Environment.NewLine);
                    rtboxMsg.AppendText("条码内容：" + result.Text + Environment.NewLine);
                    if (points.Length >= 4)
                    {

                        // 获取矩形的边界
                        float minX = Math.Min(points[0].X, points[3].X);
                        float minY = Math.Min(points[0].Y, points[1].Y);
                        float maxX = Math.Max(points[1].X, points[2].X);
                        float maxY = Math.Max(points[2].Y, points[3].Y);

                        // 绘制矩形框
                        RectangleF rect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                        graphics.DrawRectangle(Pens.Red, rect.X, rect.Y, rect.Width, rect.Height);
                    }
                    else if (points.Length == 2) // 有时可能只返回两个点
                    {
                        // 获取两个点的最小和最大坐标
                        float minX = Math.Min(points[0].X, points[1].X);
                        float minY = Math.Min(points[0].Y, points[1].Y);
                        float maxX = Math.Max(points[0].X, points[1].X);
                        float maxY = Math.Max(points[0].Y, points[1].Y);

                        // 绘制矩形框
                        RectangleF rect = new RectangleF(minX, minY, maxX - minX, maxY - minY);
                        graphics.DrawRectangle(Pens.Red, rect.X, rect.Y, rect.Width, rect.Height);
                    }
                }
            }

            return image;
        }


        private void AddPictureBoxesToTableLayoutPanel(int pictureBoxCount, TableLayoutPanel tableLayoutPanel)
        {
            // 清除现有的控件  
            tableLayoutPanel.Controls.Clear();
            tableLayoutPanel.RowCount = 0;
            tableLayoutPanel.ColumnCount = 0;

            // 设置TableLayoutPanel的行和列样式，以确保均匀分布空间  
            tableLayoutPanel.RowStyles.Clear();
            tableLayoutPanel.ColumnStyles.Clear();

            // 根据PictureBox的数量设置行和列  
            if (pictureBoxCount <= 3)
            {
                tableLayoutPanel.ColumnCount = pictureBoxCount;
                tableLayoutPanel.RowCount = 1;
                for (int i = 0; i < pictureBoxCount; i++)
                {
                    tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / pictureBoxCount));
                }
            }
            else
            {
                int columns = 3; // 每行最多3个PictureBox  
                int rows = (int)Math.Ceiling((double)pictureBoxCount / columns);
                tableLayoutPanel.ColumnCount = columns;
                tableLayoutPanel.RowCount = rows;

                for (int i = 0; i < columns; i++)
                {
                    tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
                }

                for (int i = 0; i < rows; i++)
                {
                    tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
                }
            }

            // 动态添加PictureBox到TableLayoutPanel中  
            for (int i = 0; i < pictureBoxCount; i++)
            {
                PictureBox pictureBox = new PictureBox
                {
                    Dock = DockStyle.Fill, // 使PictureBox填充其单元格  
                    SizeMode = PictureBoxSizeMode.Zoom, // 根据需要调整图片大小以适应PictureBox的大小  
                    BorderStyle = BorderStyle.Fixed3D // 可选，为了可视化边界  
                                                      // 设置其他PictureBox属性...  
                };

                int row = i / tableLayoutPanel.ColumnCount; // 确定PictureBox所在的行  
                int col = i % tableLayoutPanel.ColumnCount; // 确定PictureBox所在的列  
                tableLayoutPanel.Controls.Add(pictureBox, col, row); // 添加PictureBox到指定位置  
            }
        }

        private Bitmap PicDectect(Bitmap bmpre)
        {
            DetParam param = new DetParam
            {
                modelWidth = 320,
                modelHeight = 320,
                confidenceThreshold = 0.5,
                outResultBmp = true
            };
            Stopwatch sw = new Stopwatch();
            sw.Start();
            List<DetResult> ret = det.Detect(bmpre, param, out Bitmap resultBmp);
            if (resultBmp == null)
            {
                return null;
            }
            sw.Stop();
            for (int i = 0; i < ret.Count; i++)
            {
                // 打印左上角和右下角坐标  
                Point topLeft = new Point(ret[i].box.X, ret[i].box.Y);
                Point bottomRight = new Point(ret[i].box.X + ret[i].box.Width - 1, ret[i].box.Y + ret[i].box.Height - 1);
                //Console.WriteLine($"{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}");
                rtboxMsg.AppendText($"{ret[i].label}:{ret[i].confidence:F4},{" ，像素坐标："}{topLeft.X},{topLeft.Y},{bottomRight.X},{bottomRight.Y}" + "\n");
            }
            rtboxMsg.AppendText($"推理耗时:{sw.ElapsedMilliseconds}ms\n");
            rtboxMsg.ScrollToCaret();
            return resultBmp;
            //picBoxRes.Image = resultBmp;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            AddPictureBoxesToTableLayoutPanel(int.Parse(comboBox1.Text), tableLayoutPanel1);
            int pictureIndex = 0;
            foreach (Control control in tableLayoutPanel1.Controls)
            {
                if (control is PictureBox pictureBox)
                {
                    //if (pictureIndex < images.Length)
                    //{
                    //   pictureBox.Image = images[pictureIndex];
                    //   pictureIndex++;
                    // }
                    //else
                    //{
                    //    // 如果没有足够的图片，可以选择不显示图片或者显示默认图片  
                    //    pictureBox.Image = null; // 或者设置为默认图片  
                    //}

                    VideoCapture capture = new VideoCapture(pictureIndex);
                    if (!capture.IsOpened())
                    {
                        Console.WriteLine("video not open!");
                        pictureBox.Image = null;
                    }
                    else
                    {
                        Mat frame = new Mat(); // 创建一个Mat对象来存储捕获的图像  
                        capture.Read(frame); // 从视频流中捕获一帧  

                        if (!frame.Empty())
                        {
                            //string filename = "captured_image.bmp"; // 指定保存的文件名  
                            // 指定保存的文件路径和名称  
                            //string filePath = "photo.jpg"; // 你可以根据需要更改文件路径和格式  
                            // 将捕获的图像保存到文件  
                            //Cv2.ImWrite(filePath, frame);
                            bmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(frame);
                            bmp = PicDectect(bmp);
                            pictureBox.Image = bmp;

                        }
                        else
                        {
                            Console.WriteLine("Failed to capture frame!");
                        }
                        capture.Dispose(); // 释放VideoCapture对象资源  
                    }
                    pictureIndex ++;
                }
            }
        }

       
    }
}
