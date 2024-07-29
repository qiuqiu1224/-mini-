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
using Sunny.UI;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Media;
using System.Drawing.Imaging;
using System.Diagnostics;
using OpenCvSharp;

namespace PreviewDemo
{
    public partial class Form1 : UIForm
    {

        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const int PAGE_INDEX = 1000;
        const Int32 IR_IMAGE_WIDTH = 640;//红外图像宽度
        const Int32 IR_IMAGE_HEIGHT = 512;//红外图像宽度

        Color PIC_CLICKED_COLOR = Color.FromArgb(128, 128, 255);
        Color PIC_UNCLICKED_COLOR = Color.FromArgb(45, 45, 53);
        private PictureBox[] pics;//显示图像控件
        private UIPage fmonitor;//监控界面
        private UIPage fbrowse;//浏览界面
        private UIPage fVehicleData;//过车数据界面
        private UIPage fAlarmData;//报警数据界面
        // private UIPanel pixUIPanel;//容纳PictureBox的Panel
        //public static TransparentLabel[] labels;//图像上面标注控件,背景透明

        UISymbolButton startPrewviewBtn, stopPrewviewBtn, startRecordBtn, stopRecordBtn,
            mouseFollowBtn, takePicBtn, drawRectBtn, drawCircleBtn, deleteAllDrawBtn;

        ListView trainListView;
        private bool isTrainStart = false;//开始采集标志
        List<Socket> sockets = new List<Socket>();//连接红外相机获取温度socket
        private delegate string ConnectSocketDelegate(IPEndPoint ipep, Socket sock);
        List<Thread> threadsReceiveTmp = new List<Thread>();//接收温度数据线程
        //private int[,] realTemp;
        private List<int[,]> realTemps = new List<int[,]>();//存储温度数据
        //接收数据缓存
        private List<byte[]> dataBuffers = new List<byte[]>();
        //从dataBuffer已经存了多少个字节数据
        private List<int> contentSizes = new List<int>();
        private List<bool> receiveFlags = new List<bool>();
        private List<bool> socketReceiveFlags = new List<bool>();

        //private bool mouseFollowFlag = false;//鼠标跟随标志

        List<int> picMouseX = new List<int>();//鼠标跟随x坐标
        List<int> picMouseY = new List<int>();//鼠标跟随y坐标
        List<float> tempMouseX = new List<float>();//鼠标跟随对应温度数组x坐标
        List<float> tempMouseY = new List<float>();//鼠标跟随对应温度数组y坐标


        private List<bool> saveVideoFlags = new List<bool>();


        private bool saveVideoFlag = false;//录制视频标志
        private bool saveImageFlag;//保存图像标志
        private bool saveAlarmImageFlag;//保存报警图像标志
        private bool alertFlag;//报警标志

        string recordName;//录制视频文件名
                          // VideoWriter writer;//存储红外视频对象
        private bool isInPic;//判断鼠标是否在图像内的标志

        string sVideoFileName;

        int selectType = -1;

        List<Object> objects = new List<object>();

        List<Bitmap> imageList = new List<Bitmap>();

        List<TempRuleInfo> tempRuleInfos = new List<TempRuleInfo>();
        int rectModeIndex = 0;
        IRC_NET_POINT mouseDownPoint = new IRC_NET_POINT();
        private List<IRC_NET_POINT> points = new List<IRC_NET_POINT>();
        public int iNowPaint_X_Start = 0;
        public int iNowPaint_Y_Start = 0;
        public int iNowPaint_X_End = 0;
        public int iNowPaint_Y_End = 0;
        public int iTempType = 2;
        bool idraw = false;
        //public float fSx;//在红外显示控件上画选框，转换成红外视频帧x轴方向的缩放比例
        //public float fSy;//在红外显示控件上画选框，转换成红外视频帧y轴方向的缩放比例


        #region 海康mini云台变量
        private bool m_bInitSDK = false;
        private uint iLastErr = 0;
        private string str;
        List<int> mUserIDs = new List<int>();//设备ID集合
        List<string> ipList = new List<string>();//设备ip集合
        List<int> mRealHandles = new List<int>();//实时显示句柄集合

        public CHCNetSDK.NET_DVR_USER_LOGIN_INFO struLogInfo;//登录信息结构
        public CHCNetSDK.NET_DVR_DEVICEINFO_V40 DeviceInfo;//设备信息结构
        List<CHCNetSDK.LOGINRESULTCALLBACK> LoginCallBacks = new List<CHCNetSDK.LOGINRESULTCALLBACK>();//登录回调函数集合
        List<CHCNetSDK.NET_DVR_DEVICEINFO_V40> DeviceInfos = new List<CHCNetSDK.NET_DVR_DEVICEINFO_V40>();//设备信息集合
        List<CHCNetSDK.REALDATACALLBACK> RealDatas = new List<CHCNetSDK.REALDATACALLBACK>();//实时数据回调函数集合

        Thread GetImageDataThread;//获取温度数据线程
        Thread SaveOPImageThread;//保存可见光图像线程

        byte cacheDataCount = 0;//红外热成像缓存数据数量

        List<byte>[] cacheData = new List<byte>[20];//红外图像和温度数据缓存集合
        private List<IntPtr> m_ptrRealHandles = new List<IntPtr>();
        string savePath;
        bool isSavingIrImg;//是否正在缓存红外图像和温度数据标志
        //bool isCopyOpImage;
        //bool isAlarm = false;
        List<bool> isAlarm = new List<bool>();
        List<bool> isCopyOpImage = new List<bool>();

        List<string> saveReportPath = new List<string>();//保存过车数据根目录集合
        List<string> alarmReportPath = new List<string>();//保存报警数据根目录集合

        IndexListInfo indexList;
        int trainIndexCount = 0;

        #endregion


        public struct TempRuleInfo
        {
            public int type;
            public int index;
            public int startPointX;
            public int startPointY;
            public int endPointX;
            public int endPointY;
            public int maxTemp;
            public int maxTempLocX;
            public int maxTempLocY;
        }

        /// <summary>
        /// 测温工具模式
        /// </summary>
        public enum DrawMode
        {
            NO_DRAW = -1,
            DRAW_POINT,
            DRAW_LINE,
            DRAW_AREA,
            DRAW_CIRCLE,
            DRAW_POLYGON,
            DRAW_MOUSE//鼠标跟随
        }

        public struct IRC_NET_POINT
        {
            public int x; ///< x坐标
            public int y; ///< y坐标
        }


        [Obsolete]
        public Form1()
        {
            InitializeComponent();

            Control.CheckForIllegalCrossThreadCalls = false;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲

            //读取配置文件
            Globals.ReadInfoXml<SystemParam>(ref Globals.systemParam, Globals.systemXml);

            int pageIndex = PAGE_INDEX;

            //设置关联
            uiNavBar1.TabControl = uiTabControl1;

            //uiNavBar1设置节点，也可以在Nodes属性里配置
            uiNavBar1.Nodes.Add("在线监控");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[0], 61501);//设置图标

            ////添加实时监控界面
            fmonitor = new FMonitor();
            AddPage(fmonitor, pageIndex);
            uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[0], pageIndex);//设置显示的初始界面为实时监控界面

            pageIndex++;
            uiNavBar1.Nodes.Add("过车数据");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[1], 362008);

            fVehicleData = new FormVehicleData();
            AddPage(fVehicleData, pageIndex);
            uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[1], pageIndex);//添加过车数据界面

            //uiNavBar1.Nodes.Add("图像浏览");
            //uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[1], 61502);

            ////添加图像浏览界面  PAGE_INDEX + 1
            //pageIndex++;
            //fbrowse = new FormBrowse();
            //AddPage(fbrowse, pageIndex);
            //uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[1], pageIndex);

            pageIndex++;
            uiNavBar1.Nodes.Add("报警数据");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[2], 62151);
            fAlarmData = new FAlarmData();
            AddPage(fAlarmData, pageIndex);
            uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[2], pageIndex);


            uiNavBar1.Nodes.Add("系统设置");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[3], 61459);

            //初始化数据
            initDatas();

            //初始化图像显示控件布局
            SetFmonitorDisplayWnds((uint)Globals.systemParam.deviceCount, 2);


            trainListView = (ListView)fmonitor.GetControl("listView_trainInfo");
            //trainListView.Left = pics[0].Left;
            //trainListView.Width = pics[0].Width + pics[1].Width;

            //if (Globals.systemParam.deviceCount == 1)
            //{
            //    trainListView.Top = pics[0].Bottom;

            //    trainListView.Height = Screen.PrimaryScreen.Bounds.Height - pics[0].Bottom;
            //}
            //else
            //{
            //    trainListView.Top = pics[2].Bottom;
            //    trainListView.Height = Screen.PrimaryScreen.Bounds.Height - pics[2].Bottom;
            //}


            //获取Fmonitor界面开始采集按钮，并添加相关事件
            startPrewviewBtn = (UISymbolButton)fmonitor.GetControl("startPrewviewBtn");
            startPrewviewBtn.Click += new EventHandler(StartPrewviewBtn_Click);
            startPrewviewBtn.MouseHover += new EventHandler(StartPrewviewBtn_MouseHover);
            startPrewviewBtn.MouseLeave += new EventHandler(StartPrewviewBtn_MouseLeave);

            //获取Fmonitor界面停止按钮，并添加相关事件
            stopPrewviewBtn = (UISymbolButton)fmonitor.GetControl("stopPrewviewBtn");
            stopPrewviewBtn.Click += new EventHandler(StopPrewviewBtn_Click);
            stopPrewviewBtn.MouseHover += new EventHandler(StopPrewviewBtn_MouseHover);
            stopPrewviewBtn.MouseLeave += new EventHandler(StopPrewviewBtn_MouseLeave);

            //获取Fmonitor界面开始录制视频按钮，并添加相关事件
            startRecordBtn = (UISymbolButton)fmonitor.GetControl("startRecordBtn");
            startRecordBtn.Click += new EventHandler(StartRecordBtn_Click);
            startRecordBtn.MouseHover += new EventHandler(StartRecordBtn_MouseHover);
            startRecordBtn.MouseLeave += new EventHandler(StartRecordBtn_MouseLeave);

            //获取Fmoitor界面停止录制视频按钮，并添加相关事件
            stopRecordBtn = (UISymbolButton)fmonitor.GetControl("stopRecordBtn");
            stopRecordBtn.Click += new EventHandler(StopRecordBtn_Click);
            stopRecordBtn.MouseHover += new EventHandler(stopRecordBtn_MouseHover);
            stopRecordBtn.MouseLeave += new EventHandler(stopRecordBtn_MouseLeave);

            //获取Fmoitor界面鼠标跟随按钮，并添加相关事件
            mouseFollowBtn = (UISymbolButton)fmonitor.GetControl("mouseFollowBtn");
            mouseFollowBtn.Click += new EventHandler(mouseFollowBtn_Click);
            mouseFollowBtn.MouseHover += new EventHandler(mouseFollowBtn_MouseHover);
            mouseFollowBtn.MouseLeave += new EventHandler(mouseFollowBtn_MouseLeave);

            //获取Fmoitor界面拍照按钮，并添加相关事件
            takePicBtn = (UISymbolButton)fmonitor.GetControl("takePicBtn");
            takePicBtn.Click += new EventHandler(takePicBtn_Click);
            takePicBtn.MouseHover += new EventHandler(takePicBtn_MouseHover);
            takePicBtn.MouseLeave += new EventHandler(takePicBtn_MouseLeave);

            //获取Fmoitor界面画矩形按钮，并添加相关事件
            drawRectBtn = (UISymbolButton)fmonitor.GetControl("drawRectBtn");
            drawRectBtn.Click += new EventHandler(drawRectBtn_Click);
            drawRectBtn.MouseHover += new EventHandler(drawRectBtn_MouseHover);
            drawRectBtn.MouseLeave += new EventHandler(drawRectBtn_MouseLeave);

            //获取Fmoitor界面画圆形按钮，并添加相关事件
            drawCircleBtn = (UISymbolButton)fmonitor.GetControl("drawCircleBtn");
            drawCircleBtn.Click += new EventHandler(drawCircleBtn_Click);
            drawCircleBtn.MouseHover += new EventHandler(drawCircleBtn_MouseHover);
            drawCircleBtn.MouseLeave += new EventHandler(drawCircleBtn_MouseLeave);

            //获取Fmoitor界面删除所有选区按钮，并添加相关事件
            deleteAllDrawBtn = (UISymbolButton)fmonitor.GetControl("deleteAllDrawBtn");
            deleteAllDrawBtn.Click += new EventHandler(deleteAllDrawBtn_Click);
            deleteAllDrawBtn.MouseHover += new EventHandler(deleteAllDrawBtn_MouseHover);
            deleteAllDrawBtn.MouseLeave += new EventHandler(deleteAllDrawBtn_MouseLeave);

            //为按钮添加提示信息
            uiToolTip1.SetToolTip(startPrewviewBtn, "开始采集");
            uiToolTip1.SetToolTip(stopPrewviewBtn, "停止采集");
            uiToolTip1.SetToolTip(startRecordBtn, "开始录制");
            uiToolTip1.SetToolTip(stopRecordBtn, "停止录制");
            uiToolTip1.SetToolTip(mouseFollowBtn, "鼠标跟随");
            uiToolTip1.SetToolTip(takePicBtn, "手动抓图");
            uiToolTip1.SetToolTip(drawRectBtn, "矩形测温");
            uiToolTip1.SetToolTip(drawCircleBtn, "圆形测温");
            uiToolTip1.SetToolTip(deleteAllDrawBtn, "删除所有选区");


            //uiNavBar1.SelectedIndex = 0;
            //StartPrewview();

            //登录设备0
            LoginDevice(0, Globals.systemParam.ip_0, Globals.systemParam.username_0, Globals.systemParam.psw_0, Globals.systemParam.port_0, cbLoginCallBack_0);

            Prewview(0, 0, 1, RealDataCallBack_OP_0, false);//预览可见光图像

            Prewview(0, 1, 2, RealDataCallBack_IR_0, false);//预览红外图像

            //新建获取红外图像和温度数据线程
            GetImageDataThread = new Thread(GetImage);
            GetImageDataThread.IsBackground = true;
            GetImageDataThread.Priority = ThreadPriority.Highest;
            GetImageDataThread.Start();

            //新建保存可见光图像线程
            SaveOPImageThread = new Thread(SaveOPImage);
            SaveOPImageThread.IsBackground = true;
            SaveOPImageThread.Start();


            Thread.Sleep(100);


        }

        /// <summary>
        /// 保存可见光图像
        /// </summary>
        private void SaveOPImage()
        {
            while (true)
            {
                if (isTrainStart)//开始过车采集
                {

                    CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA();
                    lpJpegPara.wPicQuality = 2; //图像质量 Image quality
                    lpJpegPara.wPicSize = 0xff; //抓图分辨率 Picture size: 0xff-Auto(使用当前码流分辨率) 
                                                //抓图分辨率需要设备支持，更多取值请参考SDK文档

                    long dateTimeNow = TicksTimeConvert.GetNowTicks13();
                    DateTime aa = TicksTimeConvert.Ticks132LocalTime(dateTimeNow);  //时间戳转本地时间

                    string strTime = aa.ToString("yyyyMMdd_HHmmss_fff");//格式化时间

                    //JPEG抓图保存成文件 Capture a JPEG picture
                    string sJpegPicFileName;
                    sJpegPicFileName = saveReportPath[0] + "\\" + "OP_Image";

                    //sJpegPicFileName = "filetest.jpg";//图片保存路径和文件名 the path and file name to save

                    //判断文件夹是否存在，如果不存在，新建文件夹
                    if (!Directory.Exists(sJpegPicFileName))
                    {
                        Directory.CreateDirectory(sJpegPicFileName);
                    }
                    sJpegPicFileName += "\\" + strTime + ".jpeg";

                    //通过SDK进行可见光抓图
                    if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture(mUserIDs[0], 1, ref lpJpegPara, sJpegPicFileName))
                    {
                        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                        str = "设备" + ipList[0] + "可见光抓图失败！错误码为：" + iLastErr;
                        Globals.Log(str);
                        return;
                    }
                }
                Thread.Sleep(2);
            }
        }

        /// <summary>
        /// 指针转换为字节数组
        /// </summary>
        /// <param name="intPtr"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static byte[] IntPtrToByteArray(IntPtr intPtr, int length)
        {
            byte[] byteArray = new byte[length];
            Marshal.Copy(intPtr, byteArray, 0, length);
            return byteArray;
        }

        /// <summary>
        /// 获取红外图像和温度数据，并将其存入缓冲区
        /// </summary>
        private void GetImage()
        {
            while (true)
            {
                if (isTrainStart)
                {

                    isSavingIrImg = true;

                    if (mUserIDs[0] >= 0)
                    {
                        //Console.WriteLine(DateTime.Now);
                        try
                        {
                            //Stopwatch stopwatch = new Stopwatch();
                            //stopwatch.Start();

                            long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();  // 获取当前的时间戳
                            DateTime aa = TicksTimeConvert.Ticks132LocalTime(currentTimestamp);  //时间戳转本地时间

                            byte[] dateTimeNowBytes = new byte[8];
                            dateTimeNowBytes = TicksTimeConvert.TimestampToBytes(currentTimestamp);//将时间戳转换为字节数组

                            //设备抓图附加全屏测温数据结构体，海康mini云台不支持同时获取可见光图像
                            CHCNetSDK.NET_DVR_JPEGPICTURE_WITH_APPENDDATA struJpegWithAppendData = new CHCNetSDK.NET_DVR_JPEGPICTURE_WITH_APPENDDATA();
                            IntPtr ptr1 = Marshal.AllocHGlobal(100 * 1024);//分配内存
                            IntPtr ptr2 = Marshal.AllocHGlobal(2 * 1024 * 1024);
                            IntPtr ptr3 = Marshal.AllocHGlobal(4 * 1024 * 1024);
                            struJpegWithAppendData.pJpegPicBuff = ptr1;//为红外热成像Jpeg图片指针分配存储空间---100BK，实际大于32KB（640*512）
                            struJpegWithAppendData.pP2PDataBuff = ptr2;//为全屏测温数据指针分配存储空间---2MB，实际1.25MB（640*512）
                            struJpegWithAppendData.pVisiblePicBuff = ptr3;//为可见光图片指针分配存储空间---4MB

                            //获取红外图像和温度数据
                            bool res = CHCNetSDK.NET_DVR_CaptureJPEGPicture_WithAppendData(mUserIDs[0], 2, ref struJpegWithAppendData);

                            if (res != true)
                            {
                                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                                str = "设备" + ipList[0] + "热成像抓图失败，错误码为：" + iLastErr;
                                Globals.Log(str);
                                return;
                            }

                            //将红外图像指针转字节数组
                            byte[] IRImageArray = new byte[struJpegWithAppendData.dwJpegPicLen];
                            IRImageArray = IntPtrToByteArray(struJpegWithAppendData.pJpegPicBuff, (int)struJpegWithAppendData.dwJpegPicLen);

                            //将温度数据指针转字节数组
                            byte[] IRTempArray = new byte[struJpegWithAppendData.dwP2PDataLen];
                            IRTempArray = IntPtrToByteArray(struJpegWithAppendData.pP2PDataBuff, (int)struJpegWithAppendData.dwP2PDataLen);

                            //将温度字节数组转换为实际温度数组，四个字节表示一个温度数据
                            float[] temp = new float[IR_IMAGE_WIDTH * IR_IMAGE_HEIGHT];
                            temp = Globals.TempBytesToTempFloats(IRTempArray, IR_IMAGE_WIDTH, IR_IMAGE_HEIGHT);

                            //获取温度最大值
                            List<float> list = temp.ToList();
                            float maxTemp = list.Max();


                            int index;
                            //红外数据缓存数量大于等于20条，
                            if (cacheDataCount >= 20)
                            {   //将每条数据的最大值存到集合中
                                List<int> maxTempList = new List<int>();
                                for (int i = 0; i < cacheData.Length; i++)
                                {
                                    maxTempList.Add(BitConverter.ToInt32(cacheData[i].GetRange(0, 4).ToArray(), 0));
                                }

                                //获取集合中最小温度值及其位置
                                int min = maxTempList.Min();
                                int minIndex = maxTempList.IndexOf(min);

                                //如果当前温度最大值>集合中最小温度值，则将其替换
                                if(maxTemp > min)
                                {
                                    cacheData[minIndex].Clear();
                                    index = minIndex;
                                    byte[] t = new byte[4];
                                    t = BitConverter.GetBytes((maxTemp * 10));

                                    cacheData[index].AddRange(t);//添加最高温*10转换成字节数组，4个字节
                                    cacheData[index].AddRange(dateTimeNowBytes);//添加当前时间戳8个字节

                                    for (int i = 0; i < 2; i++)//2个预留字节
                                    {
                                        cacheData[index].Add(0x00);
                                    }

                                    cacheData[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwJpegPicLen));//添加红外图像长度，4个字节
                                    cacheData[index].AddRange(IRImageArray);//添加红外图像数据
                                    cacheData[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwP2PDataLen));//添加温度数据长度，四个字节
                                    cacheData[index].AddRange(IRTempArray);//添加温度数据
                                                                           //cacheData[index].AddRange(BitConverter.GetBytes(dwSizeReturned));//添加可见光图像长度，4个字节
                                                                           //cacheData[index].AddRange(byJpegPicBuffer);//添加可见光图像数据
                                }
                            }
                            else//缓存中数据少于20条，依次添加数据
                            {
                                index = cacheDataCount;
                                byte[] t = new byte[4];
                                t = BitConverter.GetBytes((maxTemp * 10));//最高温乘以10，转换为字节数组

                                cacheData[index].AddRange(t);//添加最高温*10转换成字节数组，4个字节
                                cacheData[index].AddRange(dateTimeNowBytes);//添加当前时间戳8个字节

                                for (int i = 0; i < 2; i++)//10个预留字节
                                {
                                    cacheData[index].Add(0x00);
                                }

                                cacheData[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwJpegPicLen));//添加红外图像长度，4个字节
                                cacheData[index].AddRange(IRImageArray);//添加红外图像数据
                                cacheData[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwP2PDataLen));//添加温度数据长度，4个字节
                                cacheData[index].AddRange(IRTempArray);//添加温度数据
                                                                       //cacheData[index].AddRange(BitConverter.GetBytes(dwSizeReturned));//添加可见光图像长度，4个字节
                                                                       //cacheData[index].AddRange(byJpegPicBuffer);//添加可见光图像数据
                            }


                            //int maxTempIndex = list.IndexOf(maxTemp);//获取最高温度值所在位置
                            //int maxTempX = maxTempIndex % IR_IMAGE_WIDTH;//最高温度值x坐标
                            //int maxTempY = maxTempIndex / IR_IMAGE_WIDTH;//最高温度值y坐标

                            //RectangleF rectF = GetRectArea(maxTempX, maxTempY, 5, 5);//选择最高温度点周围10*10的区域

                            //float[] result = getTempAtRect(temp, rectF, IR_IMAGE_WIDTH);//获取该区域的最值、平均值 result[2] 为平均值

                            //if (result[2] > Globals.systemParam.alarm_0)
                            //{
                            //    isAlarm = true;
                            //}

                            Marshal.FreeHGlobal(ptr1);//释放内存
                            Marshal.FreeHGlobal(ptr2);
                            Marshal.FreeHGlobal(ptr3);


                            //stopwatch.Stop();
                            //long a = stopwatch.ElapsedMilliseconds;
                            //Console.WriteLine(a);

                        }
                        catch (Exception ex)
                        {
                            str = "设备" + ipList[0] + "热成像抓图失败，异常信息为：" + ex.ToString();
                            Globals.Log(str);
                        }

                        cacheDataCount++;//缓存数据数量加1
                    }
                    isSavingIrImg = false;//缓存红外图像数据结束

                }

                Thread.Sleep(1);

            }
        }

        /// <summary>
        /// 拷贝文件夹
        /// </summary>
        /// <param name="sourceDir"></param>
        /// <param name="targetDir"></param>
        public static void CopyDirectory(string sourceDir, string targetDir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // If the source directory does not exist, throw an exception.
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
            }

            // If the destination directory does not exist, create it.
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(targetDir, file.Name);
                file.CopyTo(tempPath, false);
            }

            // If copying subdirectories, copy them and their contents to the new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(targetDir, subdir.Name);
                CopyDirectory(subdir.FullName, tempPath);
            }
        }


        //温度最大值（最大值*10）      时间戳    轴序     红外图像长度   红外数据   温度数据长度     温度数据
        // 4个字节                     8个字节   2个字节     4个字节                   4个字节
        /// <summary>
        /// 解析缓存数据
        /// </summary>
        private void AnalysisData()
        {
            for (int i = 0; i < cacheData.Length; i++)
            {
                if (cacheData[i].Count > 0)
                {

                    float maxTemp = BitConverter.ToSingle(cacheData[i].GetRange(0, 4).ToArray(), 0);//获取最高温
                    //Console.WriteLine("最大值" + maxTemp * 1.0f / 10);
                    byte[] timeBytes = cacheData[i].GetRange(4, 8).ToArray();//获取时间戳字节数组

                    long time = TicksTimeConvert.BytesToTimestamp(timeBytes);//获取时间戳      
                    DateTime aa = TicksTimeConvert.Ticks132LocalTime(time);  //时间戳转本地时间

                    short axelNum = BitConverter.ToInt16(cacheData[i].GetRange(12, 2).ToArray(), 0);//获取轴序

                    string strTime = aa.ToString("yyyyMMdd_HHmmss_fff");//格式化本地时间

                    int IRImageLength = BitConverter.ToInt32(cacheData[i].GetRange(4 + 8 + 2, 4).ToArray(), 0);//获取红外图像数据的长度
                    int tempDataLength = BitConverter.ToInt32(cacheData[i].GetRange(4 + 8 + 2 + 4 + IRImageLength, 4).ToArray(), 0);//获取温度数据的长度

                    byte[] IRTempArray = cacheData[i].GetRange(4 + 8 + 2 + 4 + IRImageLength + 4, tempDataLength).ToArray();//获取红外图像温度字节数组
                    float[] temp = Globals.TempBytesToTempFloats(IRTempArray, IR_IMAGE_WIDTH, IR_IMAGE_HEIGHT);//将温度字节数组转换为实际温度数组
                    List<float> tempList = temp.ToList();//将温度数组转为集合
                    maxTemp = tempList.Max();//获取最高温度值

                    int maxTempIndex = tempList.IndexOf(maxTemp);//获取最高温度值所在位置
                    int maxTempX = maxTempIndex % IR_IMAGE_WIDTH;//最高温度值x坐标
                    int maxTempY = maxTempIndex / IR_IMAGE_WIDTH;//最高温度值y坐标

                    RectangleF rectF = GetRectArea(maxTempX, maxTempY, 5, 5);//选择最高温度点周围10*10的区域
                    float[] result = getTempAtRect(temp, rectF, IR_IMAGE_WIDTH);//获取该区域的最值、平均值 result[2] 为平均值

                    string irImagePath = saveReportPath[0] + "\\" + strTime + "_IR_" + i + ".jpeg";//红外图像文件名
                    string tempDataPath = saveReportPath[0] + "\\" + strTime + "_temp_" + i + ".dat";//温度数据文件名

                    WriteBytesToFile(irImagePath, cacheData[i].GetRange(4 + 8 + 2 + 4, IRImageLength).ToArray(), IRImageLength);//保存红外图像
                    WriteBytesToFile(tempDataPath, IRTempArray, tempDataLength);//保存温度数据
                    //Console.WriteLine("平均值" + result[2]);

                    //最大值温度点周围10*10区域温度平均值大于报警阈值，存储报警图片
                    if (result[2] > Globals.systemParam.alarm_0)
                    {
                        isAlarm[0] = true;
                        if (!Directory.Exists(alarmReportPath[0]))
                        {
                            Directory.CreateDirectory(alarmReportPath[0]);
                        }
                        string alarmDataPath = alarmReportPath[0] + "\\" + strTime + "_temp_" + i + ".dat";//温度数据文件名
                        string alarmIrImagePath = alarmReportPath[0] + "\\" + strTime + "_IR_" + i + ".jpeg";//红外图像文件名

                        WriteBytesToFile(alarmIrImagePath, cacheData[i].GetRange(4 + 8 + 2 + 4, IRImageLength).ToArray(), IRImageLength);//保存红外图像
                        WriteBytesToFile(alarmDataPath, IRTempArray, tempDataLength);//保存温度数据
                        if (isCopyOpImage[0] == false)//拷贝可见光图片文件夹
                        {
                            CopyDirectory(saveReportPath[0] + "\\" + "OP_Image", alarmReportPath[0] + "\\" + "OP_Image");
                            isCopyOpImage[0] = true;
                        }

                    }

                }
            }
            //C:\HIK\SaveReport\三间房站\设备0\2024_07_25\20240725_151937_225
            string[] splitPath = saveReportPath[0].Split('\\');//分割路径字符串
            string saveXmlPath = null;


            for (int i = 0; i < splitPath.Length - 2; i++)
            {

                saveXmlPath += splitPath[i] + "\\";

            }

            //索引文件路径,当日日期（年_月_日）为文件名
            saveXmlPath += splitPath[5] + ".xml";

            //IndexListInfo indexList = new IndexListInfo();
            //Globals.ReadInfoXml<IndexListInfo>(ref indexList, saveXmlPath);

            TrainIndex trainIndex = new TrainIndex();
            if (isAlarm[0])
            {
                trainIndex.isAlarm = "是";              
            }
            else
            {
                trainIndex.isAlarm = "否";
            }

            trainIndex.detectTime = splitPath[6].Substring(9, 2) + ":" + splitPath[6].Substring(11, 2);
            trainIndex.IndexID = (uint)trainIndexCount + 1;
            //indexList.trainIndexList.Add(trainIndex);
            indexList.trainIndexList.Add(trainIndex);
            Globals.WriteInfoXml<IndexListInfo>(indexList, saveXmlPath);//将过车数据写入索引文件


            ListViewItem item = new ListViewItem();
            //item.SubItems.Add("");
            item.SubItems.Add(trainIndex.IndexID.ToString());
            item.SubItems.Add(trainIndex.detectTime.ToString());
            item.SubItems.Add(trainIndex.isAlarm.ToString());
            if ("是".Equals(trainIndex.isAlarm.ToString()))
            {
                item.ForeColor = Color.Red;
            }
          
            trainListView.Items.Add(item);


            isAlarm[0] = false;
            trainIndexCount++;
            //string videoPath = "your_video.mp4";
            //// 需要获取的帧索引列表
            //int[] frameIndexes = new int[] { 10, 20, 30 }; // 第10, 20, 30帧

            //using (var capture = new VideoCapture(videoPath))
            //{
            //    if (capture.IsOpened())
            //    {
            //        foreach (int frameIndex in frameIndexes)
            //        {
            //            capture.Set(CaptureProperty.PosFrames, frameIndex);
            //            using (Mat frame = new Mat())
            //            {
            //                capture.Read(frame);
            //                if (!frame.Empty())
            //                {
            //                    // 这里可以对帧frame进行处理
            //                    // 例如保存帧到文件
            //                    string outputPath = $"frame_{frameIndex}.png";
            //                    Cv2.ImWrite(outputPath, frame);
            //                }
            //            }
            //        }
            //    }
            //}

        }


        /// <summary>
        /// 获取某点周围的矩形区域
        /// </summary>
        /// <param name="locx">点的x坐标</param>
        /// <param name="locy">点的y坐标</param>
        /// <param name="w">矩形区域宽的一半</param>
        /// <param name="h">矩形区域高的一半</param>
        /// <returns></returns>
        private RectangleF GetRectArea(int locx, int locy, int w, int h)
        {
            RectangleF rectangleF;
            float x, y, width, height;
            if (locx - w <= 0)
            {
                x = 0;
            }
            else
            {
                x = locx - w;
            }

            if (locy - h <= 0)
            {
                y = 0;
            }
            else
            {
                y = locy - h;
            }

            if (x + w * 2 >= IR_IMAGE_WIDTH)
            {
                width = IR_IMAGE_WIDTH - x - 1;
            }
            else
            {
                width = w * 2;
            }
            if (y + h * 2 >= IR_IMAGE_HEIGHT)
            {
                height = IR_IMAGE_HEIGHT - y - 1;
            }
            else
            {
                height = h * 2;
            }
            rectangleF = new RectangleF(x, y, width, height);

            return rectangleF;
        }
        /// <summary>
        /// 获取矩形区域温度最大值及位置、最小值及位置、平均值
        /// </summary>
        /// <param name="tempData"></param>
        /// <param name="rectF"></param>
        /// <param name="irWidth"></param>
        /// <returns>result[0]-最大值，result[1]-最小值 result[2]-平均值 result[3]-最大值x坐标  result[4]-最大值y坐标 result[5]-最小值x坐标 result[6]-最小值y坐标</returns>
        public float[] getTempAtRect(float[] tempData, RectangleF rectF, int irWidth)
        {
            float sum = 0.0F;
            float[] result = new float[7];
            float startX = rectF.Left < rectF.Right ? rectF.Left : rectF.Right;
            float startY = rectF.Top < rectF.Bottom ? rectF.Top : rectF.Bottom;
            float endX = rectF.Left < rectF.Right ? rectF.Right : rectF.Left;
            float endY = rectF.Top < rectF.Bottom ? rectF.Bottom : rectF.Top;
            result[0] = result[1] = tempData[(int)(startX + startY * (float)irWidth)];
            result[3] = result[5] = startX;
            result[4] = result[6] = startY;
            int iCount = 0;

            for (int j = (int)startY; (float)j < endY; ++j)
            {
                for (int i = (int)startX; (float)i < endX; ++i)
                {
                    ++iCount;
                    int index = (int)((double)i + (double)j * (double)irWidth * 1.0D);
                    if (tempData[index] > result[0])
                    {
                        result[0] = tempData[index];
                        result[3] = (float)i;
                        result[4] = (float)j;
                    }

                    if (tempData[index] < result[1])
                    {
                        result[1] = tempData[index];
                        result[5] = (float)i;
                        result[6] = (float)j;
                    }

                    sum += tempData[index];
                }
            }

            if (iCount != 0)
            {
                result[2] = sum / (float)iCount;
            }

            return result;
        }


        private void deleteAllDrawBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(deleteAllDrawBtn, "delete.png");
        }

        private void deleteAllDrawBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(deleteAllDrawBtn, "delete_pressed.png");
        }

        private void deleteAllDrawBtn_Click(object sender, EventArgs e)
        {
            SetButtonImg(deleteAllDrawBtn, "delete.png");
            //SetButtonImg(drawCircleBtn, "circle.png");
            //SetButtonImg(drawRectBtn, "square.png");
            //SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            tempRuleInfos.Clear();
        }

        private void drawCircleBtn_MouseLeave(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_CIRCLE)
            {
                SetButtonImg(drawCircleBtn, "circle.png");
            }
        }

        private void drawCircleBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(drawCircleBtn, "circlePressed.png");
        }

        private void drawCircleBtn_Click(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_CIRCLE)
            {
                selectType = (int)DrawMode.DRAW_CIRCLE;
                SetButtonImg(drawCircleBtn, "circlePressed.png");
                SetButtonImg(drawRectBtn, "square.png");
                SetButtonImg(mouseFollowBtn, "鼠标跟随.png");
            }
            else
            {
                selectType = -1;
                SetButtonImg(drawCircleBtn, "circle.png");
            }
        }

        private void drawRectBtn_MouseLeave(object sender, EventArgs e)
        {
            if (selectType != (int)DrawMode.DRAW_AREA)
            {
                SetButtonImg(drawRectBtn, "square.png");
            }

        }

        private void drawRectBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(drawRectBtn, "square_pressed.png");

        }

        private void drawRectBtn_Click(object sender, EventArgs e)
        {

        }

        private void ThreadAlert()
        {

        }


        private void takePicBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(takePicBtn, "抓图.png");
        }

        private void takePicBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(takePicBtn, "抓图3.png");
        }


        private void takePicBtn_Click(object sender, EventArgs e)
        {
            string filePath = "1.xml";
            IndexListInfo indexList = new IndexListInfo();
            Globals.ReadInfoXml<IndexListInfo>(ref indexList, filePath);


            //IndexListInfo indexList = new IndexListInfo();
            //TrainIndex trainIndex = new TrainIndex();
            //trainIndex.alarmTemperatrue = "否";
            //trainIndex.detectTime = "2024-1-2";
            //trainIndex.IndexID = 1;
            //indexList.trainIndexList.Add(trainIndex);
            //indexList.trainIndexList.Add(trainIndex);
            //Globals.WriteInfoXml<IndexListInfo>(indexList, filePath);



            //var capture = new VideoCapture("20240715_094710_390.mp4");

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

            ////FileInfo vedioFileInfo = new FileInfo(Application.StartupPath + "\\" + "20240715_094710_390.mp4");
            ////string vedioFileName = vedioFileInfo.Name;
            //string vedioFileNameWithoutExtension = Path.GetFileNameWithoutExtension(Application.StartupPath + "\\" + "20240715_094710_390.mp4");
            //string vedioHour = vedioFileNameWithoutExtension.Substring(9, 2);
            //string vedioMin = vedioFileNameWithoutExtension.Substring(11, 2);
            //string vedioSec = vedioFileNameWithoutExtension.Substring(13, 2);
            //string vedioMillsec = vedioFileNameWithoutExtension.Substring(16, 3);

            //string IRImageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(Application.StartupPath + "\\" + "20240715_094711_654_IR_4.jpeg");
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
            //            // 这里可以对帧frame进行处理
            //            // 例如保存帧到文件
            //            string outputPath = $"frame_{i}.png";
            //            Cv2.ImWrite(outputPath, frame);
            //        }
            //    }
            //}

            //int a = 1;


            //CHCNetSDK.NET_DVR_JPEGPICTURE_WITH_APPENDDATA struJpegWithAppendData = new CHCNetSDK.NET_DVR_JPEGPICTURE_WITH_APPENDDATA();
            //IntPtr ptr1 = Marshal.AllocHGlobal(100 * 1024);
            //IntPtr ptr2 = Marshal.AllocHGlobal(2 * 1024 * 1024);
            //IntPtr ptr3 = Marshal.AllocHGlobal(4 * 1024 * 1024);
            //struJpegWithAppendData.pJpegPicBuff = ptr1;
            //struJpegWithAppendData.pP2PDataBuff = ptr2;
            //struJpegWithAppendData.pVisiblePicBuff = ptr3;

            //if (mUserIDs[0] >= 0)
            //{
            //    try
            //    {
            //        bool res = CHCNetSDK.NET_DVR_CaptureJPEGPicture_WithAppendData(mUserIDs[0], 2, ref struJpegWithAppendData);

            //        if (res != true)
            //        {
            //            iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //            str = "抓图失败，错误号：" + iLastErr; //登录失败，输出错误号
            //            MessageBox.Show(str);
            //            return;
            //        }

            //        //byte[] byteArray = new byte[] { 0x41, 0xE5, 0x49, 0x58 }; // 示例字节数组
            //        //float floatValue = BitConverter.ToSingle(byteArray, 0);

            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("抓图失败" + ex.ToString());
            //    }


            //    CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA();
            //    lpJpegPara.wPicQuality = 0; //图像质量 Image quality
            //    lpJpegPara.wPicSize = 0xff; //抓图分辨率 Picture size: 0xff-Auto(使用当前码流分辨率) 
            //                                //抓图分辨率需要设备支持，更多取值请参考SDK文档

            //    //JPEG抓图保存成文件 Capture a JPEG picture
            //    string sJpegPicFileName;
            //    sJpegPicFileName = "filetest.jpg";//图片保存路径和文件名 the path and file name to save

            //    //JEPG抓图，数据保存在缓冲区中 Capture a JPEG picture and save in the buffer
            //    uint iBuffSize = 1024 * 1024; //缓冲区大小需要不小于一张图片数据的大小 The buffer size should not be less than the picture size
            //    byte[] byJpegPicBuffer = new byte[iBuffSize];
            //    uint dwSizeReturned = 0;

            //    if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture_NEW(mUserIDs[0], 1, ref lpJpegPara, byJpegPicBuffer, iBuffSize, ref dwSizeReturned))
            //    {
            //        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //        str = "NET_DVR_CaptureJPEGPicture_NEW failed, error code= " + iLastErr;
            //        MessageBox.Show(str);
            //        return;
            //    }
            //    else
            //    {

            //        //将缓冲区里的JPEG图片数据写入文件 save the data into a file
            //        string str = "buffertest.jpg";
            //        int iLen = (int)dwSizeReturned;
            //        using (FileStream fs = new FileStream(str, FileMode.OpenOrCreate))
            //        {
            //            fs.Write(byJpegPicBuffer, 0, iLen);
            //        }
            //        str = "NET_DVR_CaptureJPEGPicture_NEW succ and save the data in buffer to 'buffertest.jpg'.";
            //        MessageBox.Show(str);
            //    }

            //    WriteIntPtrToFile("pointerData.jpeg", struJpegWithAppendData.pJpegPicBuff, struJpegWithAppendData.dwJpegPicLen);
            //    WriteIntPtrToFile("temp.dat", struJpegWithAppendData.pP2PDataBuff, struJpegWithAppendData.dwP2PDataLen);
            //}


            //float[] tempData = new float[640 * 512];
            //int i = 0;
            //using (FileStream fs = new FileStream("temp.dat", FileMode.OpenOrCreate))
            //{
            //    using (BinaryReader br = new BinaryReader(fs))
            //    {
            //        while (fs.Position < fs.Length)
            //        {
            //            float buffer = br.ReadSingle(); // 读取16个字节（四个四节）

            //            tempData[i] = buffer;
            //            i++;
            //            // 处理buffer中的数据
            //            // ...
            //        }
            //    }
            //}


            //float[] temp = new float[IR_IMAGE_WIDTH * IR_IMAGE_HEIGHT];
            //temp = TempBytesToTempFloats(IntPtrToByteArray(struJpegWithAppendData.pP2PDataBuff, (int)struJpegWithAppendData.dwP2PDataLen), IR_IMAGE_WIDTH, IR_IMAGE_HEIGHT);


            //List<float> list = temp.ToList();
            //float max = list.Max();

            //Marshal.FreeHGlobal(ptr1);
            //Marshal.FreeHGlobal(ptr2);
            //Marshal.FreeHGlobal(ptr3);
            //Console.WriteLine("aaa");

        }




        private void WriteIntPtrToFile(string filePath, IntPtr intPtr, uint length)
        {

            byte[] array = new byte[length]; // 创建一个int类型的数组，arrayLength为数组长度

            unsafe
            {
                byte* ptr = (byte*)intPtr.ToPointer(); // 将IntPtr转换为int类型的指针

                for (int i = 0; i < length; i++)
                {
                    array[i] = *(ptr + i); // 通过指针运算将数据复制到数组中
                }
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    // 写入指针数据
                    writer.Write(array);
                }

            }
        }

        /// <summary>
        /// 保存字节数组到文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="bytes"></param>
        /// <param name="length"></param>
        private void WriteBytesToFile(string filePath, byte[] bytes, int length)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate))
            {
                using (BinaryWriter writer = new BinaryWriter(fileStream))
                {
                    // 写入指针数据
                    writer.Write(bytes);
                }

            }
        }

        protected override void OnResize(EventArgs e)
        {
            if (WindowState == FormWindowState.Maximized)
            {
                //登录光学相机
                //LoginOpDevice(0, Globals.systemParam.op_ip_1, Globals.systemParam.op_username_1, Globals.systemParam.op_psw_1, Globals.systemParam.op_port_1, cbLoginCallBack);

                //Thread.Sleep(100);
                //采集预览光学图像
                //PreviewOpDevice(0, RealDataCallBack);
            }
            else if (WindowState == FormWindowState.Minimized)
            {

                //for (int i = 0; i < Globals.systemParam.deviceCount; i++)
                //{
                //    //isShowIRImageFlags[i] = false;//设置显示红外图像标志
                //    //socketReceiveFlags[i] = false;                

                //    //如果正在预览光学图像，停止预览，并设置mRealHandles为-1
                //    if (mRealHandles[i] >= 0)
                //    {
                //        CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i]);
                //        mRealHandles[i] = -1;
                //    }

                //    //CHCNetSDK.NET_DVR_Cleanup();
                //}
                //StopPrewview();
            }
        }


        /// <summary>
        ///  鼠标跟随按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_MouseLeave(object sender, EventArgs e)
        {


        }

        /// <summary>
        /// 鼠标跟随按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(mouseFollowBtn, "鼠标跟随1.png");
        }

        /// <summary>
        /// 鼠标跟随按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mouseFollowBtn_Click(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// 停止录制视频按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopRecordBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(stopRecordBtn, "停止录制.png");
        }

        /// <summary>
        ///  鼠标跟随按钮停止录制视频按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stopRecordBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(stopRecordBtn, "停止录制1.png");
        }


        /// <summary>
        /// 停止录制视频按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopRecordBtn_Click(object sender, EventArgs e)
        {
            isTrainStart = false;
            SetButtonImg(startRecordBtn, "开始录制-line.png");
            Thread.Sleep(100);
            ////停止录像 Stop recording
            //if (!CHCNetSDK.NET_DVR_StopSaveRealData(mRealHandles[0]))
            //{
            //    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
            //    str = "NET_DVR_StopSaveRealData failed, error code= " + iLastErr;
            //    MessageBox.Show(str);
            //    return;
            //}
            //else
            //{
            //    str = "NET_DVR_StopSaveRealData succ and the saved file is " + sVideoFileName;
            //    MessageBox.Show(str);
            //    m_bRecord = false;
            //}

            while (true)
            {
                //等待过车数据缓存完成后开始进行数据分析及存盘
                if (!isSavingIrImg)
                {

                    AnalysisData();

                    for (int i = 0; i < 20; i++)
                    {
                        cacheData[i].Clear();
                    }

                    cacheDataCount = 0;
                    isCopyOpImage[0] = false;
                    break;
                }
                Thread.Sleep(5);
            }

        }

        /// <summary>
        ///  开始录制视频按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_MouseLeave(object sender, EventArgs e)
        {
            if (!isTrainStart)
            {
                SetButtonImg(startRecordBtn, "开始录制-line.png");
            }
        }

        /// <summary>
        ///  开始录制视频鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(startRecordBtn, "开始录制-line(1).png");
        }


        private string ConnectSocket(IPEndPoint ipep, Socket sock)
        {
            string exmessage = "";
            try
            {
                sock.Connect(ipep);
            }
            catch (System.Exception ex)
            {
                exmessage = ex.Message;
            }
            finally
            {
            }

            return exmessage;
        }


        private void Receive(object deviceNum)
        {

        }

        /// <summary>
        /// 基于TCP协议的原始温度数据
        /// </summary>
        /// <param name="deviceNum"></param>
        private void ConnectSocketToReceiveTemp(int deviceNum)
        {


        }

        private void ByteToInt16(Byte[] arrByte, int nByteCount, ref Int16[] destInt16Arr)
        {
            //按两个字节⼀个整数解析，前⼀字节当做整数⾼位，后⼀字节当做整数低位
            //for (int i = 0; i < nByteCount / 2; i++)
            //{
            //    destInt16Arr[i] = Convert.ToInt16(arrByte[2 * i + 0] << 8 + arrByte[2 * i + 1]);
            //}
            int i = 0;
            try
            {
                //按两个字节一个整数解析，前一字节当做整数低位，后一字节当做整数高位，调用系统函数转化
                for (i = 0; i < nByteCount / 2; i++)
                {
                    Byte[] tmpBytes = new Byte[2] { arrByte[2 * i + 0], arrByte[2 * i + 1] };
                    destInt16Arr[i] = BitConverter.ToInt16(tmpBytes, 0);
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("Byte to Int16转化错误！i=" + e.Message + i.ToString());
            }

        }



        /// <summary>
        /// 开始录制视频按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_Click(object sender, EventArgs e)
        {
            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();

            //获取当前时间戳
            long dateTimeNow = TicksTimeConvert.GetNowTicks13();
            DateTime aa = TicksTimeConvert.Ticks132LocalTime(dateTimeNow);  //时间戳转本地时间


            saveReportPath[0] = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + aa.ToString("yyyy_MM_dd");
            alarmReportPath[0] = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + aa.ToString("yyyy_MM_dd");
            string strTime = aa.ToString("yyyyMMdd_HHmmss_fff");
            saveReportPath[0] += "\\" + strTime;

            alarmReportPath[0] += "\\" + strTime;

            //判断文件夹是否存在，如果不存在，新建文件夹
            if (!Directory.Exists(saveReportPath[0]))
            {
                Directory.CreateDirectory(saveReportPath[0]);
            }

            isTrainStart = true;
            SetButtonImg(startRecordBtn, "开始录制-line(1).png");

        }

        /// <summary>
        /// 停止采集按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopPrewviewBtn_MouseLeave(object sender, EventArgs e)
        {
            SetButtonImg(stopPrewviewBtn, "stop.png");
        }

        /// <summary>
        /// 停止采集按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopPrewviewBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(stopPrewviewBtn, "stopPressed.png");
        }



        /// <summary>
        /// 开始采集按钮鼠标在控件内事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPrewviewBtn_MouseHover(object sender, EventArgs e)
        {
            SetButtonImg(startPrewviewBtn, "开始(1).png");
        }

        /// <summary>
        /// 开始采集按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPrewviewBtn_MouseLeave(object sender, EventArgs e)
        {

        }


        /// <summary>
        /// 开始采集按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartPrewviewBtn_Click(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 停止采集按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopPrewviewBtn_Click(object sender, EventArgs e)
        {

        }



        private void StartPrewview()
        {


        }

        private void GetTmp()
        {


        }

        /// <summary>
        /// 停止采集预览
        /// </summary>
        private void StopPrewview()
        {

        }

        private string saveFilePath()
        {
            string filePath = "";
            // 创建保存对话框
            SaveFileDialog saveDataSend = new SaveFileDialog();
            saveDataSend.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);   // 获取文件路径
            if (saveDataSend.ShowDialog() == DialogResult.OK)   // 显示文件框，并且选择文件
            {
                filePath = saveDataSend.FileName.Replace("\\", "/");   // 获取文件名
            }
            return filePath;

        }

        /// <summary>
        /// 设置按钮图片
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="imageName"></param>
        private void SetButtonImg(UISymbolButton btn, string imageName)
        {
            btn.Image = Image.FromFile(Globals.startPathInfo.FullName + "\\Resources\\" + imageName);
        }

        /// <summary>
        /// 设备校时
        /// </summary>
        private void Timing()
        {

        }


        private void ShowIRImageThreadProc(object deviceNum)
        {

        }

        private void PreviewOpDevice(int deviceNum, CHCNetSDK.REALDATACALLBACK realCallback)
        {



        }

        int count;

        /// <summary>
        /// 设备0可见光实时预览数据回调
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack_OP_0(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            switch (dwDataType)
            {
                //case CHCNetSDK.NET_DVR_STREAMDATA:     // video stream datacount
                //    count++;
                //    Console.WriteLine("可见光回调函数：" + count + "时间：" + System.DateTime.Now);

                //    if (count > 2147483646)
                //    {
                //        count = 0;
                //    }
                //    break;
            }


        }

        /// <summary>
        /// 设备0红外实时预览数据回调
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack_IR_0(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            // Console.WriteLine("回调函数0：" + System.DateTime.Now);
        }


        /// <summary>
        /// 登录回调函数
        /// </summary>
        /// <param name="lUserID"></param>
        /// <param name="dwResult"></param>
        /// <param name="lpDeviceInfo"></param>
        /// <param name="pUser"></param>
        public void cbLoginCallBack_0(int lUserID, int dwResult, IntPtr lpDeviceInfo, IntPtr pUser)
        {
            //string strLoginCallBack = "登录设备，lUserID：" + lUserID + "，dwResult：" + dwResult;

            //if (dwResult == 0)
            //{
            //    uint iErrCode = CHCNetSDK.NET_DVR_GetLastError();
            //    strLoginCallBack = strLoginCallBack + "，错误号:" + iErrCode;
            //}

            ////下面代码注释掉也会崩溃
            //if (InvokeRequired)
            //{
            //    object[] paras = new object[2];
            //    paras[0] = strLoginCallBack;
            //    paras[1] = lpDeviceInfo;
            //    labelLogin.BeginInvoke(new UpdateTextStatusCallback(UpdateClientList), paras);
            //}
            //else
            //{
            //    //创建该控件的主线程直接更新信息列表 
            //    UpdateClientList(strLoginCallBack, lpDeviceInfo);
            //}

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            pictureBox3.Left = closePictureBox2.Left - pictureBox3.Width - 2;

            trainListView.View = View.Details;
            if (trainListView.Columns.Count == 0)
            {
                ColumnHeader columnHeader0 = new ColumnHeader();
                columnHeader0.Text = "序号";
                columnHeader0.Width = 0;
                columnHeader0.TextAlign = HorizontalAlignment.Center;
                trainListView.Columns.Add(columnHeader0);

                ColumnHeader columnHeader = new ColumnHeader();
                columnHeader.Text = "序号";
                columnHeader.Width = trainListView.Width * 1 / 5;
                columnHeader.TextAlign = HorizontalAlignment.Center;
                trainListView.Columns.Add(columnHeader);

                ColumnHeader columnHeader1 = new ColumnHeader();
                columnHeader1.Text = "过车时间";
                columnHeader1.Width = trainListView.Width * 2 / 5;
                columnHeader1.TextAlign = HorizontalAlignment.Center;
                trainListView.Columns.Add(columnHeader1);


                ColumnHeader columnHeader2 = new ColumnHeader();
                columnHeader2.Text = "是否报警";
                columnHeader2.Width = trainListView.Width * 2 / 5;
                columnHeader2.TextAlign = HorizontalAlignment.Center;
                trainListView.Columns.Add(columnHeader2);
                trainListView.FullRowSelect = true;
                trainListView.Scrollable = true;
            }

            //listView_trainInfo.Top = uiPanel3.Bottom - 10;
            //listView_trainInfo.Left = uiPanel3.Left;
            //trainListView.Height = 100;


            UISplitContainer uISplitContainer = (UISplitContainer)fmonitor.GetControl("uiSplitContainer1");
            uISplitContainer.SplitterDistance = uISplitContainer.Height / 40;

            string[] subdirectoryEntries;
            string trainIndexDirectoryPath = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0;
            DirectoryInfo trainIndexDirectoryInfo = new DirectoryInfo(trainIndexDirectoryPath);
            if (trainIndexDirectoryInfo.Exists)
            {
                subdirectoryEntries = Directory.GetFiles(trainIndexDirectoryPath, "*.xml", SearchOption.TopDirectoryOnly);
                if (subdirectoryEntries.Length > 0)
                {
                    foreach (string path in subdirectoryEntries)
                    {
                        string[] time = Path.GetFileNameWithoutExtension(path).Split('_');
                        int year = Convert.ToInt32(time[0]);//年
                        int month = Convert.ToInt32(time[1]);//年
                        int day = Convert.ToInt32(time[2]);//年

                        if (year == DateTime.Now.Year && month == DateTime.Now.Month && day == DateTime.Now.Day)
                        {

                            Globals.ReadInfoXml<IndexListInfo>(ref indexList, path);

                            trainListView.Items.Clear();

                            for (int i = 0; i < indexList.trainIndexList.Count; i++)
                            {
                                ListViewItem item = new ListViewItem();

                                //item.SubItems.Add("");
                                item.SubItems.Add(indexList.trainIndexList[i].IndexID.ToString());


                                //item.UseItemStyleForSubItems = false;
                                //trainListView.Columns[1].TextAlign = HorizontalAlignment.Center;
                                //trainListView.Columns[2].TextAlign = HorizontalAlignment.Center;
                                //trainListView.Columns[3].TextAlign = HorizontalAlignment.Center;

                                item.SubItems.Add(indexList.trainIndexList[i].detectTime.ToString());
                                item.SubItems.Add(indexList.trainIndexList[i].isAlarm.ToString());
                                if (indexList.trainIndexList[i].isAlarm.ToString().Equals("是"))
                                {
                                    item.ForeColor = Color.Red;
                                }
                                trainListView.Items.Add(item);
                            }
                            trainIndexCount = indexList.trainIndexList.Count;
                            break;
                        }
                    }
                }
                else
                {
                    indexList = new IndexListInfo();
                }
            }

        }

        private void Timer1_Tick(object sender, EventArgs e)
        {

        }

        private void Timer3_Tick(object sender, EventArgs e)
        {


        }

        private void SaveOpImage(int deviceNum, string rootPath, int /*userID*/handle, int channel)
        {

        }


        private void ClosePictureBox2_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 0; i < Globals.systemParam.deviceCount; i++)
                {

                    if (mRealHandles[i] >= 0)
                    {
                        CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i]);
                        mRealHandles[i] = -1;
                        RealDatas[i] = null;
                    }

                    if (Globals.systemParam.deviceCount > 1)
                    {
                        CHCNetSDK.NET_DVR_StopRealPlay(mRealHandles[i + 2]);
                        mRealHandles[i + 2] = -1;
                        RealDatas[i + 2] = null;
                    }


                    if (mUserIDs[i] >= 0)
                    {
                        CHCNetSDK.NET_DVR_Logout(mUserIDs[i]);
                        mUserIDs[i] = -1;

                    }
                }

                CHCNetSDK.NET_DVR_Cleanup();

                Globals.fileInfos = null;
            }
            catch (Exception ex)
            {
                Globals.Log("关闭窗口" + ex.ToString());
            }

            this.Close();
            Application.Exit();
            System.Environment.Exit(0);
        }

        private void PictureBox3_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;

        }

        /// <summary>
        /// 获取红外图像文件路径
        /// </summary>
        /// <param name="rootPath">根路径</param>
        /// <param name="deviceNum">设备号，从0开始</param>
        /// <returns></returns>
        private string GetIrImageFilePath(string rootPath, int deviceNum, string name)
        {
            string imagePath = rootPath + deviceNum;

            //判断文件夹是否存在，如果不存在，新建文件夹
            if (!Directory.Exists(imagePath))
            {
                Directory.CreateDirectory(imagePath);
            }

            string strTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            string IrImagePath = imagePath + "\\" + strTime + name;

            return IrImagePath;
        }


        private void DrawCrossLine(Graphics g, float startX, float startY, Pen pen, int lineLength)
        {
            g.DrawLine(pen, startX, startY, startX + lineLength, startY);
            g.DrawLine(pen, startX, startY, startX - lineLength, startY);
            g.DrawLine(pen, startX, startY, startX, startY + lineLength);
            g.DrawLine(pen, startX, startY, startX, startY - lineLength);
        }

        private void UiNavBar1_MenuItemClick(string itemText, int menuIndex, int pageIndex)
        {



        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private void initDatas()
        {
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();//初始化SDK

            if (m_bInitSDK == false)
            {
                MessageBox.Show("SDK初始化失败!");
                return;
            }
            else
            {
                //保存SDK日志
                CHCNetSDK.NET_DVR_SetLogToFile(3, Globals.SDKLogPath, true);
            }

            int deviceCount = Globals.systemParam.deviceCount; //通过配置文件获取设备数量

            pics = new PictureBox[deviceCount * 2];//定义显示图像控件，每个设备显示可见光和红外图像。

            //初始化红外设备ip集合
            ipList.Add(Globals.systemParam.ip_0);
            ipList.Add(Globals.systemParam.ip_1);

            for (int i = 0; i < deviceCount; i++)
            {
                //初始化userId为-1
                mUserIDs.Add(-1);

                DeviceInfos.Add(new CHCNetSDK.NET_DVR_DEVICEINFO_V40());
                mRealHandles.Add(-1);
                mRealHandles.Add(-1);

                LoginCallBacks.Add(null);
                RealDatas.Add(null);
                RealDatas.Add(null);

                saveReportPath.Add(null);
                alarmReportPath.Add(null);

                isAlarm.Add(false);
                isCopyOpImage.Add(false);

            }

            for (int i = 0; i < cacheData.Length; i++)
            {
                cacheData[i] = new List<byte>();
            }

            indexList = new IndexListInfo();
        }


        /// <summary>
        /// 注册设备
        /// </summary>
        /// <param name="deviceNum">设备号 从0开始</param>
        /// <param name="ipAddress">设备ip</param>
        /// <param name="userName">用户名</param>
        /// <param name="psw">密码</param>
        /// <param name="port">端口号</param>
        /// <param name="loginCallBack">登录回调函数</param>
        private void LoginDevice(int deviceNum, string ipAddress, string userName, string psw, string port, CHCNetSDK.LOGINRESULTCALLBACK loginCallBack)
        {
            if (mUserIDs[deviceNum] < 0)//如果设备ID小于0，登录设备
            {
                struLogInfo = new CHCNetSDK.NET_DVR_USER_LOGIN_INFO();

                //设备IP地址或者域名
                byte[] byIP = System.Text.Encoding.Default.GetBytes(ipAddress);
                struLogInfo.sDeviceAddress = new byte[129];
                byIP.CopyTo(struLogInfo.sDeviceAddress, 0);

                //设备用户名
                byte[] byUserName = System.Text.Encoding.Default.GetBytes(userName);
                struLogInfo.sUserName = new byte[64];
                byUserName.CopyTo(struLogInfo.sUserName, 0);

                //设备密码
                byte[] byPassword = System.Text.Encoding.Default.GetBytes(psw);
                struLogInfo.sPassword = new byte[64];
                byPassword.CopyTo(struLogInfo.sPassword, 0);

                struLogInfo.wPort = ushort.Parse(port);//设备服务端口号

                if (LoginCallBacks[deviceNum] == null)
                {
                    LoginCallBacks[deviceNum] = new CHCNetSDK.LOGINRESULTCALLBACK(loginCallBack);//注册回调函数                    
                }
                struLogInfo.cbLoginResult = LoginCallBacks[deviceNum];
                struLogInfo.bUseAsynLogin = false; //是否异步登录：0- 否，1- 是 

                DeviceInfo = DeviceInfos[deviceNum];

                //登录设备 Login the device
                mUserIDs[deviceNum] = CHCNetSDK.NET_DVR_Login_V40(ref struLogInfo, ref DeviceInfo);

                if (mUserIDs[deviceNum] < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "登录设备" + ipList[deviceNum] + "失败！" + "错误号：" + iLastErr; //登录失败，输出错误号
                    Globals.Log(str);
                    return;
                }
                else
                {
                    //登录成功
                    //MessageBox.Show("Login Success!");
                }
            }
        }

        /// <summary>
        /// 预览图像
        /// </summary>
        /// <param name="deviceNum">设备号，从0开始</param>
        /// <param name="playWndNum">预览控件编号，从0开始</param>
        /// <param name="channelNum">通道号：1--可见光；2-红外</param>
        /// <param name="realCallback">预览回调函数</param>
        /// <param name="useCallback">是否使用回调预览</param>
        private void Prewview(int deviceNum, int playWndNum, int channelNum, CHCNetSDK.REALDATACALLBACK realCallback, bool useCallback)
        {
            if (mUserIDs[deviceNum] < 0)
            {
                Globals.Log("请先登录设备" + ipList[deviceNum]);
                return;
            }

            if (mRealHandles[playWndNum] < 0)
            {
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = pics[playWndNum].Handle;//预览窗口
                lpPreviewInfo.lChannel = channelNum;//预te览的设备通道
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 0;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = true; //0- 非阻塞取流，1- 阻塞取流
                lpPreviewInfo.dwDisplayBufNum = 6; //播放库播放缓冲区最大缓冲帧数 取值范围：1、6（默认，自适应播放模式）、15，置0时默认为1
                lpPreviewInfo.byProtoType = 0;//应用层取流协议，0-私有协议，1-RTSP协议
                lpPreviewInfo.byPreviewMode = 0;//预览模式，0-正常预览，1-延迟预览

                if (RealDatas[playWndNum] == null)
                {
                    RealDatas[playWndNum] = new CHCNetSDK.REALDATACALLBACK(realCallback);//预览实时流回调函数
                }

                IntPtr pUser = new IntPtr();//用户数据

                if (useCallback)
                {
                    //回调函数预览
                    mRealHandles[playWndNum] = CHCNetSDK.NET_DVR_RealPlay_V40(mUserIDs[deviceNum], ref lpPreviewInfo, RealDatas[playWndNum], pUser);
                }
                else
                {
                    //直接预览
                    mRealHandles[playWndNum] = CHCNetSDK.NET_DVR_RealPlay_V40(mUserIDs[deviceNum], ref lpPreviewInfo, null/*realCallback*/, pUser);
                }

                if (mRealHandles[playWndNum] < 0)
                {
                    iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                    str = "预览" + ipList[deviceNum] + "通道：" + channelNum + "失败！" + "错误号：" + iLastErr; //预览失败，输出错误号
                    Globals.Log(str);
                    return;
                }
                else
                {

                }

            }
        }


        private void TabPage1_MouseDown(object sender, MouseEventArgs e)
        {

        }

        private void Pics0_MouseLeave(object sender, EventArgs e)
        {
            //mouseFollowFlag = false;
            isInPic = false;
        }

        private void UiButton1_Click(object sender, EventArgs e)
        {
            using (System.Diagnostics.Process process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = Application.StartupPath + "\\" + "ffmpeg.exe";
                process.StartInfo.Arguments = "-i";
            }
        }

        private void Pics0_MouseUp(object sender, MouseEventArgs e)
        {

        }


        private void Pics0_MouseMove(object sender, MouseEventArgs e)
        {


        }

        /// <summary>
        /// 设置实时监控界面图像显示控件布局
        /// </summary>
        /// <param name="rowNum">行数</param>
        /// <param name="colNum">列数</param>

        private void SetFmonitorDisplayWnds(uint rowNum, uint colNum)
        {
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiPanel1").Width);
            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("uiNavMenu1").Width);

            //uint w = (uint)(this.ClientSize.Width);

            //uint h = (uint)(this.ClientSize.Height - uiNavBar1.Height - fmonitor.GetControl("uiPanel1").Height);

            //uint w = (uint)(Screen.PrimaryScreen.Bounds.Width) ;
            uint w = (uint)(Screen.PrimaryScreen.Bounds.Width - fmonitor.GetControl("listView_trainInfo").Width);
            //uint h = (uint)(Screen.PrimaryScreen.Bounds.Height - uiNavBar1.Height - fmonitor.GetControl("uiPanel1").Height - fmonitor.GetControl("listView_trainInfo").Height);
            uint h = (uint)(Screen.PrimaryScreen.Bounds.Height - uiNavBar1.Height - fmonitor.GetControl("uiPanel1").Height);

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
                uint y = (uint)fmonitor.GetControl("uiPanel1").Height + (real_height - rowNum * display_height - DISPLAYWND_GAP * (rowNum - 1)) / 2 + (display_height + DISPLAYWND_GAP) * i;
                for (uint j = 0; j < colNum; j++)
                {
                    //uint x = (uint)fmonitor.GetControl("uiNavMenu1").Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;
                    uint x = (uint)fmonitor.GetControl("listView_trainInfo").Width + (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;

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

                    fmonitor.Controls.Add(pics[i * 2 + j]);

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
                            pics[i * 2 + j].MouseClick += new MouseEventHandler(Pics0_MouseClick);
                            pics[i * 2 + j].MouseDown += new MouseEventHandler(Pics0_MouseDown);
                            pics[i * 2 + j].MouseMove += new MouseEventHandler(Pics0_MouseMove);
                            pics[i * 2 + j].MouseLeave += new EventHandler(Pics0_MouseLeave);
                            //pics[i * 2 + j].MouseHover += new EventHandler(Pics0_MouseUp);
                            pics[i * 2 + j].MouseUp += new MouseEventHandler(Pics0_MouseUp);
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

            //foreach (PictureBox p in fmonitor.GetControls<PictureBox>())
            //{
            //    Console.WriteLine(p.Name);
            //}
        }

        private void Pics0_MouseClick(object sender, MouseEventArgs e)
        {
            if (selectType != -1)
            {
                int iX = e.X * 768 / pics[0].Width;
                int iY = e.Y * 576 / pics[0].Height;

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
                        case (int)DrawMode.DRAW_POINT:
                        case (int)DrawMode.DRAW_LINE:
                        case (int)DrawMode.DRAW_AREA:
                        case (int)DrawMode.DRAW_CIRCLE:

                            if (2 == points.Count)
                            {

                                TempRuleInfo tempRuleInfo = new TempRuleInfo();
                                tempRuleInfo.type = selectType;
                                tempRuleInfo.index = rectModeIndex;
                                tempRuleInfo.startPointX = points[0].x;
                                tempRuleInfo.startPointY = points[0].y;
                                tempRuleInfo.endPointX = points[1].x;
                                tempRuleInfo.endPointY = points[1].y;

                                //tempRuleInfos[rectModeIndex] = tempRuleInfo;
                                tempRuleInfos.Add(tempRuleInfo);

                                //a8sdk.A8SDK.area_pos area_data;

                                //area_data.enable = 1;
                                //area_data.height = points[1].y- points[0].y;
                                //area_data.width = points[1].x- points[0].x;
                                //area_data.x = points[0].x;
                                //area_data.y = points[0].y;
                                //int i = a8Lists[0].Set_area_pos(0, area_data);
                                //Console.WriteLine("执行结果" + i);

                                points.Clear();

                                //int i;
                                //a8sdk.A8SDK.area_pos area_data;

                                //Console.WriteLine(" tempRuleInfos[0].startPointX" + tempRuleInfos[0].startPointX);
                                //Console.WriteLine("tempRuleInfos[0].startPointY " + tempRuleInfos[0].startPointY);
                                //Console.WriteLine("tempRuleInfos[0].endPointX" + tempRuleInfos[0].endPointX);
                                //Console.WriteLine("tempRuleInfos[0].endPointY" + tempRuleInfos[0].endPointY);


                                //int x1 = tempRuleInfos[0].startPointX / 4;
                                //int y1 = tempRuleInfos[0].startPointY / 4;

                                //int x2 = tempRuleInfos[0].endPointX / 4;
                                //int y2 = tempRuleInfos[0].endPointY / 4;

                                //Console.WriteLine("x1" + x1);
                                //Console.WriteLine("y1" + y1);
                                //Console.WriteLine("x2" + x2);
                                //Console.WriteLine("y2" + y2);

                                //area_data.enable = 1;
                                //area_data.height = y2-y1;
                                //area_data.width = x2-x1;
                                //area_data.x = x1;
                                //area_data.y = y1;
                                //i = a8Lists[0].Set_area_pos(1, area_data);


                                //rectModeIndex++;

                            }
                            break;
                    }
                }

            }
        }

        /// <summary>
        /// 获取矩形区域最大值及坐标位置
        /// </summary>
        /// <param name="realTemp"></param>
        /// <param name="X1"></param>
        /// <param name="Y1"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        /// <returns></returns>
        public int[] getTempAtRect(int[,] realTemp, int X1, int Y1, int X2, int Y2)
        {
            int[] result = new int[3];
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

        /// <summary>
        /// 获取椭圆区域温度最大值及坐标
        /// </summary>
        /// <param name="imageData"></param>
        /// <param name="ellipseCenterX"></param>
        /// <param name="ellipseCenterY"></param>
        /// <param name="ellipseRadiusX"></param>
        /// <param name="ellipseRadiusY"></param>
        /// <returns></returns>
        public int[] FindMaxValueInEllipse(int[,] imageData, int ellipseCenterX, int ellipseCenterY, int ellipseRadiusX, int ellipseRadiusY)
        {
            int[] result = new int[3];
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
                            int currentValue = imageData[x, y];
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

        // 判断点是否在椭圆内的方法
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

        private void Pics0_MouseDown(object sender, MouseEventArgs e)
        {

        }

    }
}
