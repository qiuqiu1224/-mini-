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
    public partial class FAlarmData : UIPage
    {
        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const Int32 TEMP_WIDTH = 640;//红外图像宽度
        const Int32 TEMP_HEIGHT = 512;//红外图像宽度

        List<string>[] directoryFileNames = new List<string>[2];
        List<string>[] OpImageFileNames = new List<string>[2];
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

        public FAlarmData()
        {
            InitializeComponent();
            initData();
        }

        struct VideoInfo
        {
            public int hour;
            public int min;
            public int sec;
            public int millsec;
        }


        private void initData()
        {

            deviceCount = Globals.systemParam.deviceCount; //通过配置文件获取设备数量

            pics = new PictureBox[deviceCount * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

            for (int i = 0; i < deviceCount; i++)
            {
                directoryFileNames[i] = new List<string>();
                OpImageFileNames[i] = new List<string>();
                OP_Frames[i] = new List<Mat>();
                irImageListPath[i] = new List<string>();
            }
        }


        private void FAlarmData_Load(object sender, EventArgs e)
        {
            this.listView_AlarmData.View = View.Details;
            if (listView_AlarmData.Columns.Count == 0)
            {
                ColumnHeader columnHeader = new ColumnHeader();
                columnHeader.Text = "序号";
                columnHeader.Width = listView_AlarmData.Width * 3 / 10;
                columnHeader.TextAlign = HorizontalAlignment.Center;
                listView_AlarmData.Columns.Add(columnHeader);

                ColumnHeader columnHeader1 = new ColumnHeader();
                columnHeader1.Text = "探测时间";
                columnHeader1.Width = listView_AlarmData.Width * 7 / 10;
                columnHeader1.TextAlign = HorizontalAlignment.Center;
                listView_AlarmData.Columns.Add(columnHeader1);
                listView_AlarmData.FullRowSelect = true;


            }

            SetFmonitorDisplayWnds(1, 2);
            uiPanel3.SendToBack();

            label1.Left = listView_AlarmData.Width + panel1.Width / 8;
            label1.Visible = false;

            ir_Image_preview_btn.Left = label1.Right + 25;
            ir_Image_preview_btn.Visible = false;
            ir_image_next_btn.Left = ir_Image_preview_btn.Right + 25;
            ir_image_next_btn.Visible = false;

            label2.Left = listView_AlarmData.Width + panel1.Width * 5 / 8;
            label2.Visible = false;
            op_image_preview_btn.Left = label2.Right + 25;
            op_image_preview_btn.Visible = false;
            op_image_next_btn.Left = op_image_preview_btn.Right + 25;
            op_image_next_btn.Visible = false;

            uiDatePickerStart.Value = DateTime.Now;
            uiDatePickerEnd.Value = DateTime.Now;

            uiComboBox_DeviceName.Items.Add(Globals.systemParam.deviceName_0);
            if (Globals.systemParam.deviceCount > 1)
            {
                uiComboBox_DeviceName.Items.Add(Globals.systemParam.deviceName_1);
            }

            uiComboBox_DeviceName.SelectedIndex = 0;
        }

        /// <summary>
        /// 设置过车界面图像显示控件布局
        /// </summary>
        /// <param name="rowNum">行数</param>
        /// <param name="colNum">列数</param>

        private void SetFmonitorDisplayWnds(uint rowNum, uint colNum)
        {

            uint w = (uint)(this.Width - listView_AlarmData.Width);

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
                    uint x = (uint)listView_AlarmData.Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;

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

        private void UiButton1_Click(object sender, EventArgs e)
        {
            directoryFileNames[0].Clear();
            pics[0].Image = null;
            pics[1].Image = null;

            listView_AlarmData.Items.Clear();
            //if (listView_AlarmData.Items.Count >= 1)
            //{
            //    for (int j = listView_AlarmData.Items.Count - 1; j >= 0; j--)
            //    {
            //        listView_AlarmData.Items.RemoveAt(j);
            //    }
            //}

            //string folderPath = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Convert.ToDateTime(uiDatePickerStart.Text).ToString("yyyy_MM_dd") + "\\" + Globals.systemParam.ip_0; // 
            string folderPath = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + Convert.ToDateTime(uiDatePickerStart.Text).ToString("yyyy_MM_dd"); // 
            string opImageFolderPath = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + Convert.ToDateTime(uiDatePickerStart.Text).ToString("yyyy_MM_dd") + "\\" + "OP_Image";

            DirectoryInfo imageDirectoryInfo = new DirectoryInfo(folderPath);
            if (imageDirectoryInfo.Exists)
            {
                uiLabel3.Text = "";
                subdirectoryEntries_0 = Directory.GetDirectories(folderPath);

                int count = 0;
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

                    item.SubItems[0].Text = (count + 1).ToString();
                    listView_AlarmData.Columns[1].TextAlign = HorizontalAlignment.Left;
                    listView_AlarmData.Columns[0].TextAlign = HorizontalAlignment.Right;

                    fileName = fileName.Substring(0, 4) + "-" + fileName.Substring(4, 2) + "-" + fileName.Substring(6, 2) + " " + fileName.Substring(9, 2) + ":" + fileName.Substring(11, 2);
                    item.SubItems.Add(fileName);
                    listView_AlarmData.Items.Add(item);

                    count++;

                }


            }
            else
            {
                uiLabel3.Text = "没有报警数据";
            }
        }

        private void ListView_AlarmData_MouseClick(object sender, MouseEventArgs e)
        {
            pics[0].Image = null;
            pics[1].Image = null;
            string[] irJpegFiles;
            string[] opJpegFiles;
            if (e.Button == MouseButtons.Left)
            {
                currentIrImageIndex = 0;
                irImageListPath[0].Clear();

                ListViewHitTestInfo info = listView_AlarmData.HitTest(e.X, e.Y);
                item = info.Item;

                if (item != null)
                {
                    irJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index], "*.jpeg");//读取所有红外图像文件

                    foreach (string irFile in irJpegFiles)
                    {
                        irImageListPath[0].Add(irFile);
                    }

                    string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                        + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";

                    float[] tempDatas = Globals.GetTempFileToArray(tempFilePath);
                    List<float> tempDataList = tempDatas.ToList();
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

                    Globals.DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);

                    Globals.DrawText(img, maxTemp.ToString("F1"), cor);
                    pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
                    label1.Text = "红外图像 共" + irImageListPath[0].Count + "张 第" + (currentIrImageIndex + 1) + "张";
                    label1.Visible = true;
                    ir_Image_preview_btn.Visible = true;
                    ir_image_next_btn.Visible = true;

                    string irImageFilePath = Path.GetFileNameWithoutExtension(irJpegFiles[currentIrImageIndex]);

                    opJpegFiles = Directory.GetFiles(directoryFileNames[0][item.Index] + "\\" + "OP_Image");//读取所有红外图像文件
                    OpImageFileNames[0].Clear();
                    OpImageFileNames[0] = Globals.GetOPImages(irImageFilePath, opJpegFiles);

                    currentOpImageIndex = 0;
                    img = Cv2.ImRead(OpImageFileNames[0][currentOpImageIndex]);
                    pics[1].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
                    label2.Text = "可见光图像 共" + OpImageFileNames[0].Count + "张 第" + (currentOpImageIndex + 1) + "张";
                    label2.Visible = true;
                    op_image_preview_btn.Visible = true;
                    op_image_next_btn.Visible = true;


                }

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

        private void Ir_image_next_btn_Click(object sender, EventArgs e)
        {

            if ((currentIrImageIndex + 1) < irImageListPath[0].Count)
            {
                currentIrImageIndex += 1;
                Mat img = Cv2.ImRead(irImageListPath[0][currentIrImageIndex]);
                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);


                string tempFilePath = directoryFileNames[0][item.Index] + "\\" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(0, 20)
                      + "temp" + Path.GetFileNameWithoutExtension(irImageListPath[0][currentIrImageIndex]).Substring(22) + ".dat";
                float[] tempDatas = Globals.GetTempFileToArray(tempFilePath);
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

                Globals.DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);
                //Cv2.Line(img, 0, 0, 100, 100, OpenCvSharp.Scalar.FromRgb(0, 255, 0), 2);


                //cor.X = 100;
                //cor.Y = 100;

                Globals.DrawText(img, maxTemp.ToString("F1"), cor);
                //Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 20), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.Green, 1);

                pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
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
            }
        }

        private void ListView_AlarmData_SelectedIndexChanged(object sender, EventArgs e)
        {

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
                float[] tempDatas = Globals.GetTempFileToArray(tempFilePath);
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

                Globals.DrawCross(img, cor, OpenCvSharp.Scalar.FromRgb(220, 20, 60), 15, 1);

                //cor.X = 100;
                //cor.Y = 100;

                Globals.DrawText(img, maxTemp.ToString("F1"), cor);
                //Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 20), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.Green, 1);
                pics[0].Image = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(img);
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
       

            }
        }
    }
}
