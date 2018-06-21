namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlFile : GccXmlElement, INamed
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }
    }
}
