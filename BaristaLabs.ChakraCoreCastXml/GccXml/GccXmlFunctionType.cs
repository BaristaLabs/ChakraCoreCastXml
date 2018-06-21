namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    public class GccXmlFunctionType : GccXmlElement
    {
        [XmlAttribute("returns")]
        public string Returns
        {
            get;
            set;
        }

        [XmlElement("Argument", Type = typeof(GccXmlArgument))]
        public List<GccXmlArgument> Arguments
        {
            get;
            set;
        }
    }
}
