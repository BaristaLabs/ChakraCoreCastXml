namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlArgument : GccXmlElement, INamed, ITyped, IFileBased
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute("type")]
        public string Type
        {
            get;
            set;
        }

        [XmlAttribute("location")]
        public string Location
        {
            get;
            set;
        }

        [XmlAttribute("file")]
        public string File
        {
            get;
            set;
        }

        [XmlAttribute("line")]
        public string Line
        {
            get;
            set;
        }
    }
}
