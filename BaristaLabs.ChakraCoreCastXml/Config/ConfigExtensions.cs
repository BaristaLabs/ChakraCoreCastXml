namespace BaristaLabs.ChakraCoreCastXml.Config
{
    using System.Linq;
    using System.Collections.Generic;

    public static class ConfigExtensions
    {
        public static HashSet<string> GetFilesWithIncludes(this ConfigFile configRoot)
        {
            var filesWithIncludes = new HashSet<string>();

            // Check if the file has any includes related config
            foreach (var configFile in configRoot.ConfigFilesLoaded)
            {
                var includesAnyFiles = false;

                // Build prolog
                if (configFile.IncludeProlog.Count > 0)
                    includesAnyFiles = true;

                if (configFile.Includes.Count > 0)
                    includesAnyFiles = true;

                if (configFile.References.Count > 0)
                    includesAnyFiles = true;

                // If this config file has any include rules
                if (includesAnyFiles)
                    filesWithIncludes.Add(configFile.Id);
            }

            return filesWithIncludes;
        }
    }
}
