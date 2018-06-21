namespace BaristaLabs.ChakraCoreCastXml
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class CodeCommentGatherer
    {
        private static IDictionary<string, CodeCommentGatherer> s_gatherers = new Dictionary<string, CodeCommentGatherer>();

        private readonly string[] m_code;

        private CodeCommentGatherer(string codeFilePath)
        {
            m_code = File.ReadAllLines(codeFilePath);
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

        public static CodeCommentGatherer GetCodeCommentGatherer(string codeFilePath)
        {
            var normalizedFilePath = Path.GetFullPath(codeFilePath);
            if (s_gatherers.ContainsKey(normalizedFilePath))
            {
                return s_gatherers[normalizedFilePath];
            }

            var gatherer = new CodeCommentGatherer(normalizedFilePath);
            s_gatherers.Add(normalizedFilePath, gatherer);
            return gatherer;
        }
    }
}
