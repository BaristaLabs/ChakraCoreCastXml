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

    /// <summary>
    /// Config File.
    /// </summary>
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
            IgnoreFunctions = new List<string>();
            IncludeDirs = new List<IncludeDirRule>();
            IncludeProlog = new List<string>();
            Includes = new List<IncludeRule>();
            References = new List<ConfigFile>();
            TypeMap = new List<TypeMapRule>();
            Variables = new List<KeyValue>();
        }

        #region Properties

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

        public List<string> Depends { get; set; }

        /// <summary>
        /// Gets dynamic variables used by dynamic variable substitution #(MyVariable)
        /// </summary>
        /// <value>The dynamic variables.</value>
        public Dictionary<string, string> DynamicVariables { get; private set; }

        public IList<ExportExtensionRule> ExportExtensions { get; set; }

        public string ExtensionId { get { return Id + "-ext"; } }

        /// <summary>
        /// Gets the name of the extension header file.
        /// </summary>
        /// <value>The name of the extension header file.</value>
        public string ExtensionFileName { get { return ExtensionId + ".h"; } }

        /// <summary>
        /// Gets or sets the path of this MappingFile. If not null, used when saving this file.
        /// </summary>
        /// <value>The path.</value>
        public string FilePath { get; set; }

        public List<string> Files { get; set; }

        public string Id { get; set; }

        public IList<string> IgnoreFunctions { get; set; }

        public List<IncludeDirRule> IncludeDirs { get; set; }

        public List<string> IncludeProlog { get; set; }

        public List<IncludeRule> Includes { get; set; }

        public bool IncludeLineNumbers { get; set; }

        /// <summary>
        /// Gets or sets the parent of this mapping file.
        /// </summary>
        /// <value>The parent.</value>
        public ConfigFile Parent { get; set; }

        public List<ConfigFile> References { get; set; }

        public IList<TypeMapRule> TypeMap { get; set; }

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

        public static ConfigFile Load(ConfigFile root, Logger logger)
        {     
            root.Verify(logger);
            root.ExpandVariables(false, logger);
            return root;
        }
    }
}