namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public interface ISized
    {

        [XmlAttribute("size")]
        int Size
        {
            get;
            set;
        }

        [XmlAttribute("align")]
        int Align
        {
            get;
            set;
        }
    }
}
