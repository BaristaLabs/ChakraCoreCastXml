namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Xml.Serialization;

    public interface IFileBased
    {
        [XmlAttribute("location")]
        string Location
        {
            get;
            set;
        }

        [XmlAttribute("file")]
        string File
        {
            get;
            set;
        }

        [XmlAttribute("line")]
        string Line
        {
            get;
            set;
        }
    }
}
