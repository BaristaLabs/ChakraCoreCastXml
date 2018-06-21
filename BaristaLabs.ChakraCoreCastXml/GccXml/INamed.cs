namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public interface INamed
    {
        [XmlAttribute("name")]
        string Name
        {
            get;
            set;
        }
    }
}
