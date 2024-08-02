using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
  
    class Program
    {
       
        static void Main(string[] args)
        {

            int[] a = new int[32];

            a[0] = 1;
            a[2] = 2;
            Globals.test.aa = a;

            Globals.WriteInfoXml<Test>(Globals.test,"test.xml");
            
            //List<int> numbers = new List<int> { 3, 5, 1,1, 4, 2 };

            //// 使用Min获取最小值
            //int min = numbers.Min();

            //// 使用Where和IndexOf获取最小值的索引
            //int index = numbers.IndexOf(min);

            //Console.WriteLine($"最小值: {min}");
            //Console.WriteLine($"最小值位置: {index}");

            ////获取当前时间
            //DateTime dateTimeNow = TicksTimeConvert.GetNowLocalTime();
            ////当前时间转时间戳13位  到毫秒
            //long time = TicksTimeConvert.LocalTime2Ticks13(dateTimeNow);

            //long time1 = TicksTimeConvert.GetNowTicks13();

            //byte[] bb = new byte[100];

            ////时间戳转字节数组
            //bb = TicksTimeConvert.TimestampToBytes(time1);
            ////字节数组转时间戳
            //long c = TicksTimeConvert.BytesToTimestamp(bb);

            //////时间戳转本地时间
            //DateTime aa = TicksTimeConvert.Ticks132LocalTime(time1);
            //byte[] byteArray = new byte[] { 0x41, 0x48, 0x00, 0x00 }; // 示例字节数组
            //float[] floatArr = new float[byteArray.Length / 4];

            //Buffer.BlockCopy(byteArray, 0, floatArr, 0, byteArray.Length);
            //float floatValue = BitConverter.ToSingle(byteArray, 0);

            //byte[] byteTemp = new byte[4] { 0x76, 0x2D, 0xE3, 0x41 };
            //float fTemp = BitConverter.ToSingle(byteTemp, 0);

            //int decimalNumber = 1000;
            //int number = 255;

            //byte[] bytes = BitConverter.GetBytes(decimalNumber);
            //int aa = BitConverter.ToInt32(bytes, 0);

            //int a = decimalNumber / 256;
            //int b = decimalNumber % 256;

            //a = decimalNumber >> 8;
            //b = decimalNumber & 0XFF;

            //int c = (a << 8 ) + b ;


            Console.ReadLine();

        }
    }
}
