namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlFundamentalType : GccXmlElement, INamed, ISized
    {
        [XmlAttribute("name")]
        public string Name
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
