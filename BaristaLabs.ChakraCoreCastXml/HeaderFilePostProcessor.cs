namespace BaristaLabs.ChakraCoreCastXml
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    public class HeaderFilePostProcessor
    {
        private static IDictionary<string, HeaderFilePostProcessor> s_processors = new Dictionary<string, HeaderFilePostProcessor>();
        private static Regex s_directionRegex = new Regex(@"\s*(?<Direction>_\S*_).*", RegexOptions.Compiled);
        private static Regex s_directionRegex2 = new Regex(@"\s*(?<FunctionName>\S*)\((?<Direction>_\S*_).*", RegexOptions.Compiled);
        private readonly string[] m_code;

        private HeaderFilePostProcessor(string headerFilePath)
        {
            HeaderFilePath = headerFilePath;
            m_code = File.ReadAllLines(headerFilePath);
        }

        public string HeaderFilePath { get; }

        public ParameterDirection GetArgumentDirection(int lineNumber)
        {
            var argumentLine = m_code.ElementAt(lineNumber-1);
            var match = s_directionRegex.Match(argumentLine);
            if (!match.Success)
            {
                match = s_directionRegex2.Match(argumentLine);
            }

            var argumentDirection = match.Groups["Direction"].Value;
            switch(argumentDirection)
            {
                case "_In_":
                case "_In_opt_":
                case "_In_z_":
                case "_In_reads_":
                case "_Pre_maybenull_":
                    return ParameterDirection.In;
                case "_Out_":
                case "_Out_opt_":
                case "_Out_writes_to_opt_":
                case "_Outptr_result_maybenull_":
                case "_Outptr_result_bytebuffer_":
                case "_Outptr_result_buffer_":
                case "_Outptr_result_z_":
                    return ParameterDirection.Out;
                case "_Inout_":
                    return ParameterDirection.Ref;
                default:
                    return ParameterDirection.Out;
            }
        }

        public string[] GetCodeCommentPreviousToLine(int lineNumber)
        {
            //rudimentary, but it works.

            var codeCommentLines = new List<string>();

            var startIndex = lineNumber - 2;
            while (m_code[startIndex - 1].TrimStart().StartsWith("///"))
            {
                startIndex--;
            }

            return m_code.Skip(startIndex).Take((lineNumber - 2) - startIndex).ToArray();
        }

        public static HeaderFilePostProcessor GetPostProcessor(string codeFilePath)
        {
            var normalizedFilePath = Path.GetFullPath(codeFilePath);
            if (s_processors.ContainsKey(normalizedFilePath))
            {
                return s_processors[normalizedFilePath];
            }

            var processor = new HeaderFilePostProcessor(normalizedFilePath);
            s_processors.Add(normalizedFilePath, processor);
            return processor;
        }
    }
}
