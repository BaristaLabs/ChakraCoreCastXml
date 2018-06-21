namespace BaristaLabs.ChakraCoreCastXml.GccXml
{
    using Generator;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;

    [XmlRoot(ElementName = "GCC_XML")]
    public class GccXmlDoc
    {
        [XmlElement(ElementName = CastXml.TagNamespace, Type = typeof(GccXmlNamespace))]
        [XmlElement(ElementName = CastXml.TagTypedef, Type = typeof(GccXmlTypeDef))]
        [XmlElement(ElementName = CastXml.TagPointerType, Type = typeof(GccXmlPointerType))]
        [XmlElement(ElementName = CastXml.TagFundamentalType, Type = typeof(GccXmlFundamentalType))]
        [XmlElement(ElementName = CastXml.TagCvQualifiedType, Type = typeof(GccXmlCvQualifiedType))]
        [XmlElement(ElementName = CastXml.TagEnumeration, Type = typeof(GccXmlEnumeration))]
        [XmlElement(ElementName = CastXml.TagFunctionType, Type = typeof(GccXmlFunctionType))]
        [XmlElement(ElementName = CastXml.TagFile, Type = typeof(GccXmlFile))]
        //[XmlElement(ElementName = CastXml.TagStruct, Type = typeof(GccXmlStruct))]
        [XmlElement(ElementName = CastXml.TagFunction, Type = typeof(GccXmlFunction))]
        public List<GccXmlElement> Decls
        {
            get;
            set;
        }

        public GccXmlElement GetDeclById(string id)
        {
            return Decls.FirstOrDefault(decl => decl.Id == id);
        }

        public IEnumerable<(GccXmlFile, GccXmlFunction)> GetFunctionsInHeadersContainedInPath(string filePath)
        {
            var headerFiles = Decls.OfType<GccXmlFile>().Where(f => Path.GetDirectoryName(f.Name) == Path.GetDirectoryName(filePath));

            foreach(var headerFile in headerFiles)
            {
                foreach(var fn in Decls.OfType<GccXmlFunction>().Where(f => f.File == headerFile.Id))
                {
                    yield return (headerFile, fn);
                }
            }
        }

        public string GetTypeNameById(string typeId, string baseName = "")
        {
            var decl = GetDeclById(typeId);

            if (decl == null)
            {
                throw new InvalidOperationException($"Unable to locate parent type with the id of {typeId} (Base: {baseName})");
            }

            switch(decl)
            {
                case INamed named:
                    return named.Name + baseName;
                case ITyped typed:
                    return GetTypeNameById(typed.Type, baseName + "*");
                case GccXmlFunctionType fnType:
                    return baseName + "fn";
                default:
                    throw new InvalidOperationException($"Unsupported value for Type: {decl.GetType()}");
            }
        }

        public void Save(FileStream stream)
        {
            var serializer = new XmlSerializer(typeof(GccXmlDoc));
            serializer.Serialize(stream, this);
        }

        public static GccXmlDoc Load(TextReader textReader)
        {
            var serializer = new XmlSerializer(typeof(GccXmlDoc));
            var doc = serializer.Deserialize(textReader) as GccXmlDoc;

            // Perform some post-processing.
            //AdjustTypeNamesFromTypedefs(doc);

            return doc;
        }

        private static void AdjustTypeNamesFromTypedefs(GccXmlDoc doc)
        {
            foreach (var xTypedef in doc.Decls.OfType<GccXmlTypeDef>())
            {
                if (!(doc.GetDeclById(xTypedef.Type) is INamed xStruct))
                {
                    return;
                }

                var structName = xStruct.Name;
                // Rename all structure starting with tagXXXX to XXXX
                if (structName.StartsWith("tag") || structName.StartsWith("_") || string.IsNullOrEmpty(structName))
                {
                    xStruct.Name = xTypedef.Name;
                }
            }
        }
    }
}
