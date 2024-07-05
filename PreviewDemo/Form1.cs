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

namespace PreviewDemo
{
    public partial class Form1 : UIForm
    {

        const uint DISPLAYWND_GAP = 1;//监控画面的间隙
        const uint DISPLAYWND_MARGIN_LEFT = 1;//监控画面距离左边控件的距离
        const uint DISPLAYWND_MARGIN_TOP = 1; //监控画面距离上边的距离
        const int PAGE_INDEX = 1000;
        const Int32 IR_VEDIO_WIDTH = 768;//红外图像视频帧宽度
        const Int32 IR_VEDIO_HEIGHT = 576;//红外图像视频帧高度
        const Int32 IR_TEMP_WIDTH = 388;//红外温度帧宽度
        const Int32 IR_TEMP_HEIGHT = 284;//红外温度帧高度

        Color PIC_CLICKED_COLOR = Color.FromArgb(128, 128, 255);
        Color PIC_UNCLICKED_COLOR = Color.FromArgb(45, 45, 53);
        private PictureBox[] pics;//显示图像控件
        private UIPage fmonitor;//监控界面
        private UIPage fbrowse;//浏览界面
        // private UIPanel pixUIPanel;//容纳PictureBox的Panel
        //public static TransparentLabel[] labels;//图像上面标注控件,背景透明

        UISymbolButton startPrewviewBtn, stopPrewviewBtn, startRecordBtn, stopRecordBtn,
            mouseFollowBtn, takePicBtn, drawRectBtn, drawCircleBtn, deleteAllDrawBtn;
        private bool isStartPrewview = false;//开始采集标志
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

        private bool m_bInitSDK = false;


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

            uiNavBar1.Nodes.Add("图像浏览");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[1], 61502);

            ////添加图像浏览界面  PAGE_INDEX + 1
            //pageIndex++;
            //fbrowse = new FormBrowse();
            //AddPage(fbrowse, pageIndex);
            //uiNavBar1.SetNodePageIndex(uiNavBar1.Nodes[1], pageIndex);

            uiNavBar1.Nodes.Add("系统设置");
            uiNavBar1.SetNodeSymbol(uiNavBar1.Nodes[2], 61459);


            //初始化数据
            initDatas();


            //初始化图像显示控件布局
            //SetFmonitorDisplayWnds((uint)Globals.systemParam.deviceCount, 2);

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

        }

        /// <summary>
        ///  开始录制视频按钮鼠标离开控件事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartRecordBtn_MouseLeave(object sender, EventArgs e)
        {
            if (!saveVideoFlag)
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

        /// <summary>
        /// 可见光相机实时预览数据回调
        /// </summary>
        /// <param name="lRealHandle"></param>
        /// <param name="dwDataType"></param>
        /// <param name="pBuffer"></param>
        /// <param name="dwBufSize"></param>
        /// <param name="pUser"></param>
        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, IntPtr pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            if (dwBufSize > 0)
            {

                Console.WriteLine("dwBufSize" + dwBufSize);
                //byte[] sData = new byte[dwBufSize];
                //Marshal.Copy(pBuffer, sData, 0, (Int32)dwBufSize);


                //if (saveImageFlag1)
                //{
                //    string strFileName = "test1.jpg";
                //    Cv2.ImWrite(strFileName, mgMatShow);
                //    saveImageFlag1 = false;
                //}


                //string str = "实时流数据.ps";
                //FileStream fs = new FileStream(str, FileMode.Create);
                //int iLen = (int)dwBufSize;
                //fs.Write(sData, 0, iLen);
                //fs.Close();


            }
        }

        /// <summary>
        /// 登录可见光相机
        /// </summary>
        /// <param name="deviceNum">设备号，从0开始</param>
        /// <param name="ipAddress">ip地址</param>
        /// <param name="userName">用户名</param>
        /// <param name="psw">密码</param>
        /// <param name="port">端口号</param>
        /// <param name="loginCallBack">登录回调函数</param>
        private void LoginOpDevice(int deviceNum, string ipAddress, string userName, string psw, string port, CHCNetSDK.LOGINRESULTCALLBACK loginCallBack)
        {

        }
        /// <summary>
        /// 登录回调函数
        /// </summary>
        /// <param name="lUserID"></param>
        /// <param name="dwResult"></param>
        /// <param name="lpDeviceInfo"></param>
        /// <param name="pUser"></param>
        public void cbLoginCallBack(int lUserID, int dwResult, IntPtr lpDeviceInfo, IntPtr pUser)
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

            uint w = (uint)(Screen.PrimaryScreen.Bounds.Width);

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
                    uint x = (real_width - colNum * display_width - DISPLAYWND_GAP * (colNum - 1)) / 2 + (display_width + DISPLAYWND_GAP) * j;

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
