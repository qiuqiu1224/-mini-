using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PreviewDemo
{
    public class TrainIndex
    {
        /// <summary>
        /// 索引ID
        /// </summary>
        [XmlElement(Type = typeof(UInt32), ElementName = "ID")]
        public UInt32 IndexID = 0;
        /// <summary>
        /// 过车时间
        /// </summary>
        [XmlElement(ElementName = "过车时间")]
        public string detectTime;
        /// <summary>
        /// 是否报警
        /// </summary>
        [XmlElement(ElementName = "是否报警")]
        public string alarmTemperatrue = "";
    }

    public class IndexListInfo
    {
        /// <summary>
        /// 过车信息
        /// </summary>
        [XmlElement(Type = typeof(TrainIndex), ElementName = "过车信息")]
        public List<TrainIndex> trainIndexList = new List<TrainIndex>();
    }
}
