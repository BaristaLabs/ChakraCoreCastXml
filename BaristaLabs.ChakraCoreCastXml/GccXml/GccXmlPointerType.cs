namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlPointerType : GccXmlElement, ITyped, ISized
    {
        [XmlAttribute("type")]
        public string Type
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
