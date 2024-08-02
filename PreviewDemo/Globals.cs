using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Forms;
using OpenCvSharp;
using System.Drawing;
using Sunny.UI;

namespace PreviewDemo
{
    class Globals
    {

        public static SystemParam systemParam = new SystemParam();

        public static string ImageDirectoryPath = Application.StartupPath + "\\" + "Image";

        public static string AlarmImageDirectoryPath = Application.StartupPath + "\\" + "AlarmImage";

        public static string RecordDirectoryPath = Application.StartupPath + "\\" + "Record";

        public static string SDKLogPath = Application.StartupPath + "\\" + "SdkLog\\";

        public static string systemXml = Application.StartupPath + "\\SystemSetting.xml";

        public static string RootSavePath = "C:" + "\\" + "HIK";

        const Int32 TEMP_WIDTH = 640;//红外图像宽度
        const Int32 TEMP_HEIGHT = 512;//红外图像宽度


        public static FileInfo[] fileInfos;

        public static DirectoryInfo startPathInfo = new DirectoryInfo(Application.StartupPath);


        public static bool ReadInfoXml<T>(ref T Info, string fileName)
        {
            try
            {
                FileStream fs = new FileStream(fileName, FileMode.Open);

                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(T));
                    Info = (T)serializer.Deserialize(fs);
                    fs.Close();
                    fs.Dispose();

                    return true;
                }
                catch (Exception e1)
                {
                    Trace.WriteLine("ReadInfoXml-1" + e1.Message);
                    fs.Close();
                    fs.Dispose();

                    return false;
                }
            }
            catch (Exception e2)
            {
                Trace.WriteLine("ReadInfoXml-2" + e2.Message);

                return false;
            }
        }

        public static void WriteInfoXml<T>(T Info, string fileName)
        {
            try
            {
                TextWriter myWriter = new StreamWriter(fileName);

                try
                {
                    XmlSerializer mySerializer = new XmlSerializer(typeof(T));
                    mySerializer.Serialize(myWriter, Info);
                    myWriter.Close();
                    myWriter.Dispose();
                }
                catch (Exception e1)
                {
                    Console.WriteLine("WriteInfoXml-1" + e1.Message);
                    myWriter.Close();
                    myWriter.Dispose();
                }
            }
            catch (Exception e2)
            {
                Console.WriteLine("WriteInfoXml-2" + e2.Message);
            }
        }


        public static void Log(string str)
        {
            string folderName = Application.StartupPath + "\\Log\\";
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }
            DateTime dateTime = DateTime.Now;
            string fileName = dateTime.Year.ToString() + dateTime.Month.ToString("D2") + dateTime.Day.ToString("D2") + ".txt";

            str += " " + dateTime.Hour.ToString("D2") + ":" + dateTime.Minute.ToString("D2") + ":" + dateTime.Second.ToString("D2");
            str += "\r\n";
            try
            {
                FileStream fileStream = new FileStream(folderName + fileName, FileMode.Append);
                StreamWriter streamWriter = new StreamWriter(fileStream);
                streamWriter.WriteLine(str);
                streamWriter.Close();
                streamWriter.Dispose();
                fileStream.Close();
                fileStream.Dispose();
            }
            catch (Exception e)
            { }
        }

        public static void SortFolderByCreateTime(ref FileInfo[] files)
        {
            //倒叙排序，日期最新的在前面  10月19  10月18 10月17
            Array.Sort(files, (FileInfo x, FileInfo y) =>
             y.LastWriteTime.CompareTo(x.LastWriteTime));
        }

        public static float[] TempBytesToTempFloats(byte[] tempBytes, int tempWidth, int tempHeight)
        {
            int i = 0;
            int j = 0;
            float[] tempFloats = new float[tempWidth * tempHeight];
            while ((i + 4) <= tempBytes.Length)
            {
                byte[] temp = new byte[4];
                temp[0] = tempBytes[i];
                temp[1] = tempBytes[i + 1];
                temp[2] = tempBytes[i + 2];
                temp[3] = tempBytes[i + 3];
                i += 4;

                tempFloats[j++] = (float)Math.Round(BitConverter.ToSingle(temp, 0), 1); //保留一位小数
            }
            return tempFloats;
        }

        /// <summary>
        /// 将二进制温度文件转存为温度数组
        /// </summary>
        /// <param name="tempFilePath"></param>
        /// <returns></returns>
        public static float[] GetTempFileToArray(string tempFilePath)
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

        public static void DrawCross(Mat img, OpenCvSharp.Point cor, Scalar color, int crossLine, int lineWidth)
        {
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X + crossLine, cor.Y), color, lineWidth);
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X - crossLine, cor.Y), color, lineWidth);
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X, cor.Y + crossLine), color, lineWidth);
            Cv2.Line(img, cor, new OpenCvSharp.Point(cor.X, cor.Y - crossLine), color, lineWidth);
        }

        /// <summary>
        /// 设置按钮图片
        /// </summary>
        /// <param name="btn"></param>
        /// <param name="imageName"></param>
        public static void SetButtonImg(UISymbolButton btn, string imageName)
        {
            btn.Image = Image.FromFile(Globals.startPathInfo.FullName + "\\Resources\\" + imageName);
        }

        public static void DrawCross(Graphics graphics, int x, int y, int crossLine, Pen pen)
        {
            // 绘制线条，指定起点和终点
            System.Drawing.Point startPoint = new System.Drawing.Point(x, y);
            System.Drawing.Point lefPoint = new System.Drawing.Point(x - crossLine, y);
            System.Drawing.Point rightPoint = new System.Drawing.Point(x + crossLine, y);
            System.Drawing.Point topPoint = new System.Drawing.Point(x, y - crossLine);
            System.Drawing.Point bottomPoint = new System.Drawing.Point(x, y + crossLine);
            graphics.DrawLine(pen, startPoint, lefPoint);
            graphics.DrawLine(pen, startPoint, rightPoint);
            graphics.DrawLine(pen, startPoint, topPoint);
            graphics.DrawLine(pen, startPoint, bottomPoint);
        }


        public static void DrawText(Mat img, string maxTemp, OpenCvSharp.Point cor)
        {
            Cv2.PutText(img, maxTemp.ToString(), new OpenCvSharp.Point(cor.X + 3, cor.Y + 25), OpenCvSharp.HersheyFonts.HersheySimplex, 0.8, OpenCvSharp.Scalar.LightGreen, 2);
        }

        /// <summary>
        /// 在可见光图像文件夹内选区与红外图像时间戳前后500ms以后的图像
        /// </summary>
        /// <param name="irImageName"></param>
        /// <param name="opImagePaths"></param>
        /// <returns></returns>
        public static List<string> GetOPImages(string irImageName, string[] opImagePaths)
        {
            List<string> imagePaths = new List<string>();
            string IRImageFileNameWithoutExtension = Path.GetFileNameWithoutExtension(irImageName);
            string IRImageHour = IRImageFileNameWithoutExtension.Substring(9, 2);
            string IRImageMin = IRImageFileNameWithoutExtension.Substring(11, 2);
            string IRImageSec = IRImageFileNameWithoutExtension.Substring(13, 2);
            string IRImageMillsec = IRImageFileNameWithoutExtension.Substring(16, 3);

            foreach (string fileName in opImagePaths)
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string hour = fileNameWithoutExtension.Substring(9, 2);
                string min = fileNameWithoutExtension.Substring(11, 2);
                string sec = fileNameWithoutExtension.Substring(13, 2);
                string millsec = fileNameWithoutExtension.Substring(16, 3);

                int timeDiff = Convert.ToUInt16(IRImageHour) * 60 * 60 * 1000 + Convert.ToUInt16(IRImageMin) * 60 * 1000 + Convert.ToUInt16(IRImageSec) * 1000 + Convert.ToUInt16(IRImageMillsec)
                    - Convert.ToUInt16(hour) * 60 * 60 * 1000 - Convert.ToUInt16(min) * 60 * 1000 - Convert.ToUInt16(sec) * 1000 - Convert.ToUInt16(millsec);

                if (Math.Abs(timeDiff) <= 500)
                {
                    imagePaths.Add(fileName);
                }
            }

            return imagePaths;
        }

        public static float[,] ChangeTempToArray(float[] temp, int width, int height)
        {
            float[,] realTemps = new float[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    realTemps[i, j] = temp[j * width + i];

                }
            }

            return realTemps;
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

        public static void DrawCrossLine(Graphics g, float startX, float startY, Pen pen, int lineLength)
        {
            g.DrawLine(pen, startX, startY, startX + lineLength, startY);
            g.DrawLine(pen, startX, startY, startX - lineLength, startY);
            g.DrawLine(pen, startX, startY, startX, startY + lineLength);
            g.DrawLine(pen, startX, startY, startX, startY - lineLength);
        }

    }
}
