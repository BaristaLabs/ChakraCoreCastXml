namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class GccXmlFunction : GccXmlElement, INamed, IContextual, IFileBased, IMangled
    {
        [XmlAttribute("returns")]
        public string Returns
        {
            get;
            set;
        }

        [XmlElement("Argument", Type=typeof(GccXmlArgument))]
        public List<GccXmlArgument> Arguments
        {
            get;
            set;
        }

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

        [XmlAttribute("mangled")]
        public string Mangled
        {
            get;
            set;
        }
    }
}
