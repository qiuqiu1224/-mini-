using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Forms;

namespace PreviewDemo
{
    class Globals
    {

        public static SystemParam systemParam =  new SystemParam();

        public static string ImageDirectoryPath = Application.StartupPath + "\\" + "Image";

        public static string AlarmImageDirectoryPath = Application.StartupPath + "\\" + "AlarmImage";

        public static string RecordDirectoryPath = Application.StartupPath + "\\" + "Record";

        public static string SDKLogPath =  Application.StartupPath + "\\" + "SdkLog\\";

        public static string systemXml = Application.StartupPath + "\\SystemSetting.xml";


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


    }
}
