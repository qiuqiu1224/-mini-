using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace PreviewDemo
{
    public class TrainInfo
    {
        /// <summary>
        /// 索引ID
        /// </summary>
        [XmlElement(Type = typeof(UInt32), ElementName = "ID")]
        public UInt32 indexID = 0;

        [XmlElement(Type = typeof(byte), ElementName = "车型")]
        public byte carType;

        [XmlElement(Type = typeof(byte), ElementName = "轴型")]
        public byte bearTypeAxle;

        [XmlElement(Type = typeof(UInt16), ElementName = "轴地址")]
        public UInt16 axleAddr;

        [XmlElement(Type = typeof(byte[]), ElementName = "轴距")]
        public byte[] axleDistance = new byte[32];



    }
    public class TrainListInfo
    {
        /// <summary>
        /// 过车信息
        /// </summary>
        [XmlElement(Type = typeof(TrainInfo), ElementName = "过车信息")]
        public List<TrainInfo> trainIndexList = new List<TrainInfo>();

        [XmlElement(Type = typeof(UInt16[]), ElementName = "轴速")]
        public UInt16[] axleSpeed = new UInt16[1024];
    }

}
