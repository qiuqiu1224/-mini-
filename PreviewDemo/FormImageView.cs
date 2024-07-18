using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sunny.UI;

namespace PreviewDemo
{
    public partial class FormImageView : UIForm
    {
        int index;
        public FormImageView()
        {
            // Console.WriteLine("FormBrowse.ImageToShow" + FormBrowse.ImageToShow);
            InitializeComponent();
        }

        private void PictureBox1_LoadCompleted(object sender, AsyncCompletedEventArgs e)
        {

        }

        private void FormImageView_Load(object sender, EventArgs e)
        {
           

        }

        public void uiForm_MouseWheel(Object sender, MouseEventArgs e)
        {

            if (e.Delta < 0)//鼠标滚轮向下滚动
            {
                if (index < Globals.fileInfos.Length - 1)
                {
                    index++;
                }
            }
            else
            {
                if (index > 0)
                {
                    index--;
                }

            }

            //Console.WriteLine(index);

            if (index >= 0 && index < Globals.fileInfos.Length)
            {
               
                this.Text = Globals.fileInfos[index].Name;
                imageDisplay.Image = Image.FromFile(Globals.fileInfos[index].FullName);
            }

        }

        private void FormImageView_Load_1(object sender, EventArgs e)
        {
            Console.WriteLine("FormBrowse.ImageToShow" + FormBrowse.ImageToShow);
            index = Convert.ToInt32(FormBrowse.ImageToShow);

            imageDisplay.Image = Image.FromFile(Globals.fileInfos[index].FullName);
           
            this.Text = Globals.fileInfos[index].Name;

            this.MouseWheel += new System.Windows.Forms.MouseEventHandler(this.uiForm_MouseWheel);//为uiPanel1添加鼠标滚动事件
        }
    }
}
