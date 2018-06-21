namespace BaristaLabs.ChakraCoreCastXml
{
    using Config;
    using GccXml;
    using Generator;
    using Logging;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// CodeGen Application.
    /// </summary>
    public class ChakraExternGenerator
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChakraExternGenerator"/> class.
        /// </summary>
        public ChakraExternGenerator(Logger logger)
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
        public GccXmlDoc Run()
        {
            GccXmlDoc doc = null;
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

                GenerateHeaders(configsWithIncludes, consumerConfig);

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

                doc = ParseHeaders(castXml);

                if (Logger.HasErrors)
                    Logger.Fatal("Header parsing failed");

                OutputExternDefinition(doc);
            }
            finally
            {
                Logger.Progress(100, "Finished");
            }

            return doc;
        }

        private HashSet<ConfigFile> GenerateHeaders(IReadOnlyCollection<ConfigFile> configsWithIncludes, ConfigFile consumerConfig)
        {
            var headerGenerator = new CppHeaderGenerator(Logger, true, IntermediateOutputPath);

            var (cppHeadersUpdated, prolog) = headerGenerator.GenerateCppHeaders(Config, configsWithIncludes);

            consumerConfig.IncludeProlog.Add(prolog);
            return cppHeadersUpdated;
        }

        private GccXmlDoc ParseHeaders(CastXml castXml)
        {
            StreamReader xmlReader = null;
            const string progressMessage = "Parsing C++ headers starts, please wait...";

            try
            {
                Logger.Progress(15, progressMessage);

                var configRootHeader = Path.Combine(IntermediateOutputPath, Config.Id + ".h");

                xmlReader = castXml.Process(configRootHeader);

                if (xmlReader != null)
                {
                    return GccXmlDoc.Load(xmlReader);
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

                Logger.Message("Parsing headers is finished.");
            }

            return null;
        }

        private void OutputExternDefinition(GccXmlDoc doc)
        {
            Logger.Message("Generating Extern Definition");

            XElement root = new XElement("ChakraDefinitions");
            XDocument externs = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root);

            var groupedFunctions = doc.GetFunctionsInHeadersContainedInPath(Config.IncludeDirs.First().Path)
                .GroupBy(f => Path.GetFullPath(f.Item1.Name));

            foreach (var g in groupedFunctions.OrderBy(g =>
            {
                var include = Config.Includes.FirstOrDefault(i => Path.GetFullPath(i.File) == Path.GetFullPath(g.Key));
                if (include == null)
                {
                    return Config.Includes.Count + 1;
                }
                return Config.Includes.IndexOf(include);
            }))
            {
                root.Add(new XComment($@"
  ***************************************
  **
  ** {Path.GetFileName(g.First().Item1.Name)}
  **
  ***************************************
  "));
                foreach (var fn in g.OrderBy(f => int.Parse(f.Item2.Line)))
                {
                    var filePath = fn.Item1.Name;
                    var function = fn.Item2;

                    var fileName = Path.GetFileName(filePath);
                    string target = "Common";
                    if (fileName.Contains("Windows"))
                    {
                        target = "WindowsOnly";
                    }

                    var export = new XElement("Export", new XAttribute("name", fn.Item2.Name), new XAttribute("target", target), new XAttribute("source", fileName));
                    if (target == "WindowsOnly")
                    {
                        export.Add(new XAttribute("dllImportEx", ", CharSet = CharSet.Unicode"));
                    }
                    if (Config.IncludeLineNumbers)
                    {
                        export.Add(new XAttribute("line", function.Line));
                    }
                    var fnExtension = Config.ExportExtensions.FirstOrDefault(ed => ed.FunctionName == fn.Item2.Name);
                    if (fnExtension != null)
                    {
                        export.Add(fnExtension.Extensions);
                    }

                    var ccg = HeaderFilePostProcessor.GetPostProcessor(fn.Item1.Name);
                    var codeComment = ccg.GetCodeCommentPreviousToLine(int.Parse(function.Line));

                    var descriptionElement = new XElement("Description", new XText(new XText(Environment.NewLine) + "      "), new XCData(Environment.NewLine + String.Join(Environment.NewLine, codeComment) + Environment.NewLine), new XText(Environment.NewLine + "     "));
                    export.Add(descriptionElement);

                    var parameters = new XElement("Parameters");
                    foreach (var arg in fn.Item2.Arguments.OrderBy(arg => int.Parse(arg.Line)))
                    {
                        var argTypeName = doc.GetTypeNameById(arg.Type);
                        var direction = ParameterDirection.In;
                        direction = ccg.GetArgumentDirection(int.Parse(arg.Line));
                        if (direction != ParameterDirection.In && argTypeName.EndsWith('*'))
                        {
                            argTypeName = argTypeName.Substring(0, argTypeName.Length - 1);
                        }

                        if (Config.TypeMap.ContainsKey(argTypeName))
                        {
                            argTypeName = Config.TypeMap[argTypeName];
                        }

                        var argName = arg.Name;
                        switch (argName)
                        {
                            case "object":
                            case "ref":
                                argName = "@" + argName;
                                break;
                        }

                        var parameter = new XElement("Parameter", new XAttribute("type", argTypeName), new XAttribute("name", argName));

                        if (Config.IncludeLineNumbers)
                        {
                            parameter.Add(new XAttribute("line", arg.Line));
                        }

                        if (direction != ParameterDirection.In)
                        {
                            parameter.Add(new XAttribute("direction", direction.ToString()));
                        }
                        
                        parameters.Add(parameter);
                    }
                    export.Add(parameters);

                    root.Add(export);
                }
            }


            externs.Save(Path.Combine(OutputDirectory, "ChakraExternDefinitions.xml"));

            Logger.Message("Generated Extern Definition");
        }
    }
}