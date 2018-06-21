namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlEnumeration : GccXmlElement, INamed, IContextual, IFileBased, ISized
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute("context")]
        public string Context
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

        [XmlAttribute("size")]
        public int Size
        {
            get;
            set;
        }

        [XmlAttribute("align")]
        public int Align
        {
            get;
            set;
        }
    }
}
