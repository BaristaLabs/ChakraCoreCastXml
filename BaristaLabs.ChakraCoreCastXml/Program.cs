namespace BaristaLabs.ChakraCoreCastXml
{
    using Logging;
    using Config;
    using Generator;
    using System.IO;
    using System;
    using System.Xml.Linq;

    class Program
    {
        static void Main(string[] args)
        {
            var consoleLogger = new ConsoleLogger();
            var logger = new Logger(consoleLogger, null);

            ConfigFile config = new ConfigFile()
            {
                Id = "ChakraCore",
                IncludeDirs =
                {
                    new IncludeDirRule
                    {
                        Path = @"C:\Projects\chakracore\lib\Jsrt\"
                    }
                },
                Includes =
                {
                    new IncludeRule
                    {
                        Attach = true,
                        File = @"C:\Projects\chakracore\lib\Jsrt\ChakraCore.h",
                        Namespace = "ChakraCore",
                    },
                    new IncludeRule
                    {
                        Attach = true,
                        File = @"C:\Projects\chakracore\lib\Jsrt\ChakraCommon.h",
                        Namespace = "ChakraCommon",
                    },
                    new IncludeRule
                    {
                        Attach = true,
                        File = @"C:\Projects\chakracore\lib\Jsrt\ChakraDebug.h",
                        Namespace = "ChakraCommon",
                    },
                },
                IgnoreFunctions =
                {
                    "^JsTTD.*"
                },
                TypeMap = {
                    new TypeMapRule { From = "void*", To = "IntPtr" },
                    new TypeMapRule { From = "void**", To = "IntPtr*" },
                    new TypeMapRule { From = "BYTE*", To = "byte[]" },
                    new TypeMapRule { From = "char", FromDirection = ParameterDirection.Out, To = "char*", ToDirection = ParameterDirection.In },
                    new TypeMapRule { From = "char**", To = "string" },
                    new TypeMapRule { From = "uint16_t", FromDirection = ParameterDirection.Out, To = "uint16_t*", ToDirection = ParameterDirection.In },
                    new TypeMapRule { From = "uint16_t**", To = "string" },
                    new TypeMapRule { From = "unsigned int", To = "uint" },
                    new TypeMapRule { From = "unsigned int*", To = "uint*" },
                    new TypeMapRule { From = "short unsigned int", To = "ushort" },
                    new TypeMapRule { From = "JsValueRef*", To = "JsValueRef[]" },
                    new TypeMapRule { From = "JsWeakRef", FromDirection = ParameterDirection.Out, To = "IntPtr" },
                    new TypeMapRule { From = "BYTE", FromDirection = ParameterDirection.Out, To = "byte[]", ToDirection = ParameterDirection.In },
                    new TypeMapRule { From = "ChakraBytePtr", To = "IntPtr" },
                    new TypeMapRule { From = "wchar_t**", FromDirection = ParameterDirection.In, To = "string" },
                    new TypeMapRule { From = "wchar_t**", FromDirection = ParameterDirection.Out, To = "IntPtr" },
                    new TypeMapRule { From = "JsSharedArrayBufferContentHandle", FromDirection = ParameterDirection.Out, To = "IntPtr" },
                },
                ExportExtensions =
                {
                    new ExportExtensionRule
                    {
                        FunctionName = "JsCreateStringUtf16",
                        Extensions =
                        {
                            new XAttribute("dllImportEx", ", CharSet = CharSet.Unicode")
                        }
                    },
                    new ExportExtensionRule
                    {
                        FunctionName = "JsCopyString",
                        Extensions =
                        {
                            new XAttribute("dllImportEx", ", CharSet = CharSet.Ansi")
                        }
                    }
                }
            };

            var outputDi = Directory.CreateDirectory("../output");
            var chakraSharpDir = Directory.CreateDirectory("../output/ChakraSharp");
            var intermediateOutputDir = Directory.CreateDirectory("../output/temp");

            var castXmlPath = @"C:\Projects\ChakraCoreCastXml\lib\castxml\bin\castxml.exe";
            if (!File.Exists(castXmlPath))
            {
                throw new InvalidOperationException("Unable to locate CastXml at " + castXmlPath);
            }

            var resolver = new IncludeDirectoryResolver(logger);
            resolver.Configure(config);

            var castXml = new CastXml(logger, resolver, castXmlPath)
            {
                OutputPath = "",
            };

            var codeGenApp = new ChakraExternGenerator(logger)
            {
                CastXmlExecutablePath = castXmlPath,
                Config = config,
                OutputDirectory = @"C:\Projects\BaristaCore",
                IntermediateOutputPath = intermediateOutputDir.FullName,
            };

            codeGenApp.Init();
            codeGenApp.Run();
        }
    }
}
