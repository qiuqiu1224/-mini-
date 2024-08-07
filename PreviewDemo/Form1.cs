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
        UILabel info;
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
        Thread MonitorTrainComingThread;//监测来车线程
        Thread MonitorTrainLeaveThread;//监测过车线程
        Thread MonitorDeviceThread;//登录和预览设备线程
        Thread MonitorDeviceOnlineThread;//监测设备是否在线线程

        byte cacheDataCount_0 = 0;//红外热成像缓存数据数量
        byte cacheDataCount_1 = 0;//红外热成像缓存数据数量

        List<byte>[] cacheData_0 = new List<byte>[20];//红外图像和温度数据缓存集合
        List<byte>[] cacheData_1 = new List<byte>[20];//红外图像和温度数据缓存集合

        private List<IntPtr> m_ptrRealHandles = new List<IntPtr>();
        string savePath;
        bool isSavingIrImg_0, isSavingIrImg_1;//是否正在缓存红外图像和温度数据标志
        //bool isCopyOpImage;
        //bool isAlarm = false;
        List<bool> isAlarm = new List<bool>();
        List<bool> isCopyOpImage = new List<bool>();

        List<string> saveReportPath = new List<string>();//保存过车数据根目录集合
        List<string> alarmReportPath = new List<string>();//保存报警数据根目录集合
        List<string> trainInfoReportPath = new List<string>();//保存车辆信息根目录集合

        IndexListInfo indexList;
        int trainIndexCount = 0;
        bool isDeviceDetected = false;

        public CHCNetSDK.NET_DVR_DEVICECFG_V40 m_struDeviceCfg;

        #endregion


        #region 过车检测
        [System.Runtime.InteropServices.DllImport("KERNEL32")]
        private static extern bool QueryPerformanceFrequency(ref ulong lpFrequency);
        [System.Runtime.InteropServices.DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(ref ulong lpCounter);
        ushort m_wSeniorN1;     //开机次数
        ushort m_wSensorN2;     //开门次数
        ushort m_wSensorN3;     //关门次数
        ushort m_wSensorN4;     //关门次数
        ushort[] m_WheelSeniorCount = new ushort[4];
        UInt64 m_lgT2_1, m_lgT2_2, m_lgTemp2; //first,second, current
        UInt64 m_lgT3_1, m_lgT3_2, m_lgTemp3;
        UInt64 m_lgT2_To_3;    // #2 #3 time 270
        UInt64 m_lgTCPU;

        internal AutoResetEvent readN1Event = new AutoResetEvent(false);
        internal AutoResetEvent readN2Event = new AutoResetEvent(false);
        internal AutoResetEvent readN3Event = new AutoResetEvent(false);

        internal AutoResetEvent readN4Event = new AutoResetEvent(false);
        internal AutoResetEvent trainComingEvent = new AutoResetEvent(false);//来车，开始接车
        internal AutoResetEvent trainLeaveEvent = new AutoResetEvent(false);  //列车已过，停止接车

        internal ManualResetEvent killEvent = new ManualResetEvent(false);
        internal ManualResetEvent waitEvent = new ManualResetEvent(false);//test


        CarTableDeal mCarTableDel = new CarTableDeal();
        UInt64[] axleTimeTable = new UInt64[1024];//轴的时间戳表
        ushort[] axleSpeedTable = new ushort[1024];//轴的速度表
        ushort m_AxleCount = 0;
        ushort axle_Distance;

        ushort m_CarSum;
        ushort m_AxleSum;

        internal WaitHandle[] waitHandle = new WaitHandle[5];


        Thread dealTrainThread;

        byte[] data_buffer = new byte[0x6000];
        int data_count = 0;
        int deal_point = 0;
        bool kjFlag = false;
        #endregion

        private void DealTrain()
        {
            waitHandle[0] = readN1Event;
            waitHandle[1] = readN2Event;
            waitHandle[2] = readN3Event;
            waitHandle[3] = readN4Event;
            waitHandle[4] = killEvent;
            int m_WaitTrainPassedTime = 15000;//15s

            ushort m_speedTemp = 240;  // default : 80km/h , adbout: 240 dm/s

            for (int i = 0; i < 4; i++) m_WheelSeniorCount[i] = 0;


            while (serialPort1.IsOpen == true)
            {
                int k = WaitHandle.WaitAny(waitHandle, m_WaitTrainPassedTime, false);
                switch (k)
                {
                    case 0:
                        m_WaitTrainPassedTime = 15000;
                        m_WheelSeniorCount[0]++;
                        if (kjFlag) break;
                        if ((m_WheelSeniorCount[0] == 5) && ((m_WheelSeniorCount[1] + m_WheelSeniorCount[2]) < m_WheelSeniorCount[0]))
                        {
                            kjFlag = true;
                            m_WheelSeniorCount[1] = 0;
                            m_WheelSeniorCount[2] = 0;
                            m_AxleCount = 0;
                            m_lgT2_1 = 0;
                            m_lgT2_2 = 0;
                            m_lgT3_1 = 0;
                            m_lgT3_2 = 0;
                            m_lgTemp2 = 0;
                            m_lgTemp3 = 0;
                            m_lgT2_To_3 = 20;
                            //textBox1.Text = "";
                            trainComingEvent.Set();  //发出来车指令，开始处理过车图像
                        }
                        break;
                    case 1:
                        m_WaitTrainPassedTime = 15000;
                        if (!kjFlag) break;
                        m_WheelSeniorCount[1]++;
                        if (m_WheelSeniorCount[1] == 1) m_lgT2_1 = m_lgTemp2;
                        else m_lgT2_1 = m_lgT2_2;
                        m_lgT2_2 = m_lgTemp2;
                        break;
                    case 2:
                        m_WaitTrainPassedTime = 15000;
                        if (!kjFlag) break;
                        if ((m_lgTemp3 <= m_lgTemp2) && (m_lgTemp3 < 0x1000000)) break;
                        m_WheelSeniorCount[2]++;
                        m_AxleCount++;
                        if (m_AxleCount > 1000) m_AxleCount = 1000;

                        m_lgT2_To_3 = m_lgTemp3 - m_lgTemp2;
                        axleTimeTable[m_AxleCount - 1] = m_lgTemp3;//轴的时间戳表
                        try
                        {
                            if (m_WheelSeniorCount[2] == 1) m_lgT3_1 = m_lgTemp3;
                            else m_lgT3_1 = m_lgT3_2;
                            m_lgT3_2 = m_lgTemp3;

                            //  textBox1.Text += m_AxleCount.ToString();
                            //  textBox1.Text += " c2_1: " + m_lgT2_1.ToString() + " c2_2: " + m_lgT2_2.ToString();
                            // m_lgTemp2 = m_lgT2_2 - m_lgT2_1;
                            //  textBox1.Text += "  c2:" + m_lgTemp2.ToString();

                            //   textBox1.Text += " c3_1: " + m_lgT3_1.ToString() + " c3_2: " + m_lgT3_2.ToString();
                            //   m_lgTemp3 = m_lgT3_2 - m_lgT3_1;
                            //   textBox1.Text += "  c3: " + m_lgTemp3.ToString();

                            //textBox1.Text += "  c2-3: " + m_lgT2_To_3.ToString();

                            // m_speedTemp = (ushort)(27 * m_lgTCPU / m_lgT2_To_3 / 10);
                            // m_speedTemp = (ushort)(27 * 1000 / m_lgT2_To_3);
                            m_speedTemp = (ushort)(27 * 36000 / m_lgT2_To_3);
                            //textBox1.Text += " sp:" + m_speedTemp.ToString();
                            axleSpeedTable[m_AxleCount - 1] = m_speedTemp;//轴的速度表 单位 0.1km/h

                            if (m_WheelSeniorCount[2] > 1)
                            {
                                ushort axle_Distance1 = (ushort)((m_lgT2_2 - m_lgT2_1) * 270 / m_lgT2_To_3);//mm
                                //textBox1.Text += "  zj1:" + axle_Distance1.ToString();

                                axle_Distance = (ushort)((m_lgT3_2 - m_lgT3_1) * 270 / m_lgT2_To_3);//mm
                                //textBox1.Text += "  zj2:" + axle_Distance.ToString();
                                axle_Distance += axle_Distance1;
                                axle_Distance /= 200;//pingjun   mm to dm
                                if ((axle_Distance < 8) || (axle_Distance > 0xff)) m_AxleCount--;
                                mCarTableDel.distance[m_AxleCount - 2] = (byte)axle_Distance;
                                m_WaitTrainPassedTime = 6000 + 25 * 36 * 1000 / m_speedTemp;//6s + 25m/speed
                            }
                        }
                        catch { }
                        //textBox1.Text += "\r\n";

                        break;
                    case 3:
                        m_WaitTrainPassedTime = 15000;
                        break;
                    case 4:
                        m_WaitTrainPassedTime = 15000;
                        break;
                    case WaitHandle.WaitTimeout:
                        m_WaitTrainPassedTime = 20000;
                        if (kjFlag)
                        {
                            m_wSeniorN1 = m_WheelSeniorCount[0];     //开机次数
                            m_wSensorN2 = m_WheelSeniorCount[1];     //开门次数
                            m_wSensorN3 = m_WheelSeniorCount[2];     //关门次数
                            m_wSensorN4 = m_WheelSeniorCount[3];     //关门次数
                            mCarTableDel.distance[m_AxleCount - 1] = 0xff;
                            m_AxleSum = m_AxleCount;
                            if (m_AxleSum > 5) CreateCarTable();
                            //textBox1.Text += "AxleSum:" + m_AxleSum.ToString() + "  ";
                            //textBox1.Text += "CarSum:" + m_CarSum.ToString() + "\r\n";

                            trainLeaveEvent.Set();
                            Thread.Sleep(1000);
                        }
                        data_count = 0; deal_point = 0;
                        kjFlag = false;
                        m_AxleCount = 0;
                        m_lgT2_1 = 0;
                        m_AxleCount = 0;
                        m_lgT2_1 = 0;
                        m_lgT2_2 = 0;
                        m_lgT3_1 = 0;
                        m_lgT3_2 = 0;
                        m_lgTemp2 = 0;
                        m_lgTemp3 = 0;
                        m_lgT2_To_3 = m_lgTCPU / 100;

                        for (int i = 0; i < 4; i++) m_WheelSeniorCount[i] = 0;
                        Thread.Sleep(5000);
                        break;

                }
            }
        }

        public class cartabtag
        {
            public byte CarType;
            public byte BearTypeAxle;
            public ushort AxleAddr;
            public byte[] distance;
            public byte count;
            public cartabtag()
            {
                distance = new byte[32];
            }
        }

        private int CreateCarTable()
        {
            m_CarSum = 0;
            mCarTableDel.make_car_table();
            m_CarSum = mCarTableDel.carsum;

            return m_CarSum;
        }

        public class CarTableDeal
        {
            public byte[] distance = new byte[1024];
            public cartabtag[] CTPtr = new cartabtag[256];
            public byte[] Axle_distance_table = new byte[2048];
            public ushort carsum1, matchflag, checksum, conum, carsum, specialcount, missaxle;//, aLl_car_number, tempcarsum, tempnomatchaxle;

            public CarTableDeal()
            {
                int i;
                for (i = 0; i < CTPtr.GetLength(0); i++) CTPtr[i] = new cartabtag();

                int[] Axle_distance_table_temp = {
        3,   27,33,  80,86,  27,33, 0x80,	//中华之星机车
		3,   22,28, 153,158, 22,28, 0x80,	//秦沈客车
		11,  12,18, 12,18, 14,20, 12,18, 12,18, 49,141, 12,18, 12,18, 14,20, 12,18, 12,18, 0xe0,  //	;多轴货车
		19,  10,18, 10,18, 10,18, 10,18, 15,64, 10,18, 10,18, 10,18, 10,18, 21,64, 10,18, 10,18, 10,18, 10,18, 15,64, 10,18, 10,18, 10,18, 10,18, 0xe6,		//  ;双联平车D30G 钳夹车D30A中中挡27的 需要增加如下---- 2007-05-14   20170810  移至10轴车前
		9,   10,16,  9,15,  9,15, 10,16, 63,141, 10,16,  9,15,  9,15, 10,16, 0xe1,					//	;多轴货车
		9,   11,17, 11,17, 11,17, 11,17, 42,141, 11,17, 11,17, 11,17, 11,17, 0xe2,				//	;多轴货车

		9,   14,20, 19,25, 14,20, 24,38, 23,29, 11,17, 11,17, 11,17, 21,27, 0x1,				//	;韶山
		9,   21,27, 11,17, 11,17, 11,17, 23,29, 24,38, 14,20, 19,25, 14,20, 0x1,				//	;（上车韶山 0x1反向）
	
		9,   22,27, 21,25, 16,20, 31,36, 25,29, 12,17, 12,16, 12,16, 22,27, 0x2,	//  ;SY    //add:2009/03/02  cm
		9,   22,27, 12,16, 12,16, 12,17, 25,29, 31,36, 16,20, 21,25, 22,27, 0x2,	//  ;（上车SY 0x2反向）

		7,   23,30, 55,80, 23,30, 53,67, 23,30, 55,80, 23,30, 0x5,			//  ;大秦线西门子机车  //edit: 25 ->23, 68 -> 80 2009/12/21  cm
		7,   26,30, 32,36, 26,30, 56,61, 26,30, 32,36, 26,30, 0x3,			//  ;DJ1   //add:2009/03/02  cm
		7,   26,34, 48,59, 26,34, 48,62, 26,34, 48,59, 26,34, 0x4,			//	;韶山  //edit: 0x3b -> 0x3E 2009/03/02  cm

		5,   16,19, 16,19, 55,100, 16,19, 16,19, 0x10,		//	;GKD2/GKD3B/DF4/DF4B/DF4C/DF4D/DF5/DF7/DF7B/DF7C/DF7D/DF7E/DF7G/DF8/DF8B/DF8CJ/DF12/QZGYNRJC
		5,   17,22, 14,20, 95,115, 14,20, 17,22, 0x11,		//19 17 105 17 19 38 新增加和谐号机车 add 2010/02/26 cm
		5,   17,22, 14,20, 54, 67, 14,20, 17,22, 0x12,		//东风 呼局丢失机车轴距 add 2011/04/22 cm 19 16 60 16 19
		//5,   17,24, 17,23, 75,100, 17,23, 17,24, 0x13,		//	;ND4/DF11   18,23更改为17,23 cm 2010/09/30	//因大秦线两货车连接距19常误判为一机车+二特种车，故台车加大限制 
		5,   18,27, 18,27, 75,100, 18,27, 18,27, 0x13,		//	;add 2014/6/23 HX3D轴距23.5 20.0 80.2 20 23.5  将24->放大28 ND4/DF11   18,23更改为17,23 cm 2010/09/30	//因大秦线两货车连接距19常误判为一机车+二特种车，故台车加大限制 
															//	;add 2014/7/29 HXD3D轴距 20.0 23.5 80.2 23.5 20 23->28
															//  17,28-->18,27   因易与货车轴距  18 81 18 27 冲突修改，因修改23号开机丢18，误判为一机车与特种合并情况   20181119
		//5,   17,25, 17,25, 38,68, 17,25, 17,25, 0x11,		//	;new ND2/ND3 edit 2009/12/02 cm 放开后如果出现特种,会把4轴+2轴 连在一起 6轴
		5,   17,25, 17,25, 30,80,  17,25, 17,25, 0x14,		//	;SS7E SS9 DF DF2 DF3 GK2/3-1/2  ND5 ND2 ND3 ND5 合并 2009/12/25
		5,   19,25, 15,21, 34,40,  15,21, 19,25, 0x15,		//	;GK2C
		5,   20,26, 17,23, 66,77,  17,23, 20,26, 0x16,		//	;SS3/SS6/SS6B
		5,   20,26, 20,26, 53,65,  20,26, 20,26, 0x17,		//	;SS1
		5,   20,26, 20,26, 73,86,  20,26, 20,26, 0x18,		//	;6G
		5,   21,25, 16,20, 80,100, 16,20, 21,25, 0x19,		//	;NY5/NY6/NY7/  
		
		5,   25,31, 25,31, 40,48,  25,31, 25,31, 0x1A,		//	;乌兹别克斯坦  28 28 44 28 28
		5,   26,32, 38,46, 26,32,  38,46, 26,32, 0x1B,		//	;SS7/SS7B/SS7C/SS7D/6K

		3,   20,27, 72,93, 20,27, 0x20,	//   ;东方红 0x5f->0x5d 2006-03-06
		3,   20,27, 42,48, 20,27, 0x21,	//   ;韶山8		
		3,   27,32, 58,80, 27,32, 0x22,	//   ;SS8 KZ4A DJ2 DFH1  edit: 2010/05/26 
	
		//3,   19,27, 38,50, 19,27, 0x1a,	//   ;东风5  佳木斯  2003-02-18
		//3,   19,28, 50,72, 19,28, 0x1c,	//   ;北京  edit: 0x19 -> 0x1C 2009/03/02  cm
		3,   19,28, 38,72, 19,28, 0x23,	//   ;东风5  佳木斯  北京 合并2009/12/25

		4,   15,21, 31,38, 31,38, 15,21, 0x85,				//	;五轴货车 C5D 1750	3600	3600	1750
		15,  6,25, 6,25, 6,25,  6,25, 6,25, 6,25, 6,25, 45,177, 6,25, 6,25, 6,25,  6,25, 6,25, 6,25, 6,25, 0xe3,//	;多轴货车   2003-10-20日移到此处，优先判多轴
		15,  6,25, 6,25, 6,25, 22,40, 6,25, 6,25, 6,25, 45,177, 6,25, 6,25, 6,25, 22,40, 6,25, 6,25, 6,25, 0xe4,
		//  ;凹底平车D26、D25A、D25 落孔下车D26B 可归到此处但因D25A达到34，故将 0x23->0x28  2007-05-14   
		3,   14,20, 43,156, 14,20, 0x82,	//   ;合并 四轴货车 凹底平车D5、D70可以归到此处 具体见长大货车轴距统计表 2003-12-29  //2007-05-14 //cm 2012/2/8 edit 42->43 保证六轴特种 18,31,18,31,18 43不被当成4轴+2轴

		//3,   19,29, 109,224, 19,29, 0x80, //   ;四轴客车   //2002-5-23
		//3,   24,30, 142,148, 24,30, 0x80, //   ;长大四轴客车
		3,   19,28, 88,110, 19,28, 0x83,		//  ;机保  合并 2009/12/25
		3,   20,30, 110,224, 20,30, 0x80,	//   ;四轴客车   合并 2009/12/25 cm

		4,   46,52, 12,16, 90,98, 12,16, 0x80,		//	;长大五轴客车 //武汉 2000-01-18
		4,   12,16, 90,98, 12,16, 46,52, 0x80,		//	;（上车长大五轴客车反向）
		7,   13,17, 24,28, 13,17, 28,32, 13,17, 24,28, 13,17, 0xe1,//	;多轴货车
	
		//3,   13,17, 24,28, 13,17, 0x83,		//   ;首车 del 下车包含该轴距 2009/12/24 cm
		//3,   12,18, 24,30, 12,18, 0x89,		//   ;特种货车 //济南 2000-01-16 del
		3,   13,19, 24,30, 13,19, 0x87,		//   ;特种货车 edit 合并 包含上述2车 2009/12/24 cm
	
		//3,   14,20, 142,152, 14,20, 0x82, //   ;四轴货车
		//3,   15,19, 148,156, 15,19, 0x8a,		//   ;特种货车 del 2009/12/24 上车修改轴距后包含
		//3,   13,21, 126,134, 13,21, 0x8e,		//   ;特种货车;//99-1-19
		//3,   14,20, 142,156, 14,20, 0x82, //   ;四轴货车 合并 edit: 152->156 2009/12/24 del 2010/05/26
		
		//3,   21,28, 92,98, 21,28, 0x81,	//   ;机保;//fengtai jibao
		//3,   19,27, 88,110, 19,27, 0x81,		//  ;机保  //2007-05-14 将0x64->0x6e 为了五节式机械保温车 23 100 23

			
		3,   19,26, 33,43, 19,26, 0x88,			//   ;特种货车 //济南 2000-01-16
		3,   24,30, 144,150, 24,30, 0x8b,		//   ;特种货车 和客车轴距接近 轴距表中无 2009/12/25
		3,   10,14, 150,158, 10,14, 0x8d,		//   ;特种货车;//12 154 12
		3,   15,19, 170,200, 15,19, 0x82,
        3,   9,13, 132,140, 9,13, 0x8c,		//   ;特种货车;//11 136 11
		3,   9,17, 33,40, 9,17, 0x8f,			//	 ;特种货车 
		5,   26,30, 14,19, 127,135, 26,30, 14,19, 0x90,	//	;特种货车//seprate 98,10,24==28 16 130 28 16
		5,   14,19, 26,30, 127,135, 14,19, 26,30, 0x91,	//  ;（上车特种货车//seprate反向）
		5,   14,20, 28,34, 14,20, 28,34, 14,20, 0x92,	//	;特种货车C100//18,31,18,31,18 胡目天提供  //2007-11-09
		5,   24,28, 37,43, 24,28, 37,43, 24,28, 0x93,	//	;特种货车  //武汉 2000-01-18
		5,   6,25, 6,25, 100,177, 6,25, 6,25, 0x94,		//	;多轴货车 // 2007-05-14 6轴凹底平车D10、新D10 可归到此处 该车可能跟机车冲突 45 修改为68 edit 2014/6/23 68->100 防止和新和谐3D，3C，1D冲突 
		5,   11,15, 11,15, 50, 60, 11,15, 11,15, 0x95,	//  ;多轴货车 2012/8/12 add cm 嘉峪关 6轴16A车 13.2 13.2 55.7 13.2 13.2  
		
		7,   9,22,  9,22,  9,22, 80,150, 9,22, 9,22, 9,22, 0xe1,//  ;加8轴长大货车轴距  凹底平车D6\D12\D15\D16G\D22\D27\D22G\D17A  2007-05-14 
	
		19,  6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 45,177, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 6,25, 0xe5,		//  ;凹底平车D9G、落孔下车D19G、钳夹车D30A中中挡148的可归到此处  2007-05-14 
		
		11,  12,16, 12,16, 28,32, 12,16, 12,16, 144,153, 12,16, 12,16, 28,32, 12,16, 12,16, 0xe7,		//  ;多轴货车 add 2009/03/02 cm
	//	23,  6,25,6,25,6,25,6,41,6,25,6,25,6,25,6,25,6,25,6,25,6,25,45,177,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,41,6,25,6,25,6,25, 0xe8,		//	;多轴货车 //加凹底平车D32、落孔下车、350T 轴距  24轴----2007-05-14 
		23,  6,25,6,25,6,25,6,45,6,25,6,25,6,25,6,25,6,25,6,25,6,25,45,177,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,45,6,25,6,25,6,25, 0xe8,		//2017-08-02 LYCH 41-->45	;多轴货车 //加凹底平车D32、落孔下车、350T 轴距  24轴----2007-05-14 
		23,  10,22,10,22,10,22,32,44,10,22,10,22,10,22,10,22,10,22,10,22,10,22,133,160,10,22,10,22,10,22,10,22,10,22,10,22,10,22,32,44,10,22,10,22,10,22, 0xe9,		//	;多轴货车 
		23,  6,17,6,17,19,33,6,17,6,17,37,49,6,17,6,17,19,33,6,17,6,17,45,177,6,17,6,17,19,33,6,17,6,17,37,49,6,17,6,17,19,33,6,17,6,17, 0xea,		//  ;真实轴距13 13 29 13 13 44 13 13 29 13 13 151 13 13 29 13 14 45 13 13 29 13 13 
		23,  6,17,6,17,19,33,6,17,6,17,37,49,6,17,6,17,19,33,6,17,6,17,45,177,6,17,6,17,19,33,6,17,6,17,37,49,6,17,6,17,19,33,6,17,6,17, 0xea,		//  2017-08-02 LYCH DA37

		23,  11,17,11,17,15,21,11,17,11,17,46,58,11,17,11,17,14,20,11,17,11,17,128,159,11,17,11,17,14,20,11,17,11,17,46,58,11,17,11,17,15,21,11,17,11,17, 0xea,		//  lych 2017-08-02  DK36A
	
//		23,  11,17,11,17,14,21,11,17,11,17, 48,56, 11,17,11,17,14,21,11,17,11,17, 133,153, 11,17,11,17,14,21,11,17,11,17, 48,56, 11,17,11,17,14,21,11,17,11,17, 0xef, //add 2014/05/15 cm 24轴 360T落孔空车 轴距14 14 17.5 14 14 52 14 14 17 14 14 143.5 14 14 17 14 14 52 14 14 17.5 14 14 
     	23,  11,17,11,17,14,21,11,17,11,17, 40,56, 11,17,11,17,14,21,11,17,11,17, 30,44, 11,17,11,17,14,21,11,17,11,17, 40,56, 11,17,11,17,14,21,11,17,11,17, 0xef, //2017-08-02 lychDQ35空
	    23,  11,17,11,17,14,25,11,17,11,17, 40,65, 11,17,11,17,14,25,11,17,11,17, 133,183, 11,17,11,17,14,25,11,17,11,17, 40,65, 11,17,11,17,14,25,11,17,11,17, 0xef, //2017-08-02 lychDQ35重 DA37

//	23,  11,17,11,17,11,17,11,17,11,17, 40,49, 11,17,11,17,26,34,11,17,11,17, 133,153, 11,17,11,17,26,34,11,17,11,17, 40,49, 11,17,11,17,11,17,11,17,11,17, 0xef, //add 2014/06/23 cm 24轴 DK36落孔空车 轴距14 14 14 14 14 45 14 14 30 14 14 143.4 14 14 30 14 14 45 14 14 30 14 14 
        23,  11,17,11,17,26,34,11,17,11,17, 39,52, 11,17,11,17,26,34,11,17,11,17, 133,165, 11,17,11,17,26,34,11,17,11,17, 40,49, 11,17,11,17,26,34,11,17,11,17, 0xef, //2017-08-02 lych D32A DK36   add 2014/06/23 cm 24轴 DK36落孔空车 轴距14 14 14 14 14 45 14 14 30 14 14 143.4 14 14 30 14 14 45 14 14 30 14 14 

		27,  6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,45,177,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25,6,25, 0xeb,
        27,  10,22,10,22,10,22,10,22,10,22,38,50,10,22,10,22,10,22,10,22,10,22,10,22,10,22,155,195,10,22,10,22,10,22,10,22,10,22,10,22,10,22,38,50,10,22,10,22,10,22,10,22,10,22, 0xec,		// D45 ;落孔下车450T 28轴轴距----2007-05-14 加
	    27,  11,17,11,17,16,25,11,17,11,17, 11,17, 46,66,11,17,11,17,11,17,16,25, 11,17, 11,17,24,40,11,17,11,17,16,25,11,17,11,17, 11,17, 46,66,11,17,11,17,11,17,16,25, 11,17, 11,17, 0xeb, //2017-08-02 lych DQ45空
	    27,  11,17,11,17,16,25,11,17,11,17, 11,17, 46,66,11,17,11,17,11,17,16,25, 11,17, 11,17,135,165,11,17,11,17,16,25,11,17,11,17, 11,17, 46,66,11,17,11,17,11,17,16,25, 11,17, 11,17, 0xeb, //2017-08-02 lych DQ45重
		31,  10,16,10,16,10,16,12,18,10,16,10,16,10,16,12,34,10,16,10,16,10,16,12,18,10,16,10,16,10,16,28,164,10,16,10,16,10,16,12,18,10,16,10,16,10,16,12,34,10,16,10,16,10,16,12,18,10,16,10,16,10,16, 0xed,
		//;32轴货车2006-01-26 13 13 13 15 13 13 13 29 13 13 13 15 13 13 13 142 13 13 13 15 13 13 13 28 13 13 13 15 13 13 13  这是钳夹车 D38的一种，另两种是中档对应142的是154和33的
		//钳夹车D35的不同之处是对应29处是16，中档是34的，这些放在下面一起
//		5,   10,16, 10,16, 10,16, 10,16, 10,16, 0x96,							//	;特种货车  2017-03-17
		9,   6,25, 6,25, 6,25, 6,25, 45,177, 6,25, 6,25, 6,25, 6,25, 0xee,			//	;多轴货车 // 2007-05-14 落孔下车 D17  可归到此处
		11,  6,25, 6,25, 6,25, 6,25, 6,25, 45,177, 6,25, 6,25, 6,25, 6,25, 6,25, 0xef,//	;多轴货车 // 2007-05-14 凹底平车 D7 落孔下车 D17进口 可归到此处
		5,   8,16, 8,16, 19,27, 8,16, 8,16, 0x97,								//	;多轴货车;应为机车
		7,   8,16, 8,16, 8,16, 16,24, 8,16, 8,16, 8,16, 0xe2,						//  ;应为机车
		7,   20,29, 70,112, 8,15, 8,15, 8,15, 12,25, 20,29, 0xac,					//  ;实验车// 2002-4-29 上海局
		7,   20,29, 12,25, 8,15, 8,15, 8,15, 70,112, 20,29, 0xad,					//  ;实验车// 2002-4-29 上海局
		7,   15,21, 13,19, 15,21, 42,141, 15,21, 13,19, 15,21, 0xf2,				//	;此8轴与实验车匹配
		7,   14,20, 9,15, 14,20, 42,141, 14,20, 9,15, 14,20, 0xf2,				//	;此8轴与实验车匹配
		7,   6,25, 6,25, 6,25, 45,177, 6,25, 6,25, 6,25, 0xe3,					//	;此8轴与实验车匹配 //0x91原车型未改
		15,  10,16, 11,17, 10,16, 11,17, 10,16, 11,17, 10,16, 115,127, 10,16, 10,16, 11,17, 10,16, 11,17, 10,16, 11,17, 0xf5,//	;特种货?//?9-3-16
		//4,   13,22,13,22,76,90,13,22, 0x5,			//	;前进   //济南 2000-01-16
		//4,   13,22,76,90,13,22,13,22, 0x5,			//	;前进   //济南 2000-01-16 del 2009/12/24 cm
		8,   10,15, 10,15, 10,15, 10,15, 130,197, 15,20, 8,20, 15,20, 0xf3,//其他货车 TJ165A add: 2009/03/02  cm
		8,   10,16, 10,16, 10,16, 10,16, 130,170, 14,22, 7,13, 14,22, 0xf4, //add 2014/06/23 昆明增加 1300	1250	1250	1300	14825	1750	1000	1750
		8,   14,22, 7,13, 14,22, 130,170, 10,16, 10,16, 10,16, 10,16, 0xf4, //add 2014/06/23 同上 反向

		7,   10,15, 16,22, 10,15, 160,202, 15,20,  8,14, 15,20, 0xe4,		//其他货车 TJ130B add: 2009/03/02  cm
		7,   15,20,  8,14, 15,20, 160,202, 10,15, 16,22, 10,15, 0xe4,		//上车反向

		5,   14,20, 14,20, 143,156, 14,20, 14,20, 0x80,	//	;特种客车    温家宝专列// 0x11 0x11 0x97 0x11 0x11  ;99-09-14
		15,  12,17, 12,17, 12,17, 12,17, 12,17, 12,17, 12,17, 80,152, 12,17, 12,17, 12,17, 12,17, 12,17, 12,17, 12,17, 0xf6,//	;郑州局月山	2000-2-22
		3,   14,20,  9,17, 14,20, 0x84,//   ;特种货车//diao che
		3,   15,19, 29,34, 15,19, 0x81,		//	 ;N15 四轴货车 add: 2009/03/02  cm 2012/07/11 和C100轴距重叠 18,31,18,31,18 将其移动都轴距库尾部 edit 2013/03/14 cm
		5,   60,70, 13,23, 13,23, 85,115, 13,23, 0xe5,	//昆明提供特种车 6轴 6520	1800	1820	10083	1800
		5,   13,23, 85,115, 13,23, 13,23, 60,70, 0xe5,	//昆明提供特种车 6轴 同上反向 add 2014/06/23'

		15,  11,21, 10,18, 11,21, 26,34, 11,21, 10,18, 11,21, 100,133, 11,21, 10,18, 11,21, 26,34, 11,21, 10,18, 11,21, 0xf6,//	;add 2014/06/23  cm 郑州D26B 16轴 16.5 13.5 16.5	30 16.5	13.5 16.5 116.5 16.5 13.5 16.5	30 16.5 13.5 16.5
		3,   15,21, 170,200, 15,21, 0x82,    //2017-01-03 加jxq6轴距
		7,   15,21, 160,200, 15,21, 160,200, 15,21, 160,200, 15,21, 0x82,   //20181210  加Q8轴距
//1,   36,46, 0x20,							//  ;轨道车 //ji che98.9.21 del 2011/05/26 cm 防止误干扰
		0xfe };


                for (i = 0; i < Axle_distance_table_temp.Length; i++)
                    Axle_distance_table[i] = (byte)Axle_distance_table_temp[i];
                /*
                   try
                   {
                       FileStream myfile = File.Open("c:\\zjjudge.dat", FileMode.Create);
                       myfile.Write(Axle_distance_table, (int)0, 2048);
                       myfile.Close();
                   }
                   catch { }
                 */
            }

            private unsafe uint match(byte* romp2, byte* p1)
            {
                uint i, m1 = 0;
                byte* rp2;
                rp2 = romp2;
                for (i = 0; i < *(romp2); i++)
                {
                    if (*(rp2 + i * 2 + 1) < (*(p1 + i)) && ((*(rp2 + i * 2 + 2)) > (*(p1 + i))))
                    {
                        m1++;
                    }
                    else
                    {
                        return 0;
                    }
                }
                if (*(p1 + (*rp2)) == 0xff)
                {
                    return m1;
                };
                if ((*(p1 + (*rp2)) > 0x45) || *(p1 + (*rp2)) < 0x10)//wzh modify at 2003-9-5   13-0f
                {
                    return 0;
                }
                else return m1;
            }

            private unsafe int check(byte* pp3)
            {
                byte* pp2;
                byte flag = 0;

                fixed (byte* pp1 = Axle_distance_table)
                {
                    pp2 = pp1;
                    while (*pp2 != 0xfe)
                    {
                        if (match(pp2, pp3) != 0)
                        {
                            flag = 1; break;
                        }
                        else pp2 = pp2 + (*pp2) * 2 + 2;
                    }
                }
                if (flag == 0) return 0;
                else return 1;
            }

            public unsafe int make_car_table()
            {
                byte* p1;
                byte* p2;
                byte* romp2;
                byte* romp1;
                int i, j, offset;
                int matchflag = 0;
                int checksum = 3;
                byte conum = 0;

                carsum1 = 0;
                carsum = 0;
                uint axlenum;
                ushort nomatchaxle = 0;

                fixed (byte* ppp1 = Axle_distance_table)
                {
                    romp1 = ppp1;
                }
                fixed (byte* ppp2 = distance)
                {
                    p1 = ppp2;
                }       //轴距表地址
                if (*(p1) == 0xff) return 0;
                romp2 = romp1;

                try
                {
                    while (true)
                    {
                        carsum1 = carsum;
                        while (*romp2 != 0xfe)
                        {
                            axlenum = match(romp2, p1);
                            if (axlenum != 0)//matched
                            {
                                if (conum != 0)
                                {
                                    CTPtr[carsum].CarType = 0xdd;       //特种车类型
                                    if (carsum == 0) CTPtr[carsum].CarType = 0x79;//特种车类型
                                    CTPtr[carsum].BearTypeAxle = conum;
                                    carsum++;
                                    matchflag = 1;
                                    conum = 0;
                                }
                                CTPtr[carsum].CarType = *(romp2 + (*romp2) * 2 + 1);
                                if (CTPtr[carsum].CarType != 0x20) matchflag = 0;
                                CTPtr[carsum].BearTypeAxle = (byte)(axlenum + 1);
                                carsum++;
                                if (matchflag != 0)
                                {
                                    if (*(p1 + *(romp2)) != 0xff)
                                    {
                                        checksum = check(p1 + *(romp2));
                                        if (checksum != 0)
                                        {
                                            p1 = p1 + *(romp2);
                                            CTPtr[carsum - 2].BearTypeAxle += 1;
                                            carsum--;    //99-5-15
                                        }
                                        else p1 = p1 + *(romp2) + 1;
                                    }
                                    else p1 = p1 + *(romp2) + 1;
                                    matchflag = 0;
                                }
                                else
                                    p1 = p1 + *(romp2) + 1;
                                //	romp1=Axle_distance_table;
                                romp2 = romp1;
                                conum = 0;
                                break;
                            }
                            else
                            {
                                romp2 = romp2 + (*romp2) * 2 + 2;
                            };
                        };
                        if (carsum == carsum1)
                        {
                            if (conum == 0) p2 = p1;
                            conum++;
                            if (conum >= 4)
                            {
                                CTPtr[carsum].CarType = 0xdd;//特种车类型
                                if (carsum == 0) CTPtr[carsum].CarType = 0x79;
                                CTPtr[carsum].BearTypeAxle = conum;
                                carsum++;
                                conum = 0;
                            }
                            p1 = p1 + 1;
                            nomatchaxle++;
                        }
                        romp2 = romp1;
                        if (*p1 < 8) break;
                        if (*(p1 - 1) == 0xff) break;
                    }
                }
                catch { };

                if (conum != 0)
                {
                    CTPtr[carsum].CarType = 0xdd;		//特种车类型
                    if (carsum == 0) CTPtr[carsum].CarType = 0x79;
                    CTPtr[carsum].BearTypeAxle = conum;
                    carsum++;
                    conum = 0;
                }
                missaxle = nomatchaxle;
                if (carsum == 0) return 0;
                else
                {
                    offset = 0;
                    for (i = 0; i < carsum; i++)
                    {
                        CTPtr[i].AxleAddr = (UInt16)offset;
                        for (j = 0; j < CTPtr[i].BearTypeAxle; j++)
                        {
                            CTPtr[i].distance[j] = distance[offset + j];
                        }
                        offset += CTPtr[i].BearTypeAxle;
                    }

                    return 1;
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
            //Globals.RootSavePath = Globals.systemParam.savePath;

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
            uiNavBar1.Nodes.Add("数据查询");
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

            //pageIndex++;
            //uiNavBar1.Nodes.Add("报警数据");
            //uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[2], 62151);
            //fAlarmData = new FAlarmData();
            //AddPage(fAlarmData, pageIndex);
            //uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[2], pageIndex);


            //uiNavBar1.Nodes.Add("系统设置");
            //uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[3], 61459);

            uiNavBar1.SelectedIndex = 0;
            //初始化数据
            initDatas();

            DeleteDirectoryAndFile(30);
            timer1.Interval = Globals.systemParam.deleteFileInterval;//删除文件和文件夹
            timer1.Enabled = true;
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

            info = (UILabel)fmonitor.GetControl("uiLabel1");
            //获取Fmonitor界面开始采集按钮，并添加相关事件
            startPrewviewBtn = (UISymbolButton)fmonitor.GetControl("startPrewviewBtn");
            startPrewviewBtn.Click += new EventHandler(StartPrewviewBtn_Click);
            startPrewviewBtn.MouseHover += new EventHandler(StartPrewviewBtn_MouseHover);
            startPrewviewBtn.MouseLeave += new EventHandler(StartPrewviewBtn_MouseLeave);
            //startPrewviewBtn.Visible = false;

            //获取Fmonitor界面停止按钮，并添加相关事件
            stopPrewviewBtn = (UISymbolButton)fmonitor.GetControl("stopPrewviewBtn");
            stopPrewviewBtn.Click += new EventHandler(StopPrewviewBtn_Click);
            stopPrewviewBtn.MouseHover += new EventHandler(StopPrewviewBtn_MouseHover);
            stopPrewviewBtn.MouseLeave += new EventHandler(StopPrewviewBtn_MouseLeave);
            //stopPrewviewBtn.Visible = false;


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
            //mouseFollowBtn.Visible = false;

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
            //drawRectBtn.Visible = false;

            //获取Fmoitor界面画圆形按钮，并添加相关事件
            drawCircleBtn = (UISymbolButton)fmonitor.GetControl("drawCircleBtn");
            drawCircleBtn.Click += new EventHandler(drawCircleBtn_Click);
            drawCircleBtn.MouseHover += new EventHandler(drawCircleBtn_MouseHover);
            drawCircleBtn.MouseLeave += new EventHandler(drawCircleBtn_MouseLeave);
            //drawCircleBtn.Visible = false;

            //获取Fmoitor界面删除所有选区按钮，并添加相关事件
            deleteAllDrawBtn = (UISymbolButton)fmonitor.GetControl("deleteAllDrawBtn");
            deleteAllDrawBtn.Click += new EventHandler(deleteAllDrawBtn_Click);
            deleteAllDrawBtn.MouseHover += new EventHandler(deleteAllDrawBtn_MouseHover);
            deleteAllDrawBtn.MouseLeave += new EventHandler(deleteAllDrawBtn_MouseLeave);
            //deleteAllDrawBtn.Visible = false;

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


            Thread.Sleep(100);

        }

        private void MoitorTrainLeave_0()
        {
            while (true)
            {
                trainLeaveEvent.WaitOne();
                StopRecord();
                Thread.Sleep(5);
            }
        }

        private void MoitorTrainComing_0()
        {
            while (true)
            {
                trainComingEvent.WaitOne();
                StartRecording();
                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// 保存可见光图像--设备0
        /// </summary>
        private void SaveOPImage_0()
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
        /// 保存可见光图像--设备1
        /// </summary>
        private void SaveOPImage_1()
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
                    sJpegPicFileName = saveReportPath[1] + "\\" + "OP_Image";

                    //sJpegPicFileName = "filetest.jpg";//图片保存路径和文件名 the path and file name to save

                    //判断文件夹是否存在，如果不存在，新建文件夹
                    if (!Directory.Exists(sJpegPicFileName))
                    {
                        Directory.CreateDirectory(sJpegPicFileName);
                    }
                    sJpegPicFileName += "\\" + strTime + ".jpeg";

                    //通过SDK进行可见光抓图
                    if (!CHCNetSDK.NET_DVR_CaptureJPEGPicture(mUserIDs[1], 1, ref lpJpegPara, sJpegPicFileName))
                    {
                        iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                        str = "设备" + ipList[1] + "可见光抓图失败！错误码为：" + iLastErr;
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
        private void GetImage_0()
        {
            while (true)
            {
                //Console.WriteLine("mUserIDs[0]" + mUserIDs[0]);
                if (isTrainStart)
                {

                    isSavingIrImg_0 = true;

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
                            if (cacheDataCount_0 >= 20)
                            {   //将每条数据的最大值存到集合中
                                List<int> maxTempList = new List<int>();
                                for (int i = 0; i < cacheData_0.Length; i++)
                                {
                                    //Console.WriteLine("maxTempListitem_" + i +  BitConverter.ToInt32(cacheData_0[i].GetRange(0, 4).ToArray(), 0));
                                    maxTempList.Add(BitConverter.ToInt32(cacheData_0[i].GetRange(0, 4).ToArray(), 0));
                                }

                                //获取集合中最小温度值及其位置
                                int min = maxTempList.Min();
                                int minIndex = maxTempList.IndexOf(min);

                                //Console.WriteLine("maxTemp" + maxTemp*10);
                                //Console.WriteLine("min" + min);
                                //Console.WriteLine("minIndex" + minIndex);

                                //如果当前温度最大值>集合中最小温度值，则将其替换
                                if (maxTemp * 10 > min)
                                {
                                    cacheData_0[minIndex].Clear();
                                    index = minIndex;
                                    byte[] t = new byte[4];
                                    t = BitConverter.GetBytes((int)(maxTemp * 10));


                                    cacheData_0[index].AddRange(t);//添加最高温*10转换成字节数组，4个字节
                                    cacheData_0[index].AddRange(dateTimeNowBytes);//添加当前时间戳8个字节

                                    byte[] a = new byte[2];
                                    //Console.WriteLine("轴序" + m_AxleCount);
                                    a = BitConverter.GetBytes(m_AxleCount);
                                    cacheData_0[index].AddRange(a);//添加轴数，2个字节
                                    //for (int i = 0; i < 2; i++)//2个预留字节
                                    //{
                                    //    cacheData_0[index].Add(0x00);
                                    //}

                                    cacheData_0[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwJpegPicLen));//添加红外图像长度，4个字节
                                    cacheData_0[index].AddRange(IRImageArray);//添加红外图像数据
                                    cacheData_0[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwP2PDataLen));//添加温度数据长度，四个字节
                                    cacheData_0[index].AddRange(IRTempArray);//添加温度数据
                                                                             //cacheData_0[index].AddRange(BitConverter.GetBytes(dwSizeReturned));//添加可见光图像长度，4个字节
                                                                             //cacheData_0[index].AddRange(byJpegPicBuffer);//添加可见光图像数据
                                }
                            }
                            else//缓存中数据少于20条，依次添加数据
                            {
                                index = cacheDataCount_0;
                                byte[] t = new byte[4];
                                //Console.WriteLine("maxTemp" + maxTemp);
                                t = BitConverter.GetBytes((int)(maxTemp * 10));//最高温乘以10，转换为字节数组

                                //int bb = BitConverter.ToInt32(t, 0);
                                //Console.WriteLine("bb" + bb);


                                cacheData_0[index].AddRange(t);//添加最高温*10转换成字节数组，4个字节
                                cacheData_0[index].AddRange(dateTimeNowBytes);//添加当前时间戳8个字节

                                //Console.WriteLine("轴序" + m_AxleCount);
                                byte[] a = new byte[2];
                                a = BitConverter.GetBytes(m_AxleCount);
                                cacheData_0[index].AddRange(a);//添加轴数，2个字节
                                //for (int i = 0; i < 2; i++)//10个预留字节
                                //{
                                //    cacheData_0[index].Add(0x00);
                                //}

                                cacheData_0[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwJpegPicLen));//添加红外图像长度，4个字节
                                cacheData_0[index].AddRange(IRImageArray);//添加红外图像数据
                                cacheData_0[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwP2PDataLen));//添加温度数据长度，4个字节
                                cacheData_0[index].AddRange(IRTempArray);//添加温度数据
                                                                         //cacheData_0[index].AddRange(BitConverter.GetBytes(dwSizeReturned));//添加可见光图像长度，4个字节
                                                                         //cacheData_0[index].AddRange(byJpegPicBuffer);//添加可见光图像数据
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

                        cacheDataCount_0++;//缓存数据数量加1
                    }
                    isSavingIrImg_0 = false;//缓存红外图像数据结束

                }

                Thread.Sleep(1);

            }
        }

        /// <summary>
        /// 获取红外图像和温度数据，并将其存入缓冲区
        /// </summary>
        private void GetImage_1()
        {
            while (true)
            {
                if (isTrainStart)
                {

                    isSavingIrImg_1 = true;

                    if (mUserIDs[1] >= 0)
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
                            bool res = CHCNetSDK.NET_DVR_CaptureJPEGPicture_WithAppendData(mUserIDs[1], 2, ref struJpegWithAppendData);

                            if (res != true)
                            {
                                iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                                str = "设备" + ipList[1] + "热成像抓图失败，错误码为：" + iLastErr;
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
                            if (cacheDataCount_1 >= 20)
                            {   //将每条数据的最大值存到集合中
                                List<int> maxTempList = new List<int>();
                                for (int i = 0; i < cacheData_1.Length; i++)
                                {
                                    //Console.WriteLine("maxTempListitem_" + i +  BitConverter.ToInt32(cacheData_0[i].GetRange(0, 4).ToArray(), 0));
                                    maxTempList.Add(BitConverter.ToInt32(cacheData_1[i].GetRange(0, 4).ToArray(), 0));
                                }

                                //获取集合中最小温度值及其位置
                                int min = maxTempList.Min();
                                int minIndex = maxTempList.IndexOf(min);

                                //Console.WriteLine("maxTemp" + maxTemp*10);
                                //Console.WriteLine("min" + min);
                                //Console.WriteLine("minIndex" + minIndex);

                                //如果当前温度最大值>集合中最小温度值，则将其替换
                                if (maxTemp * 10 > min)
                                {
                                    cacheData_1[minIndex].Clear();
                                    index = minIndex;
                                    byte[] t = new byte[4];
                                    t = BitConverter.GetBytes((int)(maxTemp * 10));


                                    cacheData_1[index].AddRange(t);//添加最高温*10转换成字节数组，4个字节
                                    cacheData_1[index].AddRange(dateTimeNowBytes);//添加当前时间戳8个字节

                                    byte[] a = new byte[2];
                                    //Console.WriteLine("轴序" + m_AxleCount);
                                    a = BitConverter.GetBytes(m_AxleCount);
                                    cacheData_1[index].AddRange(a);//添加轴数，2个字节
                                    //for (int i = 0; i < 2; i++)//2个预留字节
                                    //{
                                    //    cacheData_0[index].Add(0x00);
                                    //}

                                    cacheData_1[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwJpegPicLen));//添加红外图像长度，4个字节
                                    cacheData_1[index].AddRange(IRImageArray);//添加红外图像数据
                                    cacheData_1[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwP2PDataLen));//添加温度数据长度，四个字节
                                    cacheData_1[index].AddRange(IRTempArray);//添加温度数据
                                                                             //cacheData_0[index].AddRange(BitConverter.GetBytes(dwSizeReturned));//添加可见光图像长度，4个字节
                                                                             //cacheData_0[index].AddRange(byJpegPicBuffer);//添加可见光图像数据
                                }
                            }
                            else//缓存中数据少于20条，依次添加数据
                            {
                                index = cacheDataCount_1;
                                byte[] t = new byte[4];
                                //Console.WriteLine("maxTemp" + maxTemp);
                                t = BitConverter.GetBytes((int)(maxTemp * 10));//最高温乘以10，转换为字节数组

                                //int bb = BitConverter.ToInt32(t, 0);
                                //Console.WriteLine("bb" + bb);


                                cacheData_1[index].AddRange(t);//添加最高温*10转换成字节数组，4个字节
                                cacheData_1[index].AddRange(dateTimeNowBytes);//添加当前时间戳8个字节

                                //Console.WriteLine("轴序" + m_AxleCount);
                                byte[] a = new byte[2];
                                a = BitConverter.GetBytes(m_AxleCount);
                                cacheData_1[index].AddRange(a);//添加轴数，2个字节
                                //for (int i = 0; i < 2; i++)//10个预留字节
                                //{
                                //    cacheData_0[index].Add(0x00);
                                //}

                                cacheData_1[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwJpegPicLen));//添加红外图像长度，4个字节
                                cacheData_1[index].AddRange(IRImageArray);//添加红外图像数据
                                cacheData_1[index].AddRange(BitConverter.GetBytes(struJpegWithAppendData.dwP2PDataLen));//添加温度数据长度，4个字节
                                cacheData_1[index].AddRange(IRTempArray);//添加温度数据
                                                                         //cacheData_0[index].AddRange(BitConverter.GetBytes(dwSizeReturned));//添加可见光图像长度，4个字节
                                                                         //cacheData_0[index].AddRange(byJpegPicBuffer);//添加可见光图像数据
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
                            str = "设备" + ipList[1] + "热成像抓图失败，异常信息为：" + ex.ToString();
                            Globals.Log(str);
                        }

                        cacheDataCount_1++;//缓存数据数量加1
                    }
                    isSavingIrImg_1 = false;//缓存红外图像数据结束

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

            try
            {
                // If the source directory does not exist, throw an exception.
                if (!dir.Exists)
                {
                    //throw new DirectoryNotFoundException($"Source directory does not exist or could not be found: {sourceDir}");
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
            catch (Exception ex)
            {
                Globals.Log("拷贝可见光文件夹失败" + ex.ToString());
            }
        }


        //温度最大值（最大值*10）      时间戳    轴序     红外图像长度   红外数据   温度数据长度     温度数据
        // 4个字节                     8个字节   2个字节     4个字节                   4个字节
        /// <summary>
        /// 解析缓存数据
        /// </summary>
        private void AnalysisData()
        {
            int carLocation = 0;
            int locomotiveCount = 0;

            try
            {

                for (int i = 0; i < cacheData_0.Length; i++)
                {
                    if (cacheData_0[i].Count > 0)
                    {

                        float maxTemp = BitConverter.ToSingle(cacheData_0[i].GetRange(0, 4).ToArray(), 0);//获取最高温
                                                                                                          //Console.WriteLine("最大值" + maxTemp * 1.0f / 10);
                        byte[] timeBytes = cacheData_0[i].GetRange(4, 8).ToArray();//获取时间戳字节数组

                        long time = TicksTimeConvert.BytesToTimestamp(timeBytes);//获取时间戳      
                        DateTime aa = TicksTimeConvert.Ticks132LocalTime(time);  //时间戳转本地时间

                        short axelNum = BitConverter.ToInt16(cacheData_0[i].GetRange(12, 2).ToArray(), 0);//获取轴序
                                                                                                          //Console.WriteLine("获取的轴序" + axelNum);
                        carLocation = 0;
                        locomotiveCount = 0;

                        if (!Globals.systemParam.isContainLocomotive)
                        {
                            //定位第几辆车
                            for (int j = 0; j < m_CarSum; j++)
                            {
                                if (mCarTableDel.CTPtr[j].CarType < 0x80)//统计机车数量
                                {
                                    locomotiveCount += 1;
                                }
                                if (j < m_CarSum - 1)
                                {
                                    if (axelNum >= mCarTableDel.CTPtr[j].AxleAddr && axelNum < mCarTableDel.CTPtr[j + 1].AxleAddr)
                                    {
                                        carLocation = j + 1;
                                    }
                                }
                                else
                                {
                                    if (axelNum >= mCarTableDel.CTPtr[j].AxleAddr)
                                    {
                                        carLocation = j + 1;
                                    }
                                }

                            }

                        }
                        carLocation = carLocation - locomotiveCount + 1;
                        //Console.WriteLine("carLocation" + carLocation);

                        string strTime = aa.ToString("yyyyMMdd_HHmmss_fff");//格式化本地时间

                        int IRImageLength = BitConverter.ToInt32(cacheData_0[i].GetRange(4 + 8 + 2, 4).ToArray(), 0);//获取红外图像数据的长度
                        int tempDataLength = BitConverter.ToInt32(cacheData_0[i].GetRange(4 + 8 + 2 + 4 + IRImageLength, 4).ToArray(), 0);//获取温度数据的长度

                        byte[] IRTempArray = cacheData_0[i].GetRange(4 + 8 + 2 + 4 + IRImageLength + 4, tempDataLength).ToArray();//获取红外图像温度字节数组
                        float[] temp = Globals.TempBytesToTempFloats(IRTempArray, IR_IMAGE_WIDTH, IR_IMAGE_HEIGHT);//将温度字节数组转换为实际温度数组
                        List<float> tempList = temp.ToList();//将温度数组转为集合
                        maxTemp = tempList.Max();//获取最高温度值

                        int maxTempIndex = tempList.IndexOf(maxTemp);//获取最高温度值所在位置
                        int maxTempX = maxTempIndex % IR_IMAGE_WIDTH;//最高温度值x坐标
                        int maxTempY = maxTempIndex / IR_IMAGE_WIDTH;//最高温度值y坐标

                        RectangleF rectF = GetRectArea(maxTempX, maxTempY, 5, 5);//选择最高温度点周围10*10的区域
                        float[] result = getTempAtRect(temp, rectF, IR_IMAGE_WIDTH);//获取该区域的最值、平均值 result[2] 为平均值

                        string irImagePath = saveReportPath[0] + "\\" + strTime + "_IR_" + carLocation + ".jpeg";//红外图像文件名
                        string tempDataPath = saveReportPath[0] + "\\" + strTime + "_temp_" + carLocation + ".dat";//温度数据文件名


                        WriteBytesToFile(irImagePath, cacheData_0[i].GetRange(4 + 8 + 2 + 4, IRImageLength).ToArray(), IRImageLength);//保存红外图像
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
                            string alarmDataPath = alarmReportPath[0] + "\\" + strTime + "_temp_" + carLocation + ".dat";//温度数据文件名
                            string alarmIrImagePath = alarmReportPath[0] + "\\" + strTime + "_IR_" + carLocation + ".jpeg";//红外图像文件名

                            WriteBytesToFile(alarmIrImagePath, cacheData_0[i].GetRange(4 + 8 + 2 + 4, IRImageLength).ToArray(), IRImageLength);//保存红外图像
                            WriteBytesToFile(alarmDataPath, IRTempArray, tempDataLength);//保存温度数据
                            if (isCopyOpImage[0] == false)//拷贝可见光图片文件夹
                            {
                                CopyDirectory(saveReportPath[0] + "\\" + "OP_Image", alarmReportPath[0] + "\\" + "OP_Image");
                                isCopyOpImage[0] = true;
                            }

                        }

                    }
                }


                if (Globals.systemParam.deviceCount > 1)
                {

                    for (int i = 0; i < cacheData_1.Length; i++)
                    {
                        if (cacheData_1[i].Count > 0)
                        {

                            float maxTemp = BitConverter.ToSingle(cacheData_0[i].GetRange(0, 4).ToArray(), 0);//获取最高温
                                                                                                              //Console.WriteLine("最大值" + maxTemp * 1.0f / 10);
                            byte[] timeBytes = cacheData_1[i].GetRange(4, 8).ToArray();//获取时间戳字节数组

                            long time = TicksTimeConvert.BytesToTimestamp(timeBytes);//获取时间戳      
                            DateTime aa = TicksTimeConvert.Ticks132LocalTime(time);  //时间戳转本地时间

                            short axelNum = BitConverter.ToInt16(cacheData_1[i].GetRange(12, 2).ToArray(), 0);//获取轴序
                                                                                                              //Console.WriteLine("获取的轴序" + axelNum);
                            carLocation = 0;
                            locomotiveCount = 0;

                            if (!Globals.systemParam.isContainLocomotive)
                            {

                                //定位第几辆车
                                for (int j = 0; j < m_CarSum; j++)
                                {
                                    if (mCarTableDel.CTPtr[j].CarType < 0x80)//统计机车数量
                                    {
                                        locomotiveCount += 1;
                                    }
                                    if (j < m_CarSum - 1)
                                    {
                                        if (axelNum >= mCarTableDel.CTPtr[j].AxleAddr && axelNum < mCarTableDel.CTPtr[j + 1].AxleAddr)
                                        {
                                            carLocation = j + 1;
                                        }
                                    }
                                    else
                                    {
                                        if (axelNum >= mCarTableDel.CTPtr[j].AxleAddr)
                                        {
                                            carLocation = j + 1;
                                        }
                                    }

                                }
                            }

                            carLocation = carLocation - locomotiveCount + 1;
                            //Console.WriteLine("carLocation" + carLocation);

                            string strTime = aa.ToString("yyyyMMdd_HHmmss_fff");//格式化本地时间

                            int IRImageLength = BitConverter.ToInt32(cacheData_1[i].GetRange(4 + 8 + 2, 4).ToArray(), 0);//获取红外图像数据的长度
                            int tempDataLength = BitConverter.ToInt32(cacheData_1[i].GetRange(4 + 8 + 2 + 4 + IRImageLength, 4).ToArray(), 0);//获取温度数据的长度

                            byte[] IRTempArray = cacheData_1[i].GetRange(4 + 8 + 2 + 4 + IRImageLength + 4, tempDataLength).ToArray();//获取红外图像温度字节数组
                            float[] temp = Globals.TempBytesToTempFloats(IRTempArray, IR_IMAGE_WIDTH, IR_IMAGE_HEIGHT);//将温度字节数组转换为实际温度数组
                            List<float> tempList = temp.ToList();//将温度数组转为集合
                            maxTemp = tempList.Max();//获取最高温度值

                            int maxTempIndex = tempList.IndexOf(maxTemp);//获取最高温度值所在位置
                            int maxTempX = maxTempIndex % IR_IMAGE_WIDTH;//最高温度值x坐标
                            int maxTempY = maxTempIndex / IR_IMAGE_WIDTH;//最高温度值y坐标

                            RectangleF rectF = GetRectArea(maxTempX, maxTempY, 5, 5);//选择最高温度点周围10*10的区域
                            float[] result = getTempAtRect(temp, rectF, IR_IMAGE_WIDTH);//获取该区域的最值、平均值 result[2] 为平均值

                            string irImagePath = saveReportPath[1] + "\\" + strTime + "_IR_" + carLocation + ".jpeg";//红外图像文件名
                            string tempDataPath = saveReportPath[1] + "\\" + strTime + "_temp_" + carLocation + ".dat";//温度数据文件名


                            WriteBytesToFile(irImagePath, cacheData_1[i].GetRange(4 + 8 + 2 + 4, IRImageLength).ToArray(), IRImageLength);//保存红外图像
                            WriteBytesToFile(tempDataPath, IRTempArray, tempDataLength);//保存温度数据
                                                                                        //Console.WriteLine("平均值" + result[2]);

                            //最大值温度点周围10*10区域温度平均值大于报警阈值，存储报警图片
                            if (result[2] > Globals.systemParam.alarm_0)
                            {
                                isAlarm[1] = true;
                                if (!Directory.Exists(alarmReportPath[1]))
                                {
                                    Directory.CreateDirectory(alarmReportPath[1]);
                                }
                                string alarmDataPath = alarmReportPath[1] + "\\" + strTime + "_temp_" + carLocation + ".dat";//温度数据文件名
                                string alarmIrImagePath = alarmReportPath[1] + "\\" + strTime + "_IR_" + carLocation + ".jpeg";//红外图像文件名

                                WriteBytesToFile(alarmIrImagePath, cacheData_1[i].GetRange(4 + 8 + 2 + 4, IRImageLength).ToArray(), IRImageLength);//保存红外图像
                                WriteBytesToFile(alarmDataPath, IRTempArray, tempDataLength);//保存温度数据
                                if (isCopyOpImage[1] == false)//拷贝可见光图片文件夹
                                {
                                    CopyDirectory(saveReportPath[1] + "\\" + "OP_Image", alarmReportPath[1] + "\\" + "OP_Image");
                                    isCopyOpImage[1] = true;
                                }

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
                saveXmlPath += splitPath[splitPath.Length - 2] + ".xml";

                //IndexListInfo indexList = new IndexListInfo();
                //Globals.ReadInfoXml<IndexListInfo>(ref indexList, saveXmlPath);

                TrainIndex trainIndex = new TrainIndex();

                bool alarm;
                if (Globals.systemParam.deviceCount == 1)
                {
                    alarm = isAlarm[0];
                }
                else
                {
                    alarm = isAlarm[0] | isAlarm[1];
                }

                if (alarm)
                {
                    trainIndex.isAlarm = "是";
                }
                else
                {
                    trainIndex.isAlarm = "否";
                }

                trainIndex.detectTime = splitPath[splitPath.Length - 1].Substring(9, 2) + ":" + splitPath[splitPath.Length - 1].Substring(11, 2);
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
                if (Globals.systemParam.deviceCount > 1)
                {
                    isAlarm[1] = false;
                }

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



                string trainInfoXmlFilePath = trainInfoReportPath[0] + "\\" + splitPath[splitPath.Length - 1] + ".xml";
                TrainListInfo trainListInfo = new TrainListInfo();
                for (uint k = 0; k < m_CarSum; k++)
                {
                    TrainInfo trainInfo = new TrainInfo();
                    trainInfo.indexID = k + 1;
                    trainInfo.carType = mCarTableDel.CTPtr[k].CarType;
                    trainInfo.bearTypeAxle = mCarTableDel.CTPtr[k].BearTypeAxle;
                    trainInfo.axleDistance = mCarTableDel.CTPtr[k].distance;
                    trainInfo.axleAddr = mCarTableDel.CTPtr[k].AxleAddr;
                    trainInfo.carCount = m_CarSum;
                    trainInfo.locomotiveCount = locomotiveCount;
                    trainInfo.axleCount = m_AxleSum;
                    trainListInfo.trainIndexList.Add(trainInfo);

                }

                trainListInfo.axleSpeed = axleSpeedTable;
                Globals.WriteInfoXml<TrainListInfo>(trainListInfo, trainInfoXmlFilePath);//将过车数据写入索引文件

            }
            catch (Exception ex)
            {
                Globals.Log("解析和保存数据失败" + ex.ToString());
            }
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

        private void DeleteDirectoryAndFile(int saveDay)
        {
            string alarmReportPath_0 = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0;
            string alarmReportPath_1 = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_1;

            string saveReportPath_0 = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0;
            string saveReportPath_1 = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_1;

            string trainInfoReportPath_0 = Globals.RootSavePath + "\\" + "TrainInfoReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0;
            string trainInfoReportPath_1 = Globals.RootSavePath + "\\" + "TrainInfoReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_1;

            try
            {
                if (Directory.Exists(alarmReportPath_0))
                {
                    Globals.DeleteDirectory(alarmReportPath_0, saveDay);

                    Globals.DeleteFile(saveReportPath_0, saveDay);
                }

                if (Directory.Exists(saveReportPath_0))
                {
                    Globals.DeleteDirectory(saveReportPath_0, saveDay);
                }

                if (Directory.Exists(trainInfoReportPath_0))
                {
                    Globals.DeleteDirectory(trainInfoReportPath_0, saveDay);
                }

                if (Globals.systemParam.deviceCount > 1)
                {

                    if (Directory.Exists(alarmReportPath_1))
                    {
                        Globals.DeleteDirectory(alarmReportPath_1, saveDay);

                        Globals.DeleteFile(saveReportPath_1, saveDay);
                    }

                    if (Directory.Exists(saveReportPath_1))
                    {
                        Globals.DeleteDirectory(saveReportPath_1, saveDay);
                    }

                    if (Directory.Exists(trainInfoReportPath_1))
                    {
                        Globals.DeleteDirectory(trainInfoReportPath_1, saveDay);
                    }
                }

            }
            catch (Exception ex)
            {
                Globals.Log("删除文件和文件夹失败！" + ex.ToString());
            }
        }


        private void takePicBtn_Click(object sender, EventArgs e)
        {
            //string filePath = "1.xml";
            //IndexListInfo indexList = new IndexListInfo();
            //Globals.ReadInfoXml<IndexListInfo>(ref indexList, filePath);


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
            StopRecord();
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
            StartRecording();
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



        private void StartRecording()
        {
            try
            {
                //Stopwatch stopwatch = new Stopwatch();
                //stopwatch.Start();

                //获取当前时间戳
                long dateTimeNow = TicksTimeConvert.GetNowTicks13();
                DateTime aa = TicksTimeConvert.Ticks132LocalTime(dateTimeNow);  //时间戳转本地时间


                saveReportPath[0] = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + aa.ToString("yyyy_MM_dd");
                alarmReportPath[0] = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + aa.ToString("yyyy_MM_dd");
                trainInfoReportPath[0] = Globals.RootSavePath + "\\" + "TrainInfoReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_0 + "\\" + aa.ToString("yyyy_MM_dd");

                if (Globals.systemParam.deviceCount > 1)
                {
                    saveReportPath[1] = Globals.RootSavePath + "\\" + "SaveReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_1 + "\\" + aa.ToString("yyyy_MM_dd");
                    alarmReportPath[1] = Globals.RootSavePath + "\\" + "AlarmReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_1 + "\\" + aa.ToString("yyyy_MM_dd");
                    // trainInfoReportPath[1] = Globals.RootSavePath + "\\" + "TrainInfoReport" + "\\" + Globals.systemParam.stationName + "\\" + Globals.systemParam.deviceName_1 + "\\" + aa.ToString("yyyy_MM_dd");
                }

                string strTime = aa.ToString("yyyyMMdd_HHmmss_fff");
                saveReportPath[0] += "\\" + strTime;
                alarmReportPath[0] += "\\" + strTime;


                //判断文件夹是否存在，如果不存在，新建文件夹
                if (!Directory.Exists(saveReportPath[0]))
                {
                    Directory.CreateDirectory(saveReportPath[0]);
                }

                //判断文件夹是否存在，如果不存在，新建文件夹
                if (!Directory.Exists(trainInfoReportPath[0]))
                {
                    Directory.CreateDirectory(trainInfoReportPath[0]);
                }

                if (Globals.systemParam.deviceCount > 1)
                {
                    saveReportPath[1] += "\\" + strTime;
                    alarmReportPath[1] += "\\" + strTime;

                    //判断文件夹是否存在，如果不存在，新建文件夹
                    if (!Directory.Exists(saveReportPath[1]))
                    {
                        Directory.CreateDirectory(saveReportPath[1]);
                    }

                    ////判断文件夹是否存在，如果不存在，新建文件夹
                    //if (!Directory.Exists(trainInfoReportPath[1]))
                    //{
                    //    Directory.CreateDirectory(trainInfoReportPath[1]);
                    //}
                }



                isTrainStart = true;
                SetButtonImg(startRecordBtn, "开始录制-line(1).png");

                info.Text = "当日过车信息：正在过车";
                info.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                Globals.Log("开始录制失败" + ex.ToString());
            }

        }

        private void GetTmp()
        {


        }

        /// <summary>
        /// 停止录制
        /// </summary>
        private void StopRecord()
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
                try
                {


                    //等待过车数据缓存完成后开始进行数据分析及存盘
                    if (!isSavingIrImg_0 & !isSavingIrImg_1)
                    {

                        AnalysisData();

                        for (int i = 0; i < 20; i++)
                        {
                            cacheData_0[i].Clear();
                        }

                        for (int i = 0; i < 20; i++)
                        {
                            cacheData_1[i].Clear();
                        }

                        cacheDataCount_0 = 0;
                        cacheDataCount_1 = 0;
                        isCopyOpImage[0] = false;
                        if (Globals.systemParam.deviceCount > 1)
                        {
                            isCopyOpImage[1] = false;
                        }
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Globals.Log("停止录制失败" + ex.ToString());
                }
                Thread.Sleep(5);
            }

            info.Text = "当日过车信息";
            info.ForeColor = Color.FromArgb(224, 224, 224);

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
            //Console.WriteLine("lRealHandle" + lRealHandle);
            //Console.WriteLine("dwDataType" + dwDataType);
            //Console.WriteLine("dwBufSize" + dwBufSize);
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
        /// 设备1可见光实时预览数据回调
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack_OP_1(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
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
        /// 设备1红外实时预览数据回调
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack_IR_1(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            // Console.WriteLine("回调函数0：" + System.DateTime.Now);
        }



        /// <summary>
        /// 设备0登录回调函数
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


        /// <summary>
        /// 设备1登录回调函数
        /// </summary>
        /// <param name="lUserID"></param>
        /// <param name="dwResult"></param>
        /// <param name="lpDeviceInfo"></param>
        /// <param name="pUser"></param>
        public void cbLoginCallBack_1(int lUserID, int dwResult, IntPtr lpDeviceInfo, IntPtr pUser)
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
            try
            {


                if (Globals.systemParam.showToolBox == false)
                {
                    fmonitor.GetControl("uiPanel1").Height = 1;
                }
                else
                {
                    fmonitor.GetControl("uiPanel1").Height = 49;
                }

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
                    columnHeader.Width = trainListView.Width * 2 / 10;
                    columnHeader.TextAlign = HorizontalAlignment.Center;
                    trainListView.Columns.Add(columnHeader);

                    ColumnHeader columnHeader1 = new ColumnHeader();
                    columnHeader1.Text = "过车时间";
                    columnHeader1.Width = trainListView.Width * 4 / 10;
                    columnHeader1.TextAlign = HorizontalAlignment.Center;
                    trainListView.Columns.Add(columnHeader1);


                    ColumnHeader columnHeader2 = new ColumnHeader();
                    columnHeader2.Text = "是否报警";
                    columnHeader2.Width = trainListView.Width * 5 / 13;
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

                QueryPerformanceFrequency(ref m_lgTCPU);        //CPU hz  
                dealTrainThread = new Thread(new ThreadStart(DealTrain));
                dealTrainThread.Priority = ThreadPriority.AboveNormal;// .Highest;

                try
                {
                    serialPort1.ReceivedBytesThreshold = 1;
                    serialPort1.Open();
                    serialPort1.DiscardInBuffer();
                    serialPort1.DiscardOutBuffer();
                    dealTrainThread.Start();

                }
                catch
                {
                    MessageBox.Show(" Cannot open serial port！");
                }


                //uiNavBar1.SelectedIndex = 0;
                //StartPrewview();

                //登录和预览线程
                MonitorDeviceThread = new Thread(MonitorDevice);
                MonitorDeviceThread.IsBackground = true;
                MonitorDeviceThread.Start();

                MonitorDeviceOnlineThread = new Thread(MonitorDeviceOnline);
                MonitorDeviceOnlineThread.IsBackground = true;
                MonitorDeviceOnlineThread.Start();

                ////登录设备1
                //LoginDevice(1, Globals.systemParam.ip_1, Globals.systemParam.username_1, Globals.systemParam.psw_1, Globals.systemParam.port_1, cbLoginCallBack_1);
                //Prewview(1, 0, 1, RealDataCallBack_OP_1, false);//预览可见光图像
                //Prewview(1, 1, 2, RealDataCallBack_IR_1, false);//预览红外图像


                //新建获取红外图像和温度数据线程--设备0
                GetImageDataThread = new Thread(GetImage_0);
                GetImageDataThread.IsBackground = true;
                GetImageDataThread.Priority = ThreadPriority.Highest;
                GetImageDataThread.Start();

                ////新建获取红外图像和温度数据线程--设备1
                //GetImageDataThread = new Thread(GetImage_1);
                //GetImageDataThread.IsBackground = true;
                //GetImageDataThread.Priority = ThreadPriority.Highest;
                //GetImageDataThread.Start();


                //新建保存可见光图像线程--设备0
                SaveOPImageThread = new Thread(SaveOPImage_0);
                SaveOPImageThread.IsBackground = true;
                SaveOPImageThread.Start();


                ////新建保存可见光图像线程--设备1
                //SaveOPImageThread = new Thread(SaveOPImage_1);
                //SaveOPImageThread.IsBackground = true;
                //SaveOPImageThread.Start();

                //监测来车事件线程
                MonitorTrainComingThread = new Thread(MoitorTrainComing_0);
                MonitorTrainComingThread.IsBackground = true;
                MonitorTrainComingThread.Start();

                //监测过车事件线程
                MonitorTrainLeaveThread = new Thread(MoitorTrainLeave_0);
                MonitorTrainLeaveThread.IsBackground = true;
                MonitorTrainLeaveThread.Start();

            }
            catch (Exception ex)
            {
                Globals.Log("加载窗体失败" + ex.ToString());
            }
        }

        private void MonitorDeviceOnline()
        {
            while (true)
            {
                try
                {


                    if (isDeviceDetected)
                    {
                        UInt32 dwReturn = 0;
                        Int32 nSize = Marshal.SizeOf(m_struDeviceCfg);
                        IntPtr ptrDeviceCfg = Marshal.AllocHGlobal(nSize);
                        Marshal.StructureToPtr(m_struDeviceCfg, ptrDeviceCfg, false);
                        if (!CHCNetSDK.NET_DVR_GetDVRConfig(mUserIDs[0], CHCNetSDK.NET_DVR_GET_DEVICECFG_V40, -1, ptrDeviceCfg, (UInt32)nSize, ref dwReturn))
                        {
                            iLastErr = CHCNetSDK.NET_DVR_GetLastError();
                            string strErr = "NET_DVR_GET_DEVICECFG_V40 failed, error code= " + iLastErr;

                            Globals.Log(strErr);
                            ////获取设备参数失败，输出错误号 Failed to get the basic parameters of device and output the error code
                            //MessageBox.Show(strErr);

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

                            isDeviceDetected = false;
                            //登录和预览线程
                            MonitorDeviceThread = new Thread(MonitorDevice);
                            MonitorDeviceThread.IsBackground = true;
                            MonitorDeviceThread.Start();


                        }
                        else
                        {
                            m_struDeviceCfg = (CHCNetSDK.NET_DVR_DEVICECFG_V40)Marshal.PtrToStructure(ptrDeviceCfg, typeof(CHCNetSDK.NET_DVR_DEVICECFG_V40));
                        }
                        Marshal.FreeHGlobal(ptrDeviceCfg);
                    }
                }
                catch (Exception ex)
                {
                    Globals.Log("检测设备是否在线失败" + ex.ToString());
                }
                Thread.Sleep(1000 * 60);
            }
        }

        private void MonitorDevice()
        {
            try
            {
                while (!isDeviceDetected) // 当设备未检测到时持续循环
                {
                    LoginDevice(0, Globals.systemParam.ip_0, Globals.systemParam.username_0, Globals.systemParam.psw_0, Globals.systemParam.port_0, cbLoginCallBack_0);
                    if (!isDeviceDetected)
                    {
                        Thread.Sleep(5000); // 等待5秒后再次尝试登录
                    }
                }

                Prewview(0, 0, 1, RealDataCallBack_OP_0, false);//预览可见光图像
                Prewview(0, 1, 2, RealDataCallBack_IR_0, false);//预览红外图像
            }
            catch (Exception ex)
            {
                Globals.Log("检测设备失败" + ex.ToString());
            }
        }

        private void Timer1_Tick(object sender, EventArgs e)
        {
            DeleteDirectoryAndFile(30);
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
            if (pageIndex == PAGE_INDEX)
            {
                //FormVehicleData.selectType = -1;
                //PictureBox pic = (PictureBox)fmonitor.GetControl("pics[0]");
                //pic.Refresh();

            }
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

                trainInfoReportPath.Add(null);


                isAlarm.Add(false);

                isCopyOpImage.Add(false);

            }

            for (int i = 0; i < cacheData_0.Length; i++)
            {
                cacheData_0[i] = new List<byte>();
            }
            for (int i = 0; i < cacheData_1.Length; i++)
            {
                cacheData_1[i] = new List<byte>();
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
            try
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
                        isDeviceDetected = false;
                        return;
                    }
                    else
                    {
                        isDeviceDetected = true;
                        //登录成功
                        //MessageBox.Show("Login Success!");
                    }
                }
            }
            catch (Exception ex)
            {
                Globals.Log("注册设备" + ipAddress + "失败" + ex.ToString());
            }
        }

        private void SerialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            byte[] rece_buffer = new byte[0x100];
            int i = 0;
            try
            {
                int readbytes = serialPort1.Read(rece_buffer, 0, 0x12);

                if (readbytes > 0)
                {
                    Array.Copy(rece_buffer, 0, data_buffer, data_count, readbytes);
                    data_count += readbytes;
                    if (data_count > 0x5000) { data_count = 0; deal_point = 0; }
                    while (data_count > (deal_point + 5))
                    {
                        if (data_buffer[deal_point] == 0)
                        {
                            if (data_buffer[deal_point + 1] == 1)
                            {
                                /*
                                     for (i = deal_point ; i < (deal_point+6); i++)
                                     {
                                         textBox1.Text +=    data_buffer[i].ToString();
                                         textBox1.Text += " ";
                                     }
                                     textBox1.Text += "\r\n";  
                                 */
                                deal_point += 6;
                                readN1Event.Set();
                            }
                            else if (data_buffer[deal_point + 1] == 2)
                            {
                                m_lgTemp2 = (UInt64)(data_buffer[deal_point + 2] * 0x1000000 + data_buffer[deal_point + 3] * 0x10000 + data_buffer[deal_point + 4] * 0x100 + data_buffer[deal_point + 5]);
                                //  textBox1.Text += "2: " +  m_lgTemp2.ToString() ;
                                //  textBox1.Text += " ";
                                //  textBox1.Text += "\r\n";

                                deal_point += 6;
                                readN2Event.Set();
                            }
                            else if (data_buffer[deal_point + 1] == 3)
                            {
                                m_lgTemp3 = (UInt64)(data_buffer[deal_point + 2] * 0x1000000 + data_buffer[deal_point + 3] * 0x10000 + data_buffer[deal_point + 4] * 0x100 + data_buffer[deal_point + 5]);
                                //textBox1.Text += "3: " + m_lgTemp3.ToString();
                                //textBox1.Text += " ";
                                ////
                                deal_point += 6;
                                readN3Event.Set();
                            }
                            else deal_point++;
                        }
                        else deal_point++;
                    }
                }
                /*
                  case 0x3:
                     for (i = 0; i < 6; i++)
                      {
                          textBox1.Text += rece_buffer[i].ToString();
                          textBox1.Text += " ";
                      }
                      textBox1.Text += "\r\n";
                      break;
                      // QueryPerformanceCounter(ref m_lgTemp3);
                      m_lgTemp3 = (UInt64)(rece_buffer[4] + rece_buffer[3] * 0x100 + rece_buffer[2] * 0x10000 + rece_buffer[1]*0x1000000);
                      textBox1.Text += "3:  " + m_lgTemp3.ToString() + " \r\n";
                      //readN3Event.Set();
                      break;
                  */
            }
            catch { }
        }

        private void SerialPort1_ErrorReceived(object sender, System.IO.Ports.SerialErrorReceivedEventArgs e)
        {
            serialPort1.DiscardInBuffer();
        }

        private void Timer2_Tick(object sender, EventArgs e)
        {
            if (!kjFlag)
            {
                data_count = 0;
                deal_point = 0;
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

            try
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
            catch (Exception ex)
            {
                Globals.Log("预览设备"  + deviceNum + "失败" + ex.ToString());
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
            try
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
            catch (Exception ex)
            {
                Globals.Log("添加picturebox失败!" + ex.ToString());
            }
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
