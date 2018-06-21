namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public class GccXmlCvQualifiedType : GccXmlElement, ITyped
    {
        [XmlAttribute("type")]
        public string Type
        {
            get;
            set;
        }
    }
}
