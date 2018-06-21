namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public interface IContextual
    {
        [XmlAttribute("context")]
        string Context
        {
            get;
            set;
        }
    }
}
