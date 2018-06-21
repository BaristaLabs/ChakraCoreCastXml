namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;
    public interface IGccXmlElement
    {
        [XmlAttribute("id")]
        string Id
        {
            get;
            set;
        }
    }
}
