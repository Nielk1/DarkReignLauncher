using System.Collections.Generic;
using System.Xml.Serialization;

namespace DarkUpdate
{
    [XmlRoot("UpdateData")]
    public class UpdateData
    {
        //[XmlElement("BaseFile")]
        [XmlArray("BaseFiles")]
        [XmlArrayItem("File", typeof(string))]
        public List<string> BaseFiles { get; set; }

        [XmlArray("DeleteFiles")]
        [XmlArrayItem("File", typeof(string))]
        public List<string> DeleteFiles { get; set; }

        [XmlArray("Modules")]
        [XmlArrayItem("Module", typeof(string))]
        public List<string> Modules { get; set; }

        [XmlArray("OldModules")]
        [XmlArrayItem("Module", typeof(string))]
        public List<string> OldModules { get; set; }
    }
}