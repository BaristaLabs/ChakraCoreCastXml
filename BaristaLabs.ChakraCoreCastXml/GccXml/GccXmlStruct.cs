namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlStruct : GccXmlElement, INamed
    {
        [XmlAttribute("name")]
        public string Name
        {
            get;
            set;
        }
    }
}
