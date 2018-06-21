namespace BaristaLabs.ChakraCoreCastXml.Config
{
    using Logging;

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;

    /// <summary>
    /// Config File.
    /// </summary>
    [XmlRoot("config", Namespace = NS)]
    public class ConfigFile
    {
        internal const string NS = "urn:ChakraCore.Config";

        private static readonly Regex ReplaceVariableRegex = new Regex(@"\$\(([a-zA-Z_][\w_]*)\)", RegexOptions.Compiled);
        private static readonly Regex ReplaceDynamicVariableRegex = new Regex(@"#\(([a-zA-Z_][\w_]*)\)", RegexOptions.Compiled);
        private Dictionary<string, ConfigFile> MapIdToFile = new Dictionary<string, ConfigFile>();

        public ConfigFile()
        {
            Depends = new List<string>();
            DynamicVariables = new Dictionary<string, string>();
            Files = new List<string>();
            ExportExtensions = new List<ExportExtensionRule>();
            IncludeDirs = new List<IncludeDirRule>();
            IncludeProlog = new List<string>();
            Includes = new List<IncludeRule>();
            References = new List<ConfigFile>();
            TypeMap = new Dictionary<string, string>();
            Variables = new List<KeyValue>();
        }

        #region Properties

        [XmlIgnore]
        public string AbsoluteFilePath
        {
            get
            {
                if (FilePath == null)
                    return null;
                if (Path.IsPathRooted(FilePath))
                    return FilePath;
                if (Parent?.AbsoluteFilePath != null)
                    return Path.Combine(Path.GetDirectoryName(Parent.AbsoluteFilePath), FilePath);
                return Path.GetFullPath(Path.Combine(".", FilePath));
            }
        }

        public IEnumerable<ConfigFile> ConfigFilesLoaded
        {
            get { return GetRoot().MapIdToFile.Values; }
        }

        [XmlElement("depends")]
        public List<string> Depends { get; set; }

        /// <summary>
        /// Gets dynamic variables used by dynamic variable substitution #(MyVariable)
        /// </summary>
        /// <value>The dynamic variables.</value>
        [XmlIgnore]
        public Dictionary<string, string> DynamicVariables { get; private set; }

        [XmlIgnore]
        public IList<ExportExtensionRule> ExportExtensions { get; set; }

        [XmlIgnore]
        public string ExtensionId { get { return Id + "-ext"; } }

        /// <summary>
        /// Gets the name of the extension header file.
        /// </summary>
        /// <value>The name of the extension header file.</value>
        [XmlIgnore]
        public string ExtensionFileName { get { return ExtensionId + ".h"; } }

        /// <summary>
        /// Gets or sets the path of this MappingFile. If not null, used when saving this file.
        /// </summary>
        /// <value>The path.</value>
        [XmlIgnore]
        public string FilePath { get; set; }

        [XmlElement("file")]
        public List<string> Files { get; set; }

        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlElement("include-dir")]
        public List<IncludeDirRule> IncludeDirs { get; set; }

        [XmlElement("include-prolog")]
        public List<string> IncludeProlog { get; set; }

        [XmlElement("include")]
        public List<IncludeRule> Includes { get; set; }

        [XmlElement("IncludeLineNumbers")]
        public bool IncludeLineNumbers { get; set; }

        /// <summary>
        /// Gets or sets the parent of this mapping file.
        /// </summary>
        /// <value>The parent.</value>
        [XmlIgnore]
        public ConfigFile Parent { get; set; }

        [XmlIgnore]
        public List<ConfigFile> References { get; set; }

        [XmlArray("TypeMap")]
        public IDictionary<string, string> TypeMap { get; set; }

        [XmlElement("var")]
        public List<KeyValue> Variables { get; set; }
        #endregion

        /// <summary>
        /// Expands a string using environment variable and variables defined in mapping files.
        /// </summary>
        /// <param name="str">The string to expand.</param>
        /// <returns>the expanded string</returns>
        public string ExpandString(string str, bool expandDynamicVariable, Logger logger)
        {
            var result = str;

            // Perform Config Variable substitution
            if (ReplaceVariableRegex.Match(result).Success)
            {
                result = ReplaceVariableRegex.Replace(
                    result,
                    match =>
                    {
                        string name = match.Groups[1].Value;
                        string localResult = GetVariable(name, logger);
                        if (localResult == null)
                            localResult = Environment.GetEnvironmentVariable(name);
                        if (localResult == null)
                        {
                            logger.Error(LoggingCodes.UnkownVariable, "Unable to substitute config/environment variable $({0}). Variable is not defined", name);
                            return "";
                        }
                        return localResult;
                    });
            }

            // Perform Dynamic Variable substitution
            if (expandDynamicVariable && ReplaceDynamicVariableRegex.Match(result).Success)
            {
                result = ReplaceDynamicVariableRegex.Replace(
                    result,
                    match =>
                    {
                        string name = match.Groups[1].Value;
                        string localResult;
                        if (!GetRoot().DynamicVariables.TryGetValue(name, out localResult))
                        {
                            logger.Error(LoggingCodes.UnkownDynamicVariable, "Unable to substitute dynamic variable #({0}). Variable is not defined", name);
                            return "";
                        }
                        localResult = localResult.Trim('"');
                        return localResult;
                    });
            }
            return result;
        }

        /// <summary>
        /// Expands all dynamic variables used inside Bindings and Mappings tags.
        /// </summary>
        /// <param name="expandDynamicVariable">if set to <c>true</c> expand dynamic variables.</param>
        public void ExpandVariables(bool expandDynamicVariable, Logger logger)
        {
            ExpandVariables(Variables, expandDynamicVariable, logger);
            ExpandVariables(Includes, expandDynamicVariable, logger);
            ExpandVariables(IncludeDirs, expandDynamicVariable, logger);
            // Do it recursively
            foreach (var configFile in References)
                configFile.ExpandVariables(expandDynamicVariable, logger);
        }

        /// <summary>
        /// Iterate on all objects and sub-objects to expand dynamic variable
        /// </summary>
        /// <param name="objectToExpand">The object to expand.</param>
        /// <returns>the expanded object</returns>
        private object ExpandVariables(object objectToExpand, bool expandDynamicVariable, Logger logger)
        {
            if (objectToExpand == null)
                return null;
            if (objectToExpand is string str)
                return ExpandString(str, expandDynamicVariable, logger);
            if (objectToExpand.GetType().IsPrimitive)
                return objectToExpand;
            if (objectToExpand is IList list)
            {
                for (int i = 0; i < list.Count; i++)
                    list[i] = ExpandVariables(list[i], expandDynamicVariable, logger);
                return list;
            }
            foreach (var propertyInfo in objectToExpand.GetType().GetRuntimeProperties())
            {
                if (!propertyInfo.GetCustomAttributes<XmlIgnoreAttribute>(false).Any())
                {
                    // Check that this field is "ShouldSerializable"
                    var method = objectToExpand.GetType().GetRuntimeMethod("ShouldSerialize" + propertyInfo.Name, Type.EmptyTypes);
                    if (method != null && !((bool)method.Invoke(objectToExpand, null)))
                        continue;

                    propertyInfo.SetValue(objectToExpand, ExpandVariables(propertyInfo.GetValue(objectToExpand, null), expandDynamicVariable, logger), null);
                }
            }
            return objectToExpand;
        }

        public ConfigFile GetRoot()
        {
            var root = this;
            while (root.Parent != null)
                root = root.Parent;
            return root;
        }

        /// <summary>
        /// Gets a variable value. Value is expanded if it contains any reference to other variables.
        /// </summary>
        /// <param name="variableName">Name of the variable.</param>
        /// <returns>the value of this variable</returns>
        private string GetVariable(string variableName, Logger logger)
        {
            foreach (var keyValue in Variables)
            {
                if (keyValue.Name == variableName)
                    return ExpandString(keyValue.Value, false, logger);
            }
            if (Parent != null)
                return Parent.GetVariable(variableName, logger);
            return null;
        }

        private void PostLoad(ConfigFile parent, string file, string[] macros, IEnumerable<KeyValue> variables, Logger logger)
        {
            FilePath = file;
            Parent = parent;

            if (AbsoluteFilePath != null)
            {
                Variables.Add(new KeyValue("THIS_CONFIG_PATH", Path.GetDirectoryName(AbsoluteFilePath)));
            }

            Variables.AddRange(variables);

            // Load all dependencies
            foreach (var dependFile in Files)
            {
                var dependFilePath = ExpandString(dependFile, false, logger);
                if (!Path.IsPathRooted(dependFilePath) && AbsoluteFilePath != null)
                    dependFilePath = Path.Combine(Path.GetDirectoryName(AbsoluteFilePath), dependFilePath);

                var subMapping = Load(this, dependFilePath, macros, variables, logger);
                if (subMapping != null)
                {
                    subMapping.FilePath = dependFile;
                    References.Add(subMapping);
                }
            }

            // Clear all depends file
            Files.Clear();

            // Add this mapping file
            GetRoot().MapIdToFile.Add(Id, this);
        }

        private void Verify(Logger logger)
        {
            Depends.Remove("");

            // TODO: verify Depends
            foreach (var depend in Depends)
            {
                if (!GetRoot().MapIdToFile.ContainsKey(depend))
                    logger.Error(LoggingCodes.MissingConfigDependency, $"Unable to resolve dependency [{depend}] for config file [{Id}]");
            }

            foreach (var includeDir in IncludeDirs)
            {
                includeDir.Path = ExpandString(includeDir.Path, false, logger);

                if (!includeDir.Path.StartsWith("=") && !Directory.Exists(includeDir.Path))
                    logger.Error(LoggingCodes.IncludeDirectoryNotFound, $"Include directory {includeDir.Path} from config file [{Id}] not found");
            }

            // Verify all dependencies
            foreach (var mappingFile in References)
                mappingFile.Verify(logger);
        }

        /// <summary>
        /// Loads the specified config file attached to a parent config file.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <param name="file">The file.</param>
        /// <returns>The loaded config</returns>
        private static ConfigFile Load(ConfigFile parent, string file, string[] macros, IEnumerable<KeyValue> variables, Logger logger)
        {
            if (!File.Exists(file))
            {
                logger.Error(LoggingCodes.ConfigNotFound, "Configuration file {0} not found.", file);
                return null;
            }

            var deserializer = new XmlSerializer(typeof(ConfigFile));
            ConfigFile config = null;
            try
            {
                logger.PushLocation(file);

                config = (ConfigFile)deserializer.Deserialize(new StringReader(Preprocessor.Preprocess(File.ReadAllText(file), macros)));

                if (config != null)
                    config.PostLoad(parent, file, macros, variables, logger);
            }
            catch (Exception ex)
            {
                logger.Error(LoggingCodes.UnableToParseConfig, "Unable to parse file [{0}]", ex, file);
            }
            finally
            {
                logger.PopLocation();
            }
            return config;
        }

        public static ConfigFile Load(ConfigFile root, string[] macros, Logger logger, params KeyValue[] variables)
        {
            root.PostLoad(null, null, macros, variables, logger);
            root.Verify(logger);
            root.ExpandVariables(false, logger);
            return root;
        }
    }
}