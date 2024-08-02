using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;


namespace ConsoleApp1
{
    class Globals
    {

        public static Test test = new Test();

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
    }
}
