namespace BaristaLabs.ChakraCoreCastXml
{
    using Logging;
    using Config;
    using Generator;

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Serialization;
    using System.Xml.Linq;
    using BaristaLabs.ChakraCoreCastXml.Extensions;

    /// <summary>
    /// CodeGen Application.
    /// </summary>
    public class CodeGenApp
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenApp"/> class.
        /// </summary>
        public CodeGenApp(Logger logger)
        {
            Macros = new HashSet<string>();
            Logger = logger;
        }

        /// <summary>
        /// Gets or sets the CastXML executable path.
        /// </summary>
        /// <value>The CastXML executable path.</value>
        public string CastXmlExecutablePath { get; set; }

        /// <summary>
        /// Gets or sets output directory.
        /// </summary>
        /// <remarks>Null is allowed, in which case sharpgen will use default</remarks>
        public string OutputDirectory { get; set; }
       
        public Logger Logger { get; }

        /// <summary>
        /// Gets or sets the macros.
        /// </summary>
        /// <value>
        /// The macros.
        /// </value>
        public HashSet<string> Macros { get; set; }

        public string ConfigRootPath { get; set; }

        public string IntermediateOutputPath { get; set; } = "";

        public ConfigFile Config { get; set; }

        public string ConsumerBindMappingConfigId { get; set; }

        /// <summary>
        /// Initializes the specified instance with a config root file.
        /// </summary>
        /// <returns>true if the config or assembly changed from the last run; otherwise returns false</returns>
        public bool Init()
        {
            Logger.Message("Loading config files...");

            if (Config == null)
            {
                throw new ArgumentNullException(nameof(Config));
            }

            Config = ConfigFile.Load(Config, Macros.ToArray(), Logger);

            if (Logger.HasErrors)
            {
                Logger.Fatal("Errors loading the config file");
            }
            return true;
        }

        /// <summary>
        /// Run CodeGenerator
        /// </summary>
        public void Run()
        {
            Logger.Progress(0, "Starting code generation...");

            try
            {
                var consumerConfig = new ConfigFile
                {
                    Id = ConsumerBindMappingConfigId
                };

                var filesWithIncludes = Config.GetFilesWithIncludes();

                var configsWithIncludes = new HashSet<ConfigFile>();

                foreach (var config in Config.ConfigFilesLoaded)
                {
                    if (filesWithIncludes.Contains(config.Id))
                    {
                        configsWithIncludes.Add(config);
                    }
                }

                var sdkResolver = new SdkResolver(Logger);

                foreach (var config in Config.ConfigFilesLoaded)
                {
                    foreach (var sdk in config.Sdks)
                    {
                        config.IncludeDirs.AddRange(sdkResolver.ResolveIncludeDirsForSdk(sdk));
                    }
                }

                var cppHeadersUpdated = GenerateHeaders(configsWithIncludes, consumerConfig);

                if (Logger.HasErrors)
                {
                    Logger.Fatal("Failed to generate C++ headers.");
                }

                var resolver = new IncludeDirectoryResolver(Logger);
                resolver.Configure(Config);

                var castXml = new CastXml(Logger, resolver, CastXmlExecutablePath)
                {
                    OutputPath = IntermediateOutputPath,
                };

                ParseCpp(castXml);

                if (Logger.HasErrors)
                    Logger.Fatal("Code generation failed");
            }
            finally
            {
                Logger.Progress(100, "Finished");
            }
        }

        private void ParseCpp(CastXml castXml)
        {
            StreamReader xmlReader = null;
            const string progressMessage = "Parsing C++ headers starts, please wait...";
            XDocument gccXmlDoc = null;

            try
            {
                Logger.Progress(15, progressMessage);

                var configRootHeader = Path.Combine(IntermediateOutputPath, Config.Id + ".h");

                xmlReader = castXml.Process(configRootHeader);
                if (xmlReader != null)
                {
                    gccXmlDoc = XDocument.Load(xmlReader);
                }

                var idElementMap = new Dictionary<string, XElement>();
                var fileElementMap = new Dictionary<string, List<XElement>>();

                // Collects all GccXml elements and build map from their id
                foreach (var xElement in gccXmlDoc.Elements("GCC_XML").Elements())
                {
                    var id = xElement.Attribute("id").Value;
                    idElementMap.Add(id, xElement);

                    var file = xElement.AttributeValue("file");
                    if (file != null)
                    {
                        if (!fileElementMap.TryGetValue(file, out List<XElement> elementsInFile))
                        {
                            elementsInFile = new List<XElement>();
                            fileElementMap.Add(file, elementsInFile);
                        }
                        elementsInFile.Add(xElement);
                    }
                }

                AdjustTypeNamesFromTypedefs(idElementMap, gccXmlDoc);

                // Find all elements that are referring to a context and attach them to
                // the context as child elements
                foreach (var xElement in idElementMap.Values)
                {
                    var id = xElement.AttributeValue("context");
                    if (id != null)
                    {
                        xElement.Remove();
                        idElementMap[id].Add(xElement);
                    }
                }

                Logger.Progress(30, progressMessage);
            }
            catch (Exception ex)
            {
                Logger.Error(null, "Unexpected error", ex);
            }
            finally
            {
                xmlReader?.Dispose();

                // Write back GCCXML document on the disk
                using (var stream = File.OpenWrite(Path.Combine(IntermediateOutputPath, Config.Id + "-gcc.xml")))
                {
                    gccXmlDoc?.Save(stream);
                }
                Logger.Message("Parsing headers is finished.");
            }
        }

        private HashSet<ConfigFile> GenerateHeaders(IReadOnlyCollection<ConfigFile> configsWithIncludes, ConfigFile consumerConfig)
        {
            var headerGenerator = new CppHeaderGenerator(Logger, true, IntermediateOutputPath);

            var (cppHeadersUpdated, prolog) = headerGenerator.GenerateCppHeaders(Config, configsWithIncludes);

            consumerConfig.IncludeProlog.Add(prolog);
            return cppHeadersUpdated;
        }

        private void GenerateConfigForConsumers(ConfigFile consumerConfig)
        {
            var consumerBindMappingFileName = Path.Combine(IntermediateOutputPath, $"{ConsumerBindMappingConfigId ?? Config.Id}.BindMapping.xml");

            if (File.Exists(consumerBindMappingFileName))
            {
                File.Delete(consumerBindMappingFileName);
            }

            using (var consumerBindMapping = File.Open(consumerBindMappingFileName, FileMode.OpenOrCreate, FileAccess.Write))
            using (var fileWriter = new StreamWriter(consumerBindMapping))
            {
                var serializer = new XmlSerializer(typeof(ConfigFile));
                serializer.Serialize(fileWriter, consumerConfig);
            }
        }

        private void AdjustTypeNamesFromTypedefs(Dictionary<string, XElement> idElementMap, XDocument doc)
        {
            foreach (var xTypedef in doc.Elements("GCC_XML").Elements(CastXml.TagTypedef))
            {
                var xStruct = idElementMap[xTypedef.AttributeValue("type")];
                switch (xStruct.Name.LocalName)
                {
                    case CastXml.TagStruct:
                    case CastXml.TagUnion:
                    case CastXml.TagEnumeration:
                        var structName = xStruct.AttributeValue("name");
                        // Rename all structure starting with tagXXXX to XXXX
                        if (structName.StartsWith("tag") || structName.StartsWith("_") || string.IsNullOrEmpty(structName))
                        {
                            var typeName = xTypedef.AttributeValue("name");
                            xStruct.SetAttributeValue("name", typeName);
                        }
                        break;
                }
            }
        }
    }
}