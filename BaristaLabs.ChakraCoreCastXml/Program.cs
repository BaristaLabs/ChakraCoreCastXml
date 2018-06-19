﻿namespace BaristaLabs.ChakraCoreCastXml
{
    using Logging;
    using Config;
    using Generator;
    using System.IO;
    using System;

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

            var codeGenApp = new CodeGenApp(logger)
            {
                CastXmlExecutablePath = castXmlPath,
                Config = config,
                OutputDirectory = chakraSharpDir.FullName,
                IntermediateOutputPath = intermediateOutputDir.FullName,
            };

            codeGenApp.Init();
            codeGenApp.Run();
            Console.ReadLine();
        }
    }
}
