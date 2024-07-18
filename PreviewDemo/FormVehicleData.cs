using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
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
        List<Mat>[] OP_Frames = new List<Mat>[2];
        List<string>[] irImageListPath = new List<string>[2];
        private List<float[,]> realTemps = new List<float[,]>();//存储温度数据

        private PictureBox[] pics;//显示图像控件
        int deviceCount;

        string[] subdirectoryEntries_0;
        string[] subdirectoryEntries_1;

        int currentIrImageIndex = 0;
        int currentOpImageIndex = 0;

        string videoFilePath;
        ListViewItem item;
        //int crossLine = 15;

        VideoCapture videoCapture;
        VideoInfo videoInfo;


        public FormVehicleData()
        {
            InitializeComponent();
            initData();

            //SetFmonitorDisplayWnds(1,2);
        }

        private void initData()
        {

            deviceCount = Globals.systemParam.deviceCount; //通过配置文件获取设备数量

            pics = new PictureBox[deviceCount * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

            for (int i = 0; i < deviceCount; i++)
            {
                directoryFileNames[i] = new List<string>();
                OP_Frames[i] = new List<Mat>();
                irImageListPath[i] = new List<string>();
            }
        }

        private void UiButton1_Click(object sender, EventArgs e)
        {
            directoryFileNames[0].Clear();
            if (listView_VehicleData.Items.Count >= 1)
            {
                for (int j = listView_VehicleData.Items.Count - 1; j >= 0; j--)
                {
                    listView_VehicleData.Items.RemoveAt(j);
                }
            }

            string folderPath = Application.StartupPath + "\\" + Globals.systemParam.ip_0; // 替换为你的文件夹路径
            subdirectoryEntries_0 = Directory.GetDirectories(folderPath);
            if (deviceCount > 1)
            {
                subdirectoryEntries_1 = Directory.GetDirectories(Application.StartupPath + "\\" + Globals.systemParam.ip_1);
            }
            int count = 0;
            for (int i = 0; i < subdirectoryEntries_0.Length; i++)
            {
                string fileName = Path.GetFileName(subdirectoryEntries_0[i]);
                int year = Convert.ToInt32(fileName.Substring(0, 4));//年
                int month = Convert.ToInt32(fileName.Substring(4, 2));//月
                int day = Convert.ToInt32(fileName.Substring(6, 2));//日
                int hour = Convert.ToInt32(fileName.Substring(9, 2));//时
                int min = Convert.ToInt32(fileName.Substring(11, 2));//分
                int sec = Convert.ToInt32(fileName.Substring(13, 2));//秒

                DateTime checkingDate = new DateTime(year, month, day, hour, min, sec);

                DateTime startDate = new DateTime(uiDatePickerStart.Year, uiDatePickerStart.Month, uiDatePickerStart.Day, 0, 0, 0);
                DateTime endDate = new DateTime(uiDatePickerEnd.Year, uiDatePickerEnd.Month, uiDatePickerEnd.Day, 23, 59, 59);

                if (checkingDate >= startDate && checkingDate <= endDate)
                {
                    directoryFileNames[0].Add(subdirectoryEntries_0[i]);
                    if (deviceCount > 1)
                    {
                        directoryFileNames[1].Add(subdirectoryEntries_1[i]);
                    }
                    ListViewItem item = new ListViewItem();

                    item.SubItems[0].Text = (count + 1).ToString();
                    listView_VehicleData.Columns[1].TextAlign = HorizontalAlignment.Left;
                    listView_VehicleData.Columns[0].TextAlign = HorizontalAlignment.Right;

                    fileName = year + "-" + month + "-" + day + " " + hour + ":" + min;
                    item.SubItems.Add(fileName);
                    listView_VehicleData.Items.Add(item);

                    count++;
                }

                Console.WriteLine("");



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

            SetFmonitorDisplayWnds((uint)Globals.systemParam.deviceCount, 2);
            uiPanel3.SendToBack();

            label1.Left = listView_VehicleData.Width + panel1.Width / 8;
            ir_Image_preview_btn.Left = label1.Right + 25;
            ir_image_next_btn.Left = ir_Image_preview_btn.Right + 25;

            label2.Left = listView_VehicleData.Width + panel1.Width * 5 / 8;
            op_image_preview_btn.Left = label2.Right + 25;
            op_image_next_btn.Left = op_image_preview_btn.Right + 25;

            uiDatePickerStart.Value = DateTime.Now;
            uiDatePickerEnd.Value = DateTime.Now;
        }


        private float[] GetTempFileToArray(string tempFilePath)
        {
            float[] tempData = new float[TEMP_WIDTH * TEMP_HEIGHT];
            int k = 0;
            using (FileStream fs = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < fs.Length)
                    {
                        float buffer = br.ReadSingle(); // 读取16个字节（四个四节）

                        tempData[k] = buffer;
                        k++;
                        // 处理buffer中的数据
                        // ...
                    }

                    return tempData;
                }
            }



            //realTemps[0] = new float[TEMP_WIDTH, TEMP_HEIGHT];
            //for (int i = 0; i < TEMP_WIDTH; i++)
            //{
            //    for (int j = 0; j < TEMP_HEIGHT; j++)
            //    {
            //        // ShortToUnsignedInt
            //        //realTemp[i][j] = (0xff & tempData[j * infraredImageWidth + i]) | (0xff00 & (tempData[j * infraredImageWidth + i + infraredImageWidth * infraredImageHeight] << 8)) & 0xffff;

            //        realTemps[0][i, j] = tempData[j * TEMP_WIDTH + i];

            //    }
            //}

        }

        private void DrawText(Mat img,string maxTemp,OpenCvSharp.Point cor)
        {
            Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 25), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.LightGreen, 2);
        }

        private void ListView_VehicleData_MouseClick(object sender, MouseEventArgs e)
        {
            string[] irJpegFiles;
            if (e.Button == MouseButtons.Left)
            {
                currentIrImageIndex = 0;
                irImageListPath[0].Clear();

                ListViewHitTestInfo info = listView_VehicleData.HitTest(e.X, e.Y);
                item = info.Item;

                if (item != null)
                {
                    irJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index], "*.jpeg", SearchOption.AllDirectories);//读取所有红外图像文件

                    foreach (string irFile in irJpegFiles)
                    {
                        irImageListPath[0].Add(irFile);
                    }

                    string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                        + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";
                    float[] tempDatas = GetTempFileToArray(tempFilePath);
                    List<float> tempDataList = new List<float>();
                    tempDataList = tempDatas.ToList();
                    float maxTemp = tempDataList.Max();
                    int index = tempDataList.IndexOf(maxTemp);
                    int maxTempY = index / TEMP_WIDTH;
                    int maxTempX = index % TEMP_WIDTH;

                    Mat img = Cv2.ImRead(irImageListPath[0][currentIrImageIndex]);
                    //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);



                    OpenCvSharp.Point cor;
                    cor.X = maxTempX;
                    cor.Y = maxTempY;

                    //cor.X = 100;
                    //cor.Y = 100;

                    DrawCross(img,cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15,1);
                   
                    DrawText(img, maxTemp.ToString("F1"), cor);
                    pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
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




                    videoFilePath = Path.GetFullPath(directoryFileNames[0][item.Index]) + "\\" + Path.GetFileNameWithoutExtension(directoryFileNames[0][item.Index]) + ".mp4";
                    string irImageFilePath = Path.GetFileNameWithoutExtension(irJpegFiles[currentIrImageIndex]);
                    videoCapture = new VideoCapture(videoFilePath);

                    string vedioFileNameWithoutExtension = Path.GetFileNameWithoutExtension(Path.GetFullPath(videoFilePath));
                    string vedioHour = vedioFileNameWithoutExtension.Substring(9, 2);
                    string vedioMin = vedioFileNameWithoutExtension.Substring(11, 2);
                    string vedioSec = vedioFileNameWithoutExtension.Substring(13, 2);
                    string vedioMillsec = vedioFileNameWithoutExtension.Substring(16, 3);

                    videoInfo.hour = Convert.ToUInt16(vedioHour);
                    videoInfo.min = Convert.ToUInt16(vedioMin);
                    videoInfo.sec = Convert.ToUInt16(vedioSec);
                    videoInfo.millsec = Convert.ToUInt16(vedioMillsec);

                    GetVideoFrames(irImageFilePath, OP_Frames[0]);
                    currentOpImageIndex = 4;
                    pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);

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
                    startFrameIndex = frameIndex;
                }


                if (frameIndex + 4 < videoCapture.FrameCount)
                {
                    endFrameIndex = frameIndex + 4;
                }
                else
                {
                    endFrameIndex = frameIndex;
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
                            //pics[i * 2 + j].Paint += new PaintEventHandler(Pics0_Paint);
                            //pics[i * 2 + j].MouseClick += new MouseEventHandler(Pics0_MouseClick);
                            //pics[i * 2 + j].MouseDown += new MouseEventHandler(Pics0_MouseDown);
                            //pics[i * 2 + j].MouseMove += new MouseEventHandler(Pics0_MouseMove);
                            //pics[i * 2 + j].MouseLeave += new EventHandler(Pics0_MouseLeave);
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

        private void DrawCross(Mat img,OpenCvSharp.Point cor,Scalar color,int crossLine,int lineWidth)
        {
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X + crossLine, cor.Y), color, lineWidth);
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X - crossLine, cor.Y), color, lineWidth);
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X, cor.Y + crossLine), color, lineWidth);
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X, cor.Y - crossLine), color, lineWidth);
        }

        private void Ir_Image_preview_btn_Click(object sender, EventArgs e)
        {

            if ((currentIrImageIndex - 1) >= 0)
            {
                currentIrImageIndex -= 1;
                Mat img = Cv2.ImRead(irImageListPath[0][currentIrImageIndex]);
                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);

                string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                  + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";
                float[] tempDatas = GetTempFileToArray(tempFilePath);
                List<float> tempDataList = new List<float>();
                tempDataList = tempDatas.ToList();
                float maxTemp = tempDataList.Max();
                int index = tempDataList.IndexOf(maxTemp);
                int maxTempY = index / TEMP_WIDTH;
                int maxTempX = index % TEMP_WIDTH;


                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);

                OpenCvSharp.Point cor;
                cor.X = maxTempX;
                cor.Y = maxTempY;

                //cor.X = 100;
                //cor.Y = 100;

                DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);

                //cor.X = 100;
                //cor.Y = 100;

                DrawText(img, maxTemp.ToString("F1"), cor);
                //Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 20), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.Green, 1);
                pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);

                GetVideoFrames(irImageListPath[0][currentIrImageIndex], OP_Frames[0]);
                currentOpImageIndex = 4;
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);
            }
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
                float[] tempDatas = GetTempFileToArray(tempFilePath);
                List<float> tempDataList = new List<float>();
                tempDataList = tempDatas.ToList();
                float maxTemp = tempDataList.Max();
                int index = tempDataList.IndexOf(maxTemp);
                int maxTempY = index / TEMP_WIDTH;
                int maxTempX = index % TEMP_WIDTH;


                OpenCvSharp.Point cor;
                cor.X = maxTempX;
                cor.Y = maxTempY;

                //cor.X = 100;
                //cor.Y = 100;

                DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);
                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);


                //cor.X = 100;
                //cor.Y = 100;

                DrawText(img, maxTemp.ToString("F1"), cor);
                //Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 20), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.Green, 1);

                pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);


                GetVideoFrames(irImageListPath[0][currentIrImageIndex], OP_Frames[0]);
                currentOpImageIndex = 4;
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);
            }

        }

        private void Op_image_preview_btn_Click(object sender, EventArgs e)
        {

            if ((currentOpImageIndex - 1) >= 0)
            {
                currentOpImageIndex -= 1;
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);
            }
        }

        private void Op_image_next_btn_Click(object sender, EventArgs e)
        {

            if ((currentOpImageIndex + 1) < OP_Frames[0].Count)
            {
                currentOpImageIndex += 1;
                pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(OP_Frames[0][currentOpImageIndex]);

            }

        }
    }
}
