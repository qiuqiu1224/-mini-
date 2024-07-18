using a8sdk;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Threading;

namespace Test
{
    public partial class Form1 : Form
    {
        A8SDK a8;
        tstRtmp rtmp = new tstRtmp();
        Thread thPlayer;
        int pt = 2;
        a8sdk.A8SDK.area_pos area_data;
        a8sdk.A8SDK.area_temp area_Temp;
        public Form1()
        {
            InitializeComponent();
            A8SDK.SDK_initialize();

            
        }
        
        private void button_play_Click(object sender, EventArgs e)
        {
            button_play.Enabled = false;
            a8 = new A8SDK("192.168.100.2");
            int i;
            a8sdk.A8SDK.area_pos area_data;
            area_data.enable = 1;
            area_data.height = 191;
            area_data.width = 255;
            area_data.x = 0;
            area_data.y = 0;
            i = a8.Set_area_pos(0, area_data);

            if (thPlayer != null)
            {
                rtmp.Stop();

                thPlayer = null;
            }
            else
            {
                thPlayer = new Thread(DeCoding);
                thPlayer.IsBackground = true;
                thPlayer.Start();
                button_play.Text = "停止播放";
                button_play.Enabled = true;
            }
        }
        // 获取当前的Dispatcher对象
        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        private delegate void UpdateUI(Bitmap bitmap);
        private void UpdateImage(Bitmap bitmap)
        {
            dispatcher.Invoke(new UpdateUI(Updatepic), bitmap);
        }
        private void Updatepic(Bitmap bitmap)
        {
            using (Graphics gfx = Graphics.FromImage(bitmap))
            {
                // 设置文本样式、颜色等属性
                Font font = new Font("Arial", 15, FontStyle.Bold);
                Brush brush;
                string text;
                PointF point;
                //int i;
                //i = a8.Get_area_temp(0, out area_Temp);
                //if (i == 0)
                //{
                //    // 将数字写入位图
                //    brush = Brushes.Red;
                //    text = ((float)area_Temp.max_temp / 10).ToString();
                //    point = new PointF(area_Temp.max_temp_x * pt, area_Temp.max_temp_y * pt);
                //    gfx.DrawString(text, font, brush, point);

                //    brush = Brushes.Blue;
                //    text = ((float)area_Temp.min_temp / 10).ToString();
                //    point = new PointF(area_Temp.min_temp_x * pt, area_Temp.min_temp_y * pt);
                //    gfx.DrawString(text, font, brush, point);

                //}
                // 将数字写入位图
                brush = Brushes.Red;
                text = ((float)100 / 10).ToString();
                point = new PointF(20 * pt, 80 * pt);
                gfx.DrawString(text, font, brush, point);

            }
            this.pictureBox1.Image = bitmap;
        }
        /// <summary>
        /// 播放线程执行方法
        /// </summary>
        [Obsolete]
        private unsafe void DeCoding()
        {
            try
            {
                Console.WriteLine("DeCoding run...");
                Bitmap oldBmp = null;


                // 更新图片显示
                tstRtmp.ShowBitmap show = (bmp) =>
                {
                    if (bmp != null)
                    {
                        UpdateImage(bmp);

                    }
                    //if (oldBmp != null)
                    //{
                    //    oldBmp.Dispose();
                    //}
                    //oldBmp = bmp;
                };
                rtmp.Start(show, "rtsp://192.168.100.2/webcam");
                Thread.Sleep(2);
                //rtmp.StartSave(show, "rtsp://192.168.100.2/webcam", "D://123//1.mp4");
                //rtmp.Start_save(show, "rtsp://192.168.100.2/webcam", "D://123//1.mp4");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                rtmp.Stop();
                Console.WriteLine("DeCoding exit");
                this.Invoke(new MethodInvoker(() =>
                {
                    button_play.Text = "停止播放";
                    button_play.Enabled = true;
                }));
            }
        }

    }
    

}
