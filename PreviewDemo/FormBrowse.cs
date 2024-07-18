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

namespace PreviewDemo
{
    public partial class FormBrowse : UIPage
    {

        int colNum;//每行显示图像的个数
        int rowNum;//每列显示图像的个数
        int RIGTH_MARGIN = 20;//最右边图像距离又边界的距离
        int TOP_MARGIN = 20;//最上边图像距离上边界的距离
        int PICTURE_WIDTH = 160;//每个缩略图的宽度
        int PICTURE_HEIGHT = 160;//每个缩略图的高度
        int PICTURE_GAP = 0; //图像间隙
        const int PAGEINDEX = 2000;

        UserControl1[] userControl1s;
        public static string ImageToShow;
        private bool clickFlag = false;
        FormImageView oldImageView;

        public FormBrowse()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true); // 禁止擦除背景.
            SetStyle(ControlStyles.DoubleBuffer, true); // 双缓冲
            InitializeComponent();
        
            System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;//设置该属性 为false
        }

        private void FormBrowse_Load(object sender, EventArgs e)
        {

            int pageIndex = PAGEINDEX;
            string text;
            for (int i = 0; i < Globals.systemParam.deviceCount; i++)
            {
                if (i == 0)
                {
                    text = Globals.systemParam.ip_0;
                }
                else
                {
                    text = Globals.systemParam.ip_1;
                }
                TreeNode parent = uiNavMenu1.CreateNode(text, 61451, 24, ++pageIndex);

                string folderPath = Application.StartupPath + "\\" + Globals.systemParam.ip_0; // 替换为你的文件夹路径
                string[] subdirectoryEntries = Directory.GetDirectories(folderPath);

                foreach (string subdirectory in subdirectoryEntries)
                {
                    Console.WriteLine(Path.GetFileName(subdirectory));
                }

                uiNavMenu1.CreateChildNode(parent,"1111",++pageIndex);
                uiNavMenu1.CreateChildNode(parent, "2222", ++pageIndex);
                uiNavMenu1.CreateChildNode(parent, "3333", ++pageIndex);
                parent.ExpandAll();
            }
            //uiPanel1.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.uiPanel1_MouseWheel);//为uiPanel1添加鼠标滚动事件          

            //colNum = (uiPanel1.Width - uiScrollBar1.Width - RIGTH_MARGIN) / (PICTURE_WIDTH + PICTURE_GAP);

            //rowNum = (uiPanel1.Height - TOP_MARGIN) / (PICTURE_HEIGHT + PICTURE_GAP);

            //int a = Globals.fileInfos.Length / (colNum * rowNum);
            //if (a == 0)
            //{
            //    uiScrollBar1.Visible = false;
            //}
            //else
            //{
            //    uiScrollBar1.Visible = true;
            //    uiScrollBar1.Maximum = a;
            //}

            //userControl1s = new UserControl1[colNum * rowNum];

            //int Xpos = RIGTH_MARGIN;
            //int Ypos = TOP_MARGIN;


            //for (int i = 0; i < colNum * rowNum; i++)
            //{
            //    userControl1s[i] = new UserControl1();
            //    userControl1s[i].Width = PICTURE_WIDTH;
            //    userControl1s[i].Height = PICTURE_HEIGHT;

            //    if ((Xpos + PICTURE_WIDTH + PICTURE_GAP) > uiPanel1.Width - uiScrollBar1.Width - PICTURE_GAP) //换行
            //    {
            //        Xpos = RIGTH_MARGIN;
            //        Ypos = Ypos + PICTURE_HEIGHT + PICTURE_GAP;
            //    }

            //    userControl1s[i].Left = Xpos;
            //    userControl1s[i].Top = Ypos;
            //    userControl1s[i].pictureBox.SizeMode = PictureBoxSizeMode.StretchImage;
            //    userControl1s[i].pictureBox.Click += this.ClickImage;

            //    if (i < Globals.fileInfos.Length)
            //    {
                   
            //        userControl1s[i].pictureBox.Tag = i;
            //        userControl1s[i].pictureBox.Image = Image.FromFile(Globals.fileInfos[i].FullName);               
            //        userControl1s[i].uiLabel.Text = Globals.fileInfos[i].Name;
            //        userControl1s[i].Visible = true;

            //    }
            //    else
            //    {
            //        userControl1s[i].Visible = false;
            //    }
            //    uiPanel1.Controls.Add(userControl1s[i]);
            //    Xpos = Xpos + PICTURE_WIDTH + PICTURE_GAP;

            //    Application.DoEvents();

            //}

        }

        public void uiPanel1_MouseWheel(Object sender, MouseEventArgs e)
        {
            if ((Globals.fileInfos.Length / (rowNum * colNum)) > 0)
            {

                if (e.Delta < 0)//鼠标滚轮向下滚动
                {
                    uiScrollBar1.Value += 1;

                }
                else//鼠标滚轮向上滚动
                {
                    uiScrollBar1.Value -= 1;
                }
             
                try
                {
                    //Console.WriteLine("向下滚动");
                    //Console.WriteLine("uiScrollBar1.Value" + uiScrollBar1.Value);

                    for (int i = 0; i < userControl1s.Length; i++)
                    {
                        if (uiScrollBar1.Value * rowNum * colNum + i >= Globals.fileInfos.Length)
                        {                         
                            userControl1s[i].Visible = false;
                        }
                        else
                        {                           
                            userControl1s[i].pictureBox.Image = Image.FromFile(Globals.fileInfos[uiScrollBar1.Value * rowNum * colNum + i].FullName);
                            userControl1s[i].Visible = true;
                            userControl1s[i].pictureBox.Tag = uiScrollBar1.Value * rowNum * colNum + i;
                            userControl1s[i].uiLabel.Text = Globals.fileInfos[uiScrollBar1.Value * rowNum * colNum + i].Name;
                     
                        }

                        if ((i + 1) % colNum == 0)//按行刷新显示
                        {
                            Application.DoEvents();
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("加载图像异常" + ex.ToString());
                }
                finally
                {
                    GC.Collect();
                }
            }
        }

        private void UiScrollBar1_ValueChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < userControl1s.Length; i++)
            {
                if (uiScrollBar1.Value * rowNum * colNum + i >= Globals.fileInfos.Length)
                {                  
                    userControl1s[i].Visible = false;
                }
                else
                {
                    
                    userControl1s[i].pictureBox.Image = Image.FromFile(Globals.fileInfos[uiScrollBar1.Value * rowNum * colNum + i].FullName);
                    userControl1s[i].Visible = true;
                    userControl1s[i].pictureBox.Tag = uiScrollBar1.Value * rowNum * colNum + i;                  
                    userControl1s[i].uiLabel.Text = Globals.fileInfos[uiScrollBar1.Value * rowNum * colNum + i].Name;

                }

                if ((i + 1) % colNum == 0)
                {
                    Application.DoEvents();
                }

            }
        }

        private void ClickImage(Object sender, System.EventArgs e)
        {
            if (clickFlag == true)
            {
                oldImageView.Close();
            }
            ImageToShow = ((System.Windows.Forms.PictureBox)sender).Tag.ToString();
           
            FormImageView formImageView = new FormImageView();

            oldImageView = formImageView;
            formImageView.Show();

            clickFlag = true;
        }

        private void UiNavMenu1_MenuItemClick(TreeNode node, NavMenuItem item, int pageIndex)
        {
            bool chooseChildNode = false;
            uiScrollBar1.Value = 0;
          
            string directoryPath = Globals.AlarmImageDirectoryPath + 0;

            switch (pageIndex)
            {
                case PAGEINDEX + 2:
                    chooseChildNode = true;
                    directoryPath = Globals.AlarmImageDirectoryPath + 0;
                    break;
                case PAGEINDEX + 3:
                    chooseChildNode = true;
                    directoryPath = Globals.ImageDirectoryPath + 0;
                    break;
                case PAGEINDEX + 5:
                    chooseChildNode = true;
                    directoryPath = Globals.AlarmImageDirectoryPath + 1;
                    break;
                case PAGEINDEX + 6:
                    chooseChildNode = true;
                    directoryPath = Globals.ImageDirectoryPath + 1;
                    break;

            }

            if (chooseChildNode)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(directoryPath);
                Globals.fileInfos = dirInfo.GetFiles();
                //Globals.fileInfos = dirInfo.GetFiles("*.*");
                Globals.SortFolderByCreateTime(ref Globals.fileInfos);

                if (Globals.fileInfos.Length != 0)
                {

                    for (int i = 0; i < colNum * rowNum; i++)
                    {
                        if (i < Globals.fileInfos.Length)
                        {
                            userControl1s[i].pictureBox.Tag = i;
                            userControl1s[i].pictureBox.Image = Image.FromFile(Globals.fileInfos[i].FullName);

                            userControl1s[i].uiLabel.Text = Globals.fileInfos[i].Name;
                            userControl1s[i].Visible = true;

                        }
                        else
                        {
                            userControl1s[i].Visible = false;
                        }

                        if ((i + 1) % colNum == 0)
                        {
                            Application.DoEvents();
                        }

                    }


                    int a = Globals.fileInfos.Length / (colNum * rowNum);
                    if (a == 0)
                    {
                        uiScrollBar1.Visible = false;
                    }
                    else
                    {
                        uiScrollBar1.Visible = true;
                        uiScrollBar1.Maximum = a;
                    }

                }

                else
                {
                    for (int i = 0; i < colNum * rowNum; i++)
                    {
                        userControl1s[i].Visible = false;
                    }
                }
            }

        }

        private void FormBrowse_Initialize(object sender, EventArgs e)
        {
            
        }
    }
}
