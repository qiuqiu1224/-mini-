using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConsoleApp1
{
    public class Test
    {
        public Test() { }
        [XmlElement(ElementName = "是否报警")]
        public int[] aa = new int[32];
    }


}
