namespace BaristaLabs.ChakraCoreCastXml.Config
{
    using System.Collections.Generic;
    using System.Xml.Linq;

    public class ExportExtensionRule
    {
        public ExportExtensionRule()
        {
            this.Extensions = new List<XObject>();
        }

        public string FunctionName
        {
            get;
            set;
        }

        public IList<XObject> Extensions
        {
            get;
            set;
        }
    }
}
