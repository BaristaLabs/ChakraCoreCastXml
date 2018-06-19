namespace BaristaLabs.ChakraCoreCastXml.Extensions
{
    using System.Xml.Linq;

    public static class XElementExtensions
    {
        /// <summary>
        /// Get the value from an attribute.
        /// </summary>
        /// <param name="xElement">The <see cref="XElement"/> object to get the attribute from.</param>
        /// <param name="name">The name of the attribute.</param>
        /// <returns></returns>
        public static string AttributeValue(this XElement xElement, string name) => xElement.Attribute(name)?.Value;
    }
}
