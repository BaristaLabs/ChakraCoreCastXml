namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public interface ITyped
    {
        [XmlAttribute("type")]
        string Type
        {
            get;
            set;
        }
    }
}
