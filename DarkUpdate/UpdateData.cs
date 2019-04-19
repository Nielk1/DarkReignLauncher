using System.Collections.Generic;
using System.Xml.Serialization;

namespace DarkUpdate
{
    [XmlRoot("UpdateData")]
    public class UpdateData
    {
        [XmlElement("Version")]
        public string Version { get; set; }

        [XmlArray("IniFixes")]
        [XmlArrayItem("IniFix", typeof(IniFix))]
        public List<IniFix> IniFixes { get; set; }

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

    public class IniFix
    {
        [XmlAttribute("File")]
        public string File { get; set; }

        [XmlAttribute("Section")]
        public string Section { get; set; }

        [XmlAttribute("Key")]
        public string Key { get; set; }

        [XmlAttribute("Value")]
        public string Value { get; set; }
    }
}