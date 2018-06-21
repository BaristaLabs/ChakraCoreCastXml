namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlNamespace : GccXmlElement
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }

        [XmlAttribute("members")]
        public string Members
        {
            get;
            set;
        }
    }
}
