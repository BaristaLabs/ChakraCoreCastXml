namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public interface IMangled
    {
        [XmlAttribute("mangled")]
        string Mangled
        {
            get;
            set;
        }
    }
}
