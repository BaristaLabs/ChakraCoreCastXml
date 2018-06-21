namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public abstract class GccXmlElement : IGccXmlElement
    {
        [XmlAttribute("id")]
        public string Id
        {
            get;
            set;
        }
    }
}
