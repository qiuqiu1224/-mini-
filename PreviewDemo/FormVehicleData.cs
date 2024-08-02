using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sunny.UI;

namespace PreviewDemo
{
    public partial class FormVehicleData : UIPage
    {

        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const Int32 TEMP_WIDTH = 640;//红外图像宽度
        const Int32 TEMP_HEIGHT = 512;//红外图像宽度

        List<string>[] directoryFileNames = new List<string>[2];
        List<string>[] OpImageFileNames = new List<string>[2];
        //List<Mat>[] OP_Frames = new List<Mat>[2];
        List<string>[] irImageListPath = new List<string>[2];
        //private List<float[,]> realTemps = new List<float[,]>();//存储温度数据

        private PictureBox[] pics;//显示图像控件
        int deviceCount;

        string[] subdirectoryEntries_0;
        string[] subdirectoryEntries_1;

        string[] opImageDirectoryEntries_0;

        int currentIrImageIndex = 0;
        int currentOpImageIndex = 0;

        string videoFilePath;
        ListViewItem item;
        //int crossLine = 15;

        VideoCapture videoCapture;
        VideoInfo videoInfo;

        int selectType = -1;
        Globals.IRC_NET_POINT mouseDownPoint = new Globals.IRC_NET_POINT();

        TrainListInfo trainListInfo;
        private bool isInPic;//判断鼠标是否在图像内的标志
        float[,] realTemps = new float[640, 512];
        Image image;
        private List<Globals.IRC_NET_POINT> points = new List<Globals.IRC_NET_POINT>();
        List<TempRuleInfo> tempRuleInfos = new List<TempRuleInfo>();
        public int iNowPaint_X_Start = 0;
        public int iNowPaint_Y_Start = 0;
        public int iNowPaint_X_End = 0;
        public int iNowPaint_Y_End = 0;
        public int iTempType = 2;
        bool idraw = false;
        string directoryName, deviceName;

        public FormVehicleData()
        {
            InitializeComponent();
            initData();

            uiToolTip1.SetToolTip(mouseFollowBtn, "鼠标跟随");
            uiToolTip1.SetToolTip(drawRectBtn, "矩形测温");
            uiToolTip1.SetToolTip(drawCircleBtn, "圆形测温");
            uiToolTip1.SetToolTip(deleteAllDrawBtn, "删除所有选区");
            //SetFmonitorDisplayWnds(1,2);
        }

        private void initData()
        {

            deviceCount = Globals.systemParam.deviceCount; //通过配置文件获取设备数量

            pics = new PictureBox[deviceCount * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

            for (int i = 0; i < deviceCount; i++)
            {
                directoryFileNames[i] = new List<string>();
                OpImageFileNames[i] = new List<string>();
                irImageListPath[i] = new List<string>();
            }
        }



        private void FormVehicleData_Load(object sender, EventArgs e)
        {

            this.listView_VehicleData.View = View.Details;
            if (listView_VehicleData.Columns.Count == 0)
            {
                ColumnHeader columnHeader = new ColumnHeader();
                columnHeader.Text = "序号";
                columnHeader.Width = listView_VehicleData.Width * 3 / 10;
                columnHeader.TextAlign = HorizontalAlignment.Center;
                listView_VehicleData.Columns.Add(columnHeader);

                ColumnHeader columnHeader1 = new ColumnHeader();
                columnHeader1.Text = "探测时间";
                columnHeader1.Width = listView_VehicleData.Width * 7 / 10;
                columnHeader1.TextAlign = HorizontalAlignment.Center;
                listView_VehicleData.Columns.Add(columnHeader1);
                listView_VehicleData.FullRowSelect = true;
            }

            SetFmonitorDisplayWnds(1, 2);
            uiPanel3.SendToBack();


            label1.Left = listView_VehicleData.Width + panel1.Width / 8;
            label1.Visible = false;

            ir_Image_preview_btn.Left = label1.Right + 25;
            ir_Image_preview_btn.Visible = false;
            ir_image_next_btn.Left = ir_Image_preview_btn.Right + 25;
            ir_image_next_btn.Visible = false;

            uiLabel5.Left = listView_VehicleData.Width + panel1.Width * 3 / 7;

            label2.Left = listView_VehicleData.Width + panel1.Width * 5 / 8;
            label2.Visible = false;
            op_image_preview_btn.Left = label2.Right + 25;
            op_image_preview_btn.Visible = false;
            op_image_next_btn.Left = op_image_preview_btn.Right + 25;
            op_image_next_btn.Visible = false;

            line_Btn.Left = tipLable.Right + 20;
            mouseFollowBtn.Left = line_Btn.Right + 2;
            drawRectBtn.Left = mouseFollowBtn.Right + 20;
            drawCircleBtn.Left = drawRectBtn.Right + 20;
            deleteAllDrawBtn.Left = drawCircleBtn.Right + 20;

            uiDatePickerStart.Value = DateTime.Now;
            uiDatePickerEnd.Value = DateTime.Now;

            uiComboBox_DeviceName.Items.Add(Globals.systemParam.deviceName_0);
            if (Globals.systemParam.deviceCount > 1)
            {
                uiComboBox_DeviceName.Items.Add(Globals.systemParam.deviceName_1);
            }

            uiComboBox_DeviceName.SelectedIndex = 0;

            uiComboBox_dataType.Items.Add("过车数据");
            uiComboBox_dataType.Items.Add("报警数据");
            uiComboBox_dataType.SelectedIndex = 0;

        }



        public Bitmap ConvertJpgToBitmap(string imagePath)
        {
            // 加载JPG图片
            using (Image image = Image.FromFile(imagePath))
            {
                // 创建Bitmap的副本
                Bitmap bitmap = new Bitmap(image);
                return bitmap;
            }
        }

        public void DrawMaxTempAndCross(Bitmap irBitmap, string maxTempString, int x, int y, PictureBox pic, Font font, Brush brush)
        {
            // 创建Graphics对象
            using (Graphics graphics = Graphics.FromImage(irBitmap))
            {

                PointF point;
                //获取最大值字符串在屏幕上显示的尺寸
                SizeF maxTempStringSize = graphics.MeasureString(maxTempString, font);

                //超出边界，调整显示位置
                if (x + maxTempStringSize.Width > irBitmap.Width)
                {
                    x = x - (int)maxTempStringSize.Width;
                }

                if (y + maxTempStringSize.Height > irBitmap.Height)
                {
                    y = y - (int)maxTempStringSize.Height;
                }

                point = new PointF(x, y);

                //图像上显示全局温度最大值
                graphics.DrawString(maxTempString, font, brush, point);


                // 设置线条属性，例如粗细、颜色等
                Pen pen = new Pen(Color.Red, 1);

                Globals.DrawCross(graphics, x, y, 15, pen);
                // 绘制线条，指定起点和终点
                //System.Drawing.Point startPoint = new System.Drawing.Point(mouseDownPoint.x, mouseDownPoint.y);
                //System.Drawing.Point endPoint = new System.Drawing.Point(bitmap.Width, bitmap.Height);
                //graphics.DrawLine(pen, startPoint, endPoint);
                pic.Image = irBitmap;
                // 清理资源
                pen.Dispose();

            }

        }

        private void ListView_VehicleData_MouseClick(object sender, MouseEventArgs e)
        {
            pics[0].Image = null;
            pics[1].Image = null;
            string[] irJpegFiles;
            string[] opJpegFiles;
            if (e.Button == MouseButtons.Left)
            {
                currentIrImageIndex = 0;
                irImageListPath[0].Clear();
                ClearAllTempRules();

                ListViewHitTestInfo info = listView_VehicleData.HitTest(e.X, e.Y);
                item = info.Item;

                if (item != null)
                {
                    irJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index], "*.jpeg");//读取所有红外图像文件

                    foreach (string irFile in irJpegFiles)
                    {
                        irImageListPath[0].Add(irFile);
                    }

                    //温度文件路径
                    string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                        + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";



                    string[] splitPath = directoryFileNames[0][item.Index].Split("\\");
                    splitPath[2] = "TrainInfoReport";
                    string trainInfoXmlPath = null;

                    for (int m = 0; m < splitPath.Length; m++)
                    {
                        if (m != splitPath.Length - 1)
                        {
                            trainInfoXmlPath += splitPath[m] + "\\";
                        }
                        else
                        {
                            trainInfoXmlPath += splitPath[m] + ".xml";
                        }
                    }

                    trainListInfo = new TrainListInfo();
                    Globals.ReadInfoXml<TrainListInfo>(ref trainListInfo, trainInfoXmlPath);


                    int carLocation = int.Parse(Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(23));
                    uiLabel5.Text = "第 " + carLocation + " 辆车";
                    uiLabel5.Visible = true;

                    //获取温度数组
                    float[] tempDatas = Globals.GetTempFileToArray(tempFilePath);
                    realTemps = Globals.ChangeTempToArray(tempDatas, 640, 512);



                    List<float> tempDataList = tempDatas.ToList();
                    float maxTemp = tempDataList.Max();//获取最高温度值
                    int index = tempDataList.IndexOf(maxTemp);//最高温度值位置
                    int maxTempY = index / TEMP_WIDTH;
                    int maxTempX = index % TEMP_WIDTH;

                    Font font = new Font("Arial", 16);
                    Brush brush = Brushes.LightGreen;
                    string maxTempString = maxTemp.ToString("F1");//保留一位小数
                    Bitmap irBitmap = ConvertJpgToBitmap(irImageListPath[0][currentIrImageIndex]);

                    DrawMaxTempAndCross(irBitmap, maxTempString, maxTempX, maxTempY, pics[0], font, brush);
                    // 创建Graphics对象
                    //using (Graphics graphics = Graphics.FromImage(irBitmap))
                    //{

                    //    PointF point;
                    //    //获取最大值字符串在屏幕上显示的尺寸
                    //    SizeF maxTempStringSize = graphics.MeasureString(maxTempString, font);

                    //    //超出边界，调整显示位置
                    //    if (maxTempX + maxTempStringSize.Width > irBitmap.Width)
                    //    {
                    //        maxTempX = maxTempX - (int)maxTempStringSize.Width;
                    //    }

                    //    if (maxTempY + maxTempStringSize.Height > irBitmap.Height)
                    //    {
                    //        maxTempY = maxTempY - (int)maxTempStringSize.Height;
                    //    }

                    //    point = new PointF(maxTempX, maxTempY);

                    //    //图像上显示全局温度最大值
                    //    graphics.DrawString(maxTempString, font, brush, point);


                    //    // 设置线条属性，例如粗细、颜色等
                    //    Pen pen = new Pen(Color.Red, 1);

                    //    Globals.DrawCross(graphics, maxTempX, maxTempY, 15, pen);
                    //    // 绘制线条，指定起点和终点
                    //    //System.Drawing.Point startPoint = new System.Drawing.Point(mouseDownPoint.x, mouseDownPoint.y);
                    //    //System.Drawing.Point endPoint = new System.Drawing.Point(bitmap.Width, bitmap.Height);
                    //    //graphics.DrawLine(pen, startPoint, endPoint);
                    //    pics[0].Image = irBitmap;
                    //    // 清理资源
                    //    pen.Dispose();

                    //}


                    // Mat img = Cv2.ImRead(irImageListPath[0][currentIrImageIndex]);
                    //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);

                    //OpenCvSharp.Point cor;
                    //cor.X = maxTempX;
                    //cor.Y = maxTempY;

                    ////cor.X = 100;
                    ////cor.Y = 100;

                    ////在图像上绘制最高温度值及十字光标
                    //Globals.DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);
                    //Globals.DrawText(img, maxTemp.ToString("F1"), cor);


                    pics[0].Image = irBitmap;
                    image = pics[0].Image;

                    label1.Text = "红外图像 共" + irImageListPath[0].Count + "张 第" + (currentIrImageIndex + 1) + "张";
                    label1.Visible = true;
                    ir_Image_preview_btn.Visible = true;
                    ir_image_next_btn.Visible = true;
                    // 点击事件处理代码
                    // 比如显示点击的项的文本
                    //MessageBox.Show("你点击了: " + item.Index);


                    //var capture = new VideoCapture(Path.GetFullPath(directoryFileNames[0][item.Index]) + "\\" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4");

                    //// 检查视频是否成功打开
                    //if (capture.IsOpened())
                    //{

                    //    // 获取视频的帧数
                    //    Console.WriteLine(" 帧数" + capture.FrameCount);
                    //    Console.WriteLine(" 帧频" + capture.Fps);
                    //    Console.WriteLine("时长" + capture.FrameCount / capture.Fps);

                    //}
                    //else
                    //{
                    //    Console.WriteLine("Unable to open the video file.");
                    //    return;
                    //}

                    //FileInfo vedioFileInfo = new FileInfo(Application.StartupPath + "\\" + "20240715_094710_390.mp4");
                    //string vedioFileName = vedioFileInfo.Name;
                    //string vedioFileNameWithoutExtension = Path.GetFileNameWithoutExtension(Path.GetFullPath(directoryFileNames[0][item.Index]) + "\\" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4");
                    //string vedioHour = vedioFileNameWithoutExtension.Substring(9, 2);
                    //string vedioMin = vedioFileNameWithoutExtension.Substring(11, 2);
                    //string vedioSec = vedioFileNameWithoutExtension.Substring(13, 2);
                    //string vedioMillsec = vedioFileNameWithoutExtension.Substring(16, 3);

                    //string IRImageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(irJpegFiles[0]);
                    //string IRImageHour = IRImageFileNameWithoutExtension.Substring(9, 2);
                    //string IRImageMin = IRImageFileNameWithoutExtension.Substring(11, 2);
                    //string IRImageSec = IRImageFileNameWithoutExtension.Substring(13, 2);
                    //string IRImageMillsec = IRImageFileNameWithoutExtension.Substring(16, 3);


                    //int timeDiff = Convert.ToUInt16(IRImageHour) * 60 * 60 * 1000 + Convert.ToUInt16(IRImageMin) * 60 * 1000 + Convert.ToUInt16(IRImageSec) * 1000 + Convert.ToUInt16(IRImageMillsec)
                    //    - Convert.ToUInt16(vedioHour) * 60 * 60 * 1000 - Convert.ToUInt16(vedioMin) * 60 * 1000 - Convert.ToUInt16(vedioSec) * 1000 - Convert.ToUInt16(vedioMillsec);

                    //int frameIndex = (int)(timeDiff * capture.Fps / 1000);

                    //int startFrameIndex = 0;
                    //int endFrameIndex = 0;

                    //if (frameIndex - 5 > 0)
                    //{
                    //    startFrameIndex = frameIndex - 5;
                    //}
                    //else
                    //{
                    //    startFrameIndex = frameIndex;
                    //}


                    //if (frameIndex + 4 < capture.FrameCount)
                    //{
                    //    endFrameIndex = frameIndex + 4;
                    //}
                    //else
                    //{
                    //    endFrameIndex = frameIndex;
                    //}

                    //for (int i = startFrameIndex; i <= endFrameIndex; i++)
                    //{
                    //    capture.Set(CaptureProperty.PosFrames, i);
                    //    using (Mat frame = new Mat())
                    //    {
                    //        capture.Read(frame);
                    //        if (!frame.Empty())
                    //        {

                    //            OP_Frames[0].Add(frame.Clone());
                    //            // 这里可以对帧frame进行处理
                    //            // 例如保存帧到文件
                    //            string outputPath = $"frame_{i}.png";
                    //            Cv2.ImWrite(outputPath, frame);

                    //        }
                    //    }
                    //}



                    string irImageFilePath = Path.GetFileNameWithoutExtension(irJpegFiles[currentIrImageIndex]);

                    opJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index] + "\\" + "OP_Image");//读取所有可见光图像文件
                    OpImageFileNames[0].Clear();
                    OpImageFileNames[0] = Globals.GetOPImages(irImageFilePath, opJpegFiles);

                    currentOpImageIndex = 0;
                    Mat img = Cv2.ImRead(OpImageFileNames[0][currentOpImageIndex]);
                    pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
                    label2.Text = "可见光图像 共" + OpImageFileNames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
                    label2.Visible = true;
                    op_image_preview_btn.Visible = true;
                    op_image_next_btn.Visible = true;


                    //videoFilePath = Path.GetFullPath(directoryFileNames[0][item.Index]) + "\\" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4";
                    //videoCapture = new VideoCapture(videoFilePath);

                    //string vedioFileNameWithoutExtension = Path.GetFileNameWithoutExtension(Path.GetFullPath(videoFilePath));
                    //string vedioHour = vedioFileNameWithoutExtension.Substring(9, 2);
                    //string vedioMin = vedioFileNameWithoutExtension.Substring(11, 2);
                    //string vedioSec = vedioFileNameWithoutExtension.Substring(13, 2);
                    //string vedioMillsec = vedioFileNameWithoutExtension.Substring(16, 3);

                    //videoInfo.hour = Convert.ToUInt16(vedioHour);
                    //videoInfo.min = Convert.ToUInt16(vedioMin);
                    //videoInfo.sec = Convert.ToUInt16(vedioSec);
                    //videoInfo.millsec = Convert.ToUInt16(vedioMillsec);

                    //GetVideoFrames(irImageFilePath, OP_Frames[0]);
                    //if (OP_Frames[0].Count > 0)
                    //{


                    //    if (OP_Frames[0].Count >= 10)
                    //    {
                    //        currentOpImageIndex = 9;
                    //    }
                    //    else
                    //    {
                    //        if (OP_Frames[0].Count >= 4)
                    //        {
                    //            currentOpImageIndex = OP_Frames[0].Count - 1;
                    //        }
                    //        else
                    //        {
                    //            currentOpImageIndex = 0;
                    //        }

                    //    }

                    //    label2.Text = "可见光图像 共" + OP_Frames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
                    //    label2.Visible = true;
                    //    op_image_preview_btn.Visible = true;
                    //    op_image_next_btn.Visible = true;

                    //    if (currentOpImageIndex <= OP_Frames[0].Count - 1)
                    //    {

                    //        pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);
                    //    }
                    //    else
                    //    {
                    //        Globals.Log("解析可见光视频" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4" + "失败，视频录制不全");
                    //    }
                    //}
                    //else
                    //{
                    //    Globals.Log("解析可见光视频" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4" + "失败，视频大小0KB");
                    //}
                }

            }
        }

        struct VideoInfo
        {
            public int hour;
            public int min;
            public int sec;
            public int millsec;
        }



        private void GetVideoFrames(string irImageName, List<Mat> matList)
        {
            matList.Clear();
            //currentIrImageIndex = 0;


            // 检查视频是否成功打开
            if (videoCapture.IsOpened())
            {

                //FileInfo vedioFileInfo = new FileInfo(Application.StartupPath + "\\" + "20240715_094710_390.mp4");
                //string vedioFileName = vedioFileInfo.Name;


                string IRImageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(irImageName);
                string IRImageHour = IRImageFileNameWithoutExtension.Substring(9, 2);
                string IRImageMin = IRImageFileNameWithoutExtension.Substring(11, 2);
                string IRImageSec = IRImageFileNameWithoutExtension.Substring(13, 2);
                string IRImageMillsec = IRImageFileNameWithoutExtension.Substring(16, 3);


                int timeDiff = Convert.ToUInt16(IRImageHour) * 60 * 60 * 1000 + Convert.ToUInt16(IRImageMin) * 60 * 1000 + Convert.ToUInt16(IRImageSec) * 1000 + Convert.ToUInt16(IRImageMillsec)
                    - videoInfo.hour * 60 * 60 * 1000 - videoInfo.min * 60 * 1000 - videoInfo.sec * 1000 - Convert.ToUInt16(videoInfo.millsec);


                int frameIndex = (int)(timeDiff * videoCapture.Fps / 1000);

                int startFrameIndex = 0;
                int endFrameIndex = 0;

                if (frameIndex - 5 > 0)
                {
                    startFrameIndex = frameIndex - 5;
                }
                else
                {
                    startFrameIndex = 0;
                }


                if (frameIndex + 4 < videoCapture.FrameCount)
                {
                    endFrameIndex = frameIndex + 4;
                }
                else
                {
                    endFrameIndex = videoCapture.FrameCount - 1;
                }

                for (int i = startFrameIndex; i <= endFrameIndex; i++)
                {
                    videoCapture.Set(CaptureProperty.PosFrames, i);
                    using (Mat frame = new Mat())
                    {
                        videoCapture.Read(frame);
                        if (!frame.Empty())
                        {

                            matList.Add(frame.Clone());
                            // 这里可以对帧frame进行处理
                            // 例如保存帧到文件
                            //string outputPath = $"frame_{i}.png";
                            //Cv2.ImWrite(outputPath, frame);

                        }
                    }
                }

            }
            else
            {
                Globals.Log("不能打开可见光视频文件");
                return;
            }


        }

        /// <summary>
        /// 设置过车界面图像显示控件布局
        /// </summary>
        /// <param name="rowNum">行数</param>
        /// <param name="colNum">列数</param>

        private void SetFmonitorDisplayWnds(uint rowNum, uint colNum)
        {

            uint w = (uint)(this.Width - listView_VehicleData.Width);

            uint h = (uint)(this.Height - uiPanel1.Height);


            //先计算显示窗口的位置和大小，依据为：在不超过主窗口大小的情况下尽可能大，同时严格保持4:3的比例显示
            uint real_width = w;
            uint real_height = h;

            uint display_width = (real_width - DISPLAYWND_MARGIN_LEFT * 2 - (colNum - 1) * DISPLAYWND_GAP) / colNum;//单个相机显示区域的宽度(还未考虑比例)
            uint display_height = (real_height - DISPLAYWND_MARGIN_TOP * 2 - (rowNum - 1) * DISPLAYWND_GAP) / rowNum;//单个相机显示区域的高度(还未考虑比例)

            if (display_width * 3 >= display_height * 4)//考虑比例
            {
                uint ret = display_height % 3;
                if (ret != 0)
                {
                    display_height -= ret;
                }
                display_width = display_height * 4 / 3;
            }
            else
            {
                uint ret = display_width % 4;
                if (ret != 0)
                {
                    display_width -= ret;
                }
                display_height = display_width * 3 / 4;
            }



            for (uint i = 0; i < rowNum; i++)
            {
                uint y = (uint)uiPanel1.Height + (real_height - rowNum * display_height - DISPLAYWND_GAP * (rowNum - 1)) / 2 + (display_height + DISPLAYWND_GAP) * i;
                for (uint j = 0; j < colNum; j++)
                {
                    //uint x = (uint)fmonitor.GetControl("uiNavMenu1").Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;
                    uint x = (uint)listView_VehicleData.Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;

                    pics[i * 2 + j] = new PictureBox();
                    pics[i * 2 + j].Left = (int)x;
                    pics[i * 2 + j].Top = (int)y;
                    pics[i * 2 + j].Width = (int)display_width;
                    pics[i * 2 + j].Height = (int)display_height;
                    pics[i * 2 + j].Show();
                    pics[i * 2 + j].BackColor = Color.FromArgb(45, 45, 53);
                    //pics[i * 2 + j].Image = Image.FromFile(@"D:\C#\IRAY_Test\IR_Tmp_Measurement\bin\Debug\AlarmImage\20230330105027_Visual.jpg");
                    //pics[i * 2 + j].Name = "pic" + (i * 2 + j).ToString();
                    pics[i * 2 + j].SizeMode = PictureBoxSizeMode.StretchImage;
                    //pics[i * 2 + j].BringToFront();


                    this.Controls.Add(pics[i * 2 + j]);

                    //labels[i * 2 + j] = new TransparentLabel();
                    //labels[i * 2 + j].Left = (int)x;
                    //labels[i * 2 + j].Top = (int)y;
                    ////labels[i * 2 + j].Text = "轴" + (i * 2 + j + 1).ToString();
                    //labels[i * 2 + j].ForeColor = Color.WhiteSmoke;
                    //labels[i * 2 + j].BackColor = Color.Transparent;
                    //// label.Show();

                    //fmonitor.Controls.Add(labels[i * 2 + j]);
                    ////pic[i * 2 + j].Controls.Add(label);
                    ////label.Parent = fmonitor;
                    //labels[i * 2 + j].BringToFront();

                    switch (i * 2 + j)
                    {
                        case 0:
                            //pics[i * 2 + j].Tag = 0;
                            pics[i * 2 + j].Paint += new PaintEventHandler(Pics0_Paint);
                            pics[i * 2 + j].MouseClick += new MouseEventHandler(Pics0_MouseClick);
                            //pics[i * 2 + j].MouseDown += new MouseEventHandler(Pics0_MouseDown);
                            pics[i * 2 + j].MouseMove += new MouseEventHandler(Pics0_MouseMove);
                            pics[i * 2 + j].MouseLeave += new EventHandler(Pics0_MouseLeave);
                            ////pics[i * 2 + j].MouseHover += new EventHandler(Pics0_MouseUp);
                            //pics[i * 2 + j].MouseUp += new MouseEventHandler(Pics0_MouseUp);
                            break;
                        case 1:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics1_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics1_Click);
                            break;
                        case 2:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics2_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics2_Click);
                            break;
                        case 3:
                            //pics[i * 2 + j].Tag = 0;
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics3_Paint);
                            //pics[i * 2 + j].Click += new EventHandler(Pics3_Click);
                            break;

                    }

                }

            }
        }

        private void Pics0_MouseLeave(object sender, EventArgs e)
        {
            isInPic = false;
        }

        private void Pics0_MouseMove(object sender, MouseEventArgs e)
        {
            switch (selectType)
            {
                case (int)Globals.DrawMode.DRAW_MOUSE:
                    isInPic = true;


                    mouseDownPoint.x = e.X * 640 / pics[0].Width;
                    mouseDownPoint.y = e.Y * 512 / pics[0].Height;

                    pics[0].Refresh();
                    break;
                case (int)Globals.DrawMode.DRAW_LINE:
                case (int)Globals.DrawMode.DRAW_AREA:
                case (int)Globals.DrawMode.DRAW_CIRCLE:
                    idraw = true;
                    if (iTempType == 3)
                    {
                        iNowPaint_X_End = e.X * 640 / pics[0].Width;
                        iNowPaint_Y_End = e.Y * 512 / pics[0].Height;

                        mouseDownPoint.x = e.X * 640 / pics[0].Width;
                        mouseDownPoint.y = e.Y * 512 / pics[0].Height;

                        if (points.Count == 1)
                        {

                            points.Add(mouseDownPoint);
                        }
                        else if (points.Count == 2)
                        {
                            points[1] = mouseDownPoint;
                        }
                    }
                    pics[0].Refresh();
                    break;
            }
        }

        private void Pics0_Paint(object sender, PaintEventArgs e)
        {
            if (selectType != -1)
            {

                if (image == null)
                {
                    // 如果PictureBox没有Image，可以创建一个空白Image或者提示错误
                    // image = new Bitmap(pictureBox.Width, pictureBox.Height);
                    //MessageBox.Show("PictureBox中没有图像。");
                    return;
                }


                //Console.WriteLine("realTemp" + realTemps[mouseDownPoint.x*640 / pics[0].Width , mouseDownPoint.y *512/ pics[0].Height * 512]);

                Bitmap bitmap = new Bitmap(image);
                Font font = new Font("Arial", 16);
                Brush brush = Brushes.LightGreen;



                // 创建Graphics对象
                using (Graphics graphics = Graphics.FromImage(bitmap))
                {
                    // 设置线条属性，例如粗细、颜色等
                    Pen pen = new Pen(Color.Red, 1);

                    if (selectType == (int)Globals.DrawMode.DRAW_MOUSE)
                    {
                        if (isInPic)
                        {
                            string maxTempString = realTemps[mouseDownPoint.x, mouseDownPoint.y].ToString("F1");//保留一位小数

                            Globals.DrawCross(graphics, mouseDownPoint.x, mouseDownPoint.y, 15, pen);

                            SizeF tempStringSize = graphics.MeasureString(maxTempString, font);

                            int locX = mouseDownPoint.x;
                            int locY = mouseDownPoint.y;

                            ////超出边界，调整显示位置
                            if (locX + tempStringSize.Width > bitmap.Width)
                            {
                                locX = locX - (int)tempStringSize.Width - 10;
                            }

                            if (locY + tempStringSize.Height > bitmap.Height)
                            {
                                locY = locY - (int)tempStringSize.Height - 10;
                            }

                            graphics.DrawString(maxTempString, font, brush, locX + 10, locY + 10);
                        }
                    }

                    if ((selectType == (int)Globals.DrawMode.DRAW_AREA) && iTempType == 3 && idraw == true)
                    {
                        //gfx.DrawPoint(Color.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, 4);
                        graphics.DrawRectangle(Pens.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, iNowPaint_X_End - iNowPaint_X_Start, iNowPaint_Y_End - iNowPaint_Y_Start);

                        //gfx.DrawEllipse(Pens.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, iNowPaint_X_End - iNowPaint_X_Start, iNowPaint_Y_End - iNowPaint_Y_Start);
                    }

                    if ((selectType == (int)Globals.DrawMode.DRAW_CIRCLE) && iTempType == 3 && idraw == true)
                    {
                        //gfx.DrawPoint(Color.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, 4);
                        //gfx.DrawPoint(Color.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, 4);

                        graphics.DrawEllipse(Pens.LightGreen, iNowPaint_X_Start, iNowPaint_Y_Start, iNowPaint_X_End - iNowPaint_X_Start, iNowPaint_Y_End - iNowPaint_Y_Start);
                    }

                    for (int k = 0; k < tempRuleInfos.Count; k++)
                    {

                        if (tempRuleInfos[k].type == (int)Globals.DrawMode.DRAW_AREA)
                        {
                            graphics.DrawRectangle(new Pen(Color.LightGreen, 2), tempRuleInfos[k].startPointX, tempRuleInfos[k].startPointY, tempRuleInfos[k].endPointX - tempRuleInfos[k].startPointX, tempRuleInfos[k].endPointY - tempRuleInfos[k].startPointY);

                        }
                        if (tempRuleInfos[k].type == (int)Globals.DrawMode.DRAW_CIRCLE)
                        {
                            graphics.DrawEllipse(new Pen(Color.LightGreen, 2), tempRuleInfos[k].startPointX, tempRuleInfos[k].startPointY, tempRuleInfos[k].endPointX - tempRuleInfos[k].startPointX, tempRuleInfos[k].endPointY - tempRuleInfos[k].startPointY);
                        }

                        Globals.DrawCrossLine(graphics, tempRuleInfos[k].maxTempLocX, tempRuleInfos[k].maxTempLocY, pen, 10);
                        string maxTemp = ((float)tempRuleInfos[k].maxTemp).ToString("F1");//全局最高温度，保留一位小数
                        PointF point = new PointF(tempRuleInfos[k].maxTempLocX, tempRuleInfos[k].maxTempLocY);
                        graphics.DrawString(maxTemp, font, brush, point);

                    }

                    pics[0].Image = bitmap;


                    // 清理资源
                    pen.Dispose();
                }
            }

        }

        private bool IsPointInEllipse(int x, int y, int ellipseCenterX, int ellipseCenterY, int ellipseRadiusX, int ellipseRadiusY)
        {
            // 使用椭圆的标准方程进行检查
            double xDiff = x - ellipseCenterX;
            double yDiff = y - ellipseCenterY;
            double xRadiusSquared = ellipseRadiusX * ellipseRadiusX;
            double yRadiusSquared = ellipseRadiusY * ellipseRadiusY;
            double ratio = xRadiusSquared / yRadiusSquared;

            return (xDiff * xDiff) / xRadiusSquared + (yDiff * yDiff) / (ratio * yRadiusSquared) <= 1;

        }
        public float[] FindMaxValueInEllipse(float[,] imageData, int ellipseCenterX, int ellipseCenterY, int ellipseRadiusX, int ellipseRadiusY)
        {
            float[] result = new float[3];
            result[0] = int.MinValue;
            //Console.WriteLine("ellipseCenterX:" + ellipseCenterX);
            //Console.WriteLine("ellipseCenterY:" + ellipseCenterY);
            //Console.WriteLine("ellipseRadiusX:" + ellipseRadiusX);
            //Console.WriteLine("ellipseRadiusY:" + ellipseRadiusY);

            for (int y = ellipseCenterY - ellipseRadiusY; y <= ellipseCenterY + ellipseRadiusY; y++)
            {
                for (int x = ellipseCenterX - ellipseRadiusX; x <= ellipseCenterX + ellipseRadiusX; x++)
                {

                    // 检查点是否在椭圆内
                    if (IsPointInEllipse(x, y, ellipseCenterX, ellipseCenterY, ellipseRadiusX, ellipseRadiusY))
                    {
                        //Console.WriteLine(" imageData.GetLength(0)" + imageData.GetLength(0));
                        //Console.WriteLine(" imageData.GetLength(1)" + imageData.GetLength(1));

                        // 确保坐标在图像数组范围内
                        if (x >= 0 && x < imageData.GetLength(0) && y >= 0 && y < imageData.GetLength(1))
                        {
                            float currentValue = imageData[x, y];
                            if (currentValue > result[0])
                            {
                                result[0] = currentValue;
                                result[1] = x;
                                result[2] = y;
                            }
                            // result[0] = Math.Max(result[0], currentValue);

                        }
                    }
                }
            }

            return result;
        }

        public float[] getTempAtRect(float[,] realTemp, int X1, int Y1, int X2, int Y2)
        {
            float[] result = new float[3];
            int startX = X1 < X2 ? X1 : X2;
            int startY = Y1 < Y2 ? Y1 : Y2;
            int endX = X1 < X2 ? X2 : X1;
            int endY = Y1 < Y2 ? Y2 : Y1;
            result[0] = realTemp[startX, startY];
            result[1] = startX;
            result[2] = startY;

            for (int j = startY; j < endY; ++j)
            {
                for (int i = startX; i < endX; ++i)
                {


                    if (realTemp[i, j] > result[0])
                    {
                        result[0] = realTemp[i, j];
                        result[1] = i;
                        result[2] = j;
                    }

                }
            }
            return result;
        }

        private void Pics0_MouseClick(object sender, MouseEventArgs e)
        {

            if (selectType != (int)Globals.DrawMode.DRAW_MOUSE)
            {
                int iX = e.X * 640 / pics[0].Width;
                int iY = e.Y * 512 / pics[0].Height;

                mouseDownPoint.x = iX;
                mouseDownPoint.y = iY;

                iNowPaint_X_End = 0;
                iNowPaint_Y_End = 0;

                if (e.Button == MouseButtons.Left)
                {
                    if (iTempType == 2)//画矩形或画圆形
                    {
                        idraw = false;
                        iNowPaint_X_Start = iX;
                        iNowPaint_Y_Start = iY;

                        points.Add(mouseDownPoint);

                        iTempType = 3;
                    }

                }
                else if (e.Button == MouseButtons.Right)
                {
                    iTempType = 2;
                    idraw = false;
                    switch (selectType)
                    {
                        case (int)Globals.DrawMode.DRAW_POINT:
                        case (int)Globals.DrawMode.DRAW_LINE:
                        case (int)Globals.DrawMode.DRAW_AREA:
                        case (int)Globals.DrawMode.DRAW_CIRCLE:

                            if (2 == points.Count)
                            {

                                TempRuleInfo tempRuleInfo = new TempRuleInfo();
                                tempRuleInfo.type = selectType;
                                tempRuleInfo.index = 0;
                                tempRuleInfo.startPointX = points[0].x;
                                tempRuleInfo.startPointY = points[0].y;
                                tempRuleInfo.endPointX = points[1].x;
                                tempRuleInfo.endPointY = points[1].y;
                                if (selectType == (int)Globals.DrawMode.DRAW_AREA)
                                {
                                    float[] results = getTempAtRect(realTemps, tempRuleInfo.startPointX, tempRuleInfo.startPointY, tempRuleInfo.endPointX, tempRuleInfo.endPointY);
                                    tempRuleInfo.maxTemp = results[0];
                                    tempRuleInfo.maxTempLocX = (int)results[1];
                                    tempRuleInfo.maxTempLocY = (int)results[2];
                                }

                                if (selectType == (int)Globals.DrawMode.DRAW_CIRCLE)
                                {
                                    int startX = tempRuleInfo.startPointX;
                                    int startY = tempRuleInfo.startPointY;
                                    int endX = tempRuleInfo.endPointX;
                                    int endY = tempRuleInfo.endPointY;
                                    int radiusX = (endX - startX) / 2;
                                    int radiusY = (endY - startY) / 2;
                                    float[] results = FindMaxValueInEllipse(realTemps, startX + radiusX, startY + radiusY, radiusX, radiusY);
                                    tempRuleInfo.maxTemp = results[0];
                                    tempRuleInfo.maxTempLocX = (int)results[1];
                                    tempRuleInfo.maxTempLocY = (int)results[2];

                                }

                                //tempRuleInfos[rectModeIndex] = tempRuleInfo;
                                tempRuleInfos.Add(tempRuleInfo);

                                points.Clear();



                            }
                            break;
                    }
                }


            }

        }

        public struct TempRuleInfo
        {
            public int type;
            public int index;
            public int startPointX;
            public int startPointY;
            public int endPointX;
            public int endPointY;
            public float maxTemp;
            public int maxTempLocX;
            public int maxTempLocY;
        }

        private void Ir_Image_preview_btn_Click(object sender, EventArgs e)
        {

            if ((currentIrImageIndex - 1) >= 0)
            {
                currentIrImageIndex -= 1;
                Mat img = Cv2.ImRead(irImageListPath[0][currentIrImageIndex]);
                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);

                int carLocation = int.Parse(Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(23));
                uiLabel5.Text = "第 " + carLocation + " 辆车";

                string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                  + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";
                float[] tempDatas = Globals.GetTempFileToArray(tempFilePath);
                realTemps = Globals.ChangeTempToArray(tempDatas, 640, 512);
                List<float> tempDataList = new List<float>();
                tempDataList = tempDatas.ToList();
                float maxTemp = tempDataList.Max();
                int index = tempDataList.IndexOf(maxTemp);
                int maxTempY = index / TEMP_WIDTH;
                int maxTempX = index % TEMP_WIDTH - 1;

                Font font = new Font("Arial", 16);
                Brush brush = Brushes.LightGreen;
                string maxTempString = maxTemp.ToString("F1");//保留一位小数
                Bitmap irBitmap = ConvertJpgToBitmap(irImageListPath[0][currentIrImageIndex]);

                DrawMaxTempAndCross(irBitmap, maxTempString, maxTempX, maxTempY, pics[0], font, brush);
                ClearAllTempRules();
                image = irBitmap;

                ////Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);

                //OpenCvSharp.Point cor;
                //cor.X = maxTempX;
                //cor.Y = maxTempY;

                ////cor.X = 100;
                ////cor.Y = 100;

                //Globals.DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);

                ////cor.X = 100;
                ////cor.Y = 100;

                //Globals.DrawText(img, maxTemp.ToString("F1"), cor);
                ////Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 20), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.Green, 1);
                //pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);

                label1.Text = "红外图像 共" + irImageListPath[0].Count + "张 第" + (currentIrImageIndex + 1) + "张";

                string irImageFilePath = Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]);

                string[] opJpegFiles;
                opJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index] + "\\" + "OP_Image");//读取所有红外图像文件
                OpImageFileNames[0].Clear();
                OpImageFileNames[0] = Globals.GetOPImages(irImageFilePath, opJpegFiles);

                currentOpImageIndex = 0;
                img = Cv2.ImRead(OpImageFileNames[0][currentOpImageIndex]);
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
                label2.Text = "可见光图像 共" + OpImageFileNames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";

                //GetVideoFrames(irImageListPath[0][currentIrImageIndex], OP_Frames[0]);
                //if (OP_Frames[0].Count > 0)
                //{


                //    if (OP_Frames[0].Count >= 10)
                //    {
                //        currentOpImageIndex = 9;
                //    }
                //    else
                //    {
                //        if (OP_Frames[0].Count >= 4)
                //        {
                //            currentOpImageIndex = OP_Frames[0].Count - 1;
                //        }
                //        else
                //        {
                //            currentOpImageIndex = 0;
                //        }

                //    }


                //    if (currentOpImageIndex <= OP_Frames[0].Count - 1)
                //    {

                //        pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);
                //        label2.Text = "可见光图像 共" + OP_Frames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
                //    }
                //    else
                //    {
                //        Globals.Log("解析可见光视频" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4" + "失败，视频录制不全");
                //    }
                //}

                //else
                //{
                //    Globals.Log("解析可见光视频" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4" + "失败，视频大小0KB");
                //}


            }
        }

        private void ClearAllTempRules()
        {
            selectType = (int)Globals.DrawMode.NO_DRAW;
            tempRuleInfos.Clear();
            pics[0].Refresh();

            Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            Globals.SetButtonImg(drawRectBtn, "square.png");
            Globals.SetButtonImg(drawCircleBtn, "circle.png");

        }

        private void Ir_image_next_btn_Click(object sender, EventArgs e)
        {

            if ((currentIrImageIndex + 1) < irImageListPath[0].Count)
            {
                currentIrImageIndex += 1;
                Mat img = Cv2.ImRead(irImageListPath[0][currentIrImageIndex]);
                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);


                string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                      + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";
                int carLocation = int.Parse(Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(23));
                uiLabel5.Text = "第 " + carLocation + " 辆车";

                float[] tempDatas = Globals.GetTempFileToArray(tempFilePath);
                realTemps = Globals.ChangeTempToArray(tempDatas, 640, 512);

                List<float> tempDataList = new List<float>();
                tempDataList = tempDatas.ToList();

                //float[] r = getTempAtRect(realTemps, 0, 0, 639, 511);

                float maxTemp = tempDataList.Max();
                int index = tempDataList.IndexOf(maxTemp);
                int maxTempY = index / TEMP_WIDTH;
                int maxTempX = index % TEMP_WIDTH;

                Font font = new Font("Arial", 16);
                Brush brush = Brushes.LightGreen;
                string maxTempString = maxTemp.ToString("F1");//保留一位小数
                Bitmap irBitmap = ConvertJpgToBitmap(irImageListPath[0][currentIrImageIndex]);

                DrawMaxTempAndCross(irBitmap, maxTempString, maxTempX, maxTempY, pics[0], font, brush);

                ClearAllTempRules();
                image = irBitmap;
              

                //OpenCvSharp.Point cor;
                //cor.X = maxTempX;
                //cor.Y = maxTempY;

                ////cor.X = 100;
                ////cor.Y = 100;

                //Globals.DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);
                ////Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);


                ////cor.X = 100;
                ////cor.Y = 100;

                //Globals.DrawText(img, maxTemp.ToString("F1"), cor);
                ////Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 20), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.Green, 1);

                //pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);


                label1.Text = "红外图像 共" + irImageListPath[0].Count + "张 第" + (currentIrImageIndex + 1) + "张";


                string irImageFilePath = Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]);

                string[] opJpegFiles;
                opJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index] + "\\" + "OP_Image");//读取所有红外图像文件
                OpImageFileNames[0].Clear();
                OpImageFileNames[0] = Globals.GetOPImages(irImageFilePath, opJpegFiles);

                currentOpImageIndex = 0;
                img = Cv2.ImRead(OpImageFileNames[0][currentOpImageIndex]);
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
                label2.Text = "可见光图像 共" + OpImageFileNames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";


                //GetVideoFrames(irImageListPath[0][currentIrImageIndex], OP_Frames[0]);
                //if (OP_Frames[0].Count > 0)
                //{


                //    if (OP_Frames[0].Count >= 10)
                //    {
                //        currentOpImageIndex = 9;
                //    }
                //    else
                //    {
                //        if (OP_Frames[0].Count >= 4)
                //        {
                //            currentOpImageIndex = OP_Frames[0].Count - 1;
                //        }
                //        else
                //        {
                //            currentOpImageIndex = 0;
                //        }

                //    }


                //    if (currentOpImageIndex <= OP_Frames[0].Count - 1)
                //    {

                //        pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);
                //        label2.Text = "可见光图像 共" + OP_Frames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
                //    }
                //    else
                //    {
                //        Globals.Log("解析可见光视频" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4" + "失败，视频录制不全");
                //    }
                //}
                //else
                //{
                //    Globals.Log("解析可见光视频" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4" + "失败，视频大小0KB");
                //}
            }

        }

        private void Op_image_preview_btn_Click(object sender, EventArgs e)
        {

            if ((currentOpImageIndex - 1) >= 0)
            {
                currentOpImageIndex -= 1;
                Mat img = Cv2.ImRead(OpImageFileNames[0][currentOpImageIndex]);
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
                label2.Text = "可见光图像 共" + OpImageFileNames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
            }
        }

        private void Op_image_next_btn_Click(object sender, EventArgs e)
        {

            if ((currentOpImageIndex + 1) < OpImageFileNames[0].Count)
            {
                currentOpImageIndex += 1;
                label2.Text = "可见光图像 共" + OpImageFileNames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
                Mat img = Cv2.ImRead(OpImageFileNames[0][currentOpImageIndex]);
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);

            }

        }

        private void uiButton_query_Click(object sender, EventArgs e)
        {
            directoryFileNames[0].Clear();
            pics[0].Image = null;
            pics[1].Image = null;

            listView_VehicleData.Items.Clear();
            //if (listView_VehicleData.Items.Count >= 1)
            //{
            //    for (int j = listView_VehicleData.Items.Count - 1; j >= 0; j--)
            //    {
            //        listView_VehicleData.Items.RemoveAt(j);
            //    }
            //}

            //string folderPath = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Convert.ToDateTime(uiDatePickerStart.Text).ToString("yyyy_MM_dd") + "\\" + Globals.systemParam.ip_0; // 

            if (uiComboBox_DeviceName.SelectedIndex == 0)
            {
                deviceName = Globals.systemParam.deviceName_0;
            }
            else
            {
                deviceName = Globals.systemParam.deviceName_1;
            }

            if (uiComboBox_dataType.SelectedIndex == 0)
            {
                directoryName = "SaveReport";
            }
            else
            {
                directoryName = "AlarmReport";
            }


            string folderPath = Globals.RootSavePath + "\\" + directoryName + "\\" + Globals.systemParam.stationName + "\\" + deviceName + "\\" + Convert.ToDateTime(uiDatePickerStart.Text).ToString("yyyy_MM_dd"); // 

            DirectoryInfo imageDirectoryInfo = new DirectoryInfo(folderPath);
            if (imageDirectoryInfo.Exists)
            {
                tipLable.Text = "";
                subdirectoryEntries_0 = Directory.GetDirectories(folderPath);//获取当日所有过车数据文件夹路径

                int count = 1;
                for (int i = 0; i < subdirectoryEntries_0.Length; i++)
                {
                    string fileName = Path.GetFileName(subdirectoryEntries_0[i]);
                    //int year = Convert.ToInt32(fileName.Substring(0, 4));//年
                    //int month = Convert.ToInt32(fileName.Substring(4, 2));//月
                    //int day = Convert.ToInt32(fileName.Substring(6, 2));//日
                    //int hour = Convert.ToInt32(fileName.Substring(9, 2));//时
                    //int min = Convert.ToInt32(fileName.Substring(11, 2));//分
                    //int sec = Convert.ToInt32(fileName.Substring(13, 2));//秒

                    directoryFileNames[0].Add(subdirectoryEntries_0[i]);
                    //if (deviceCount > 1)
                    //{
                    //    directoryFileNames[1].Add(subdirectoryEntries_1[i]);
                    //}
                    ListViewItem item = new ListViewItem();

                    item.SubItems[0].Text = count.ToString();
                    listView_VehicleData.Columns[1].TextAlign = HorizontalAlignment.Left;
                    listView_VehicleData.Columns[0].TextAlign = HorizontalAlignment.Right;

                    fileName = fileName.Substring(0, 4) + "-" + fileName.Substring(4, 2) + "-" + fileName.Substring(6, 2) + " " + fileName.Substring(9, 2) + ":" + fileName.Substring(11, 2);
                    item.SubItems.Add(fileName);
                    listView_VehicleData.Items.Add(item);

                    count++;


                    //DateTime checkingDate = new DateTime(year, month, day, hour, min, sec);

                    //DateTime startDate = new DateTime(uiDatePickerStart.Year, uiDatePickerStart.Month, uiDatePickerStart.Day, 0, 0, 0);
                    //DateTime endDate = new DateTime(uiDatePickerStart.Year, uiDatePickerStart.Month, uiDatePickerStart.Day, 23, 59, 59);

                    //if (checkingDate >= startDate && checkingDate <= endDate)
                    //{
                    //    directoryFileNames[0].Add(subdirectoryEntries_0[i]);
                    //    if (deviceCount > 1)
                    //    {
                    //        directoryFileNames[1].Add(subdirectoryEntries_1[i]);
                    //    }
                    //    ListViewItem item = new ListViewItem();

                    //    item.SubItems[0].Text = (count + 1).ToString();
                    //    listView_VehicleData.Columns[1].TextAlign = HorizontalAlignment.Left;
                    //    listView_VehicleData.Columns[0].TextAlign = HorizontalAlignment.Right;

                    //    fileName = fileName.Substring(0, 4) + "-" + fileName.Substring(4, 2) + "-" + fileName.Substring(6, 2) + " " + fileName.Substring(9, 2) + ":" + fileName.Substring(11, 2);
                    //    item.SubItems.Add(fileName);
                    //    listView_VehicleData.Items.Add(item);

                    //    count++;
                    //}

                    // Console.WriteLine("");

                }


            }
            else
            {
                if (uiComboBox_dataType.SelectedIndex == 0)
                {
                    tipLable.Text = "没有过车数据";
                }
                else
                {
                    tipLable.Text = "没有报警数据";
                }

            }
        }

        private void MouseFollowBtn_Click(object sender, EventArgs e)
        {
            if (selectType != (int)Globals.DrawMode.DRAW_MOUSE)
            {
                selectType = (int)Globals.DrawMode.DRAW_MOUSE;
                //mouseFollowFlag = true;
                Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随1.png");
                Globals.SetButtonImg(drawRectBtn, "square.png");
                Globals.SetButtonImg(drawCircleBtn, "circle.png");
            }
            else
            {
                selectType = (int)Globals.DrawMode.NO_DRAW;
                //mouseFollowFlag = false;
                Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
        }

        private void MouseFollowBtn_MouseHover(object sender, EventArgs e)
        {
            Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随1.png");
        }

        private void MouseFollowBtn_MouseLeave(object sender, EventArgs e)
        {
            //if (!mouseFollowFlag)
            if (selectType != (int)Globals.DrawMode.DRAW_MOUSE)
            {
                Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
        }

        private void FormVehicleData_Leave(object sender, EventArgs e)
        {
            selectType = (int)Globals.DrawMode.NO_DRAW;
            Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            pics[0].Refresh();

        }

        private void DrawRectBtn_Click(object sender, EventArgs e)
        {
            if (selectType != (int)Globals.DrawMode.DRAW_AREA)
            {
                selectType = (int)Globals.DrawMode.DRAW_AREA;
                Globals.SetButtonImg(drawRectBtn, "square_pressed.png");
                Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
                Globals.SetButtonImg(drawCircleBtn, "circle.png");
            }
            else
            {
                selectType = -1;
                Globals.SetButtonImg(drawRectBtn, "square.png");
            }
        }

        private void DrawRectBtn_MouseHover(object sender, EventArgs e)
        {
            Globals.SetButtonImg(drawRectBtn, "square_pressed.png");
        }

        private void DrawRectBtn_MouseLeave(object sender, EventArgs e)
        {
            if (selectType != (int)Globals.DrawMode.DRAW_AREA)
            {
                Globals.SetButtonImg(drawRectBtn, "square.png");
            }
        }

        private void DeleteAllDrawBtn_Click(object sender, EventArgs e)
        {
            Globals.SetButtonImg(deleteAllDrawBtn, "delete.png");
            //SetButtonImg(drawCircleBtn, "circle.png");
            //SetButtonImg(drawRectBtn, "square.png");
            //SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            tempRuleInfos.Clear();
        }

        private void DrawCircleBtn_Click(object sender, EventArgs e)
        {
            if (selectType != (int)Globals.DrawMode.DRAW_CIRCLE)
            {
                selectType = (int)Globals.DrawMode.DRAW_CIRCLE;
                Globals.SetButtonImg(drawCircleBtn, "circlePressed.png");
                Globals.SetButtonImg(drawRectBtn, "square.png");
                Globals.SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
            else
            {
                selectType = -1;
                Globals.SetButtonImg(drawCircleBtn, "circle.png");
            }
        }

        private void DrawCircleBtn_MouseHover(object sender, EventArgs e)
        {
            Globals.SetButtonImg(drawCircleBtn, "circlePressed.png");
        }

        private void UiComboBox_dataType_SelectedIndexChanged(object sender, EventArgs e)
        {


        }

        private void UiComboBox_DeviceName_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void DrawCircleBtn_MouseLeave(object sender, EventArgs e)
        {
            if (selectType != (int)Globals.DrawMode.DRAW_CIRCLE)
            {
                Globals.SetButtonImg(drawCircleBtn, "circle.png");
            }
        }
    }
}
