using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using wcg.CodeGeneration;
using wcg.WebFiles;

namespace wcg
{
    internal static class Program
    {
        internal class ProgramArgs
        {
            [Arg("wsdl", Description = "The directory containing .wsdl files")]
            public string WsdlPath { get; set; }

            [Arg("xsd", Description = "The directory containing .xsd files")]
            public string XsdPath { get; set; }

            [Arg("out", ShortName = "o", Description = "The directory where generated code files will be placed")]
            public string OutPath { get; set; }

            [Arg("namespace", ShortName = "ns", Description = "The namespace to use for all generated code files")]
            public string Namespace { get; set; }

            [Arg("interactive", ShortName = "i", Description = "Interactive prompts regarding file overwriting and program dismissal")]
            public bool Interactive { get; set; }

            [Arg("recursive", ShortName = "r", Description = "Search subdirectories of the 'wsdl' and 'xsd' paths for files")]
            public bool Recursive { get; set; }

            [Arg("verbose", ShortName = "v", Description = "Increased verbosity of console output")]
            public bool Verbose { get; set; }

            [Arg("debug", ShortName = "dbg", Description = "Display program stack trace information for bug reporting")]
            public bool StackTraces { get; set; }

            [Arg("help", ShortName = "?", Description = "Display this help page")]
            public bool Help { get; set; }
        }

        private static ProgramArgs _args;

        private static void Main(string[] args)
        {
            try
            {
                Output.SetTitle(ApplicationUtilities.Description);

                var argParser = new MicroArgs<ProgramArgs>(args);

                _args = argParser.Parsed;

                if (_args == null || _args.Help || _args.Verbose)
                {
                    argParser.DisplayVersion();
                }

                if (_args == null || _args.Help)
                {
                    argParser.DisplayHelp();

                    if ((_args?.Interactive ?? false) || Debugger.IsAttached)
                    {
                        Output.AnyKey();
                    }

                    Environment.ExitCode = -1;
                    Console.ResetColor();
                    return;
                }

                if (Debugger.IsAttached)
                {
                    _args.Interactive = true;
                }

                if (string.IsNullOrEmpty(_args.WsdlPath))
                {
                    if (string.IsNullOrEmpty(_args.XsdPath))
                    {
                        _args.WsdlPath = Environment.CurrentDirectory;
                    }
                    else
                    {
                        _args.WsdlPath = _args.XsdPath;
                    }
                }

                if (string.IsNullOrEmpty(_args.XsdPath))
                {
                    _args.XsdPath = _args.WsdlPath;
                }

                if (string.IsNullOrEmpty(_args.OutPath))
                {
                    _args.OutPath = Path.Combine(Environment.CurrentDirectory, "out");
                }

                if (string.IsNullOrEmpty(_args.Namespace))
                {
                    _args.Namespace = "Wcg.Generated";
                }

                // TODO: if URLs provided, pull those files -- same with links within the files


                if (_args.Verbose)
                {
                    Output.WriteSection("Application Parameters");
                    Output.NameValue("WSDL", _args.WsdlPath);
                    Output.NameValue("XSD", _args.XsdPath);
                    Output.NameValue("OUT", _args.OutPath);
                    Output.NameValue("NS", _args.Namespace);
                    Output.NameValue("Interactive", _args.Interactive.ToString());
                    Output.NameValue("Recursive", _args.Recursive.ToString());
                    Output.NameValue("Verbose", _args.Verbose.ToString());
                    Output.NameValue("Stack Traces", _args.StackTraces.ToString());
                    Output.EndSection();
                }

                if (Directory.Exists(_args.OutPath) && Directory.GetFileSystemEntries(_args.OutPath).Any())
                {
                    if (_args.Interactive)
                    {
                        var answer = Output.YesNo("Output directory exists and contains files/subdirectories, proceed anyway?", "(output files may overwrite existing files; directory will not be emptied prior to generation)", "to continue", "to abort");
                        
                        if (answer)
                        {
                            Output.DisplayInfo("Continuing");
                        }
                        else
                        {
                            Output.DisplayInfo("Aborting");
                            throw new Exception("User aborted due to existing directory");
                        }
                    }
                    else
                    {
                        throw new Exception("Output directory already exists and is non-empty");
                    }
                }

                Output.Timed("Finding WSDL files");
                var wsdlFiles = File.Exists(_args.WsdlPath) ? new [] { _args.WsdlPath } : Directory.GetFiles(_args.WsdlPath, "*.wsdl", _args.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToArray();
                Output.EndTimed();

                Output.DisplayInfo($"Found {wsdlFiles.Length} WSDL files");
                if (_args.Verbose)
                {
                    Output.DisplaySubList(wsdlFiles);
                }

                Output.Timed("Finding XSD files");
                var xsdFiles = File.Exists(_args.XsdPath) ? new[] { _args.XsdPath } : Directory.GetFiles(_args.XsdPath, "*.xsd", _args.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToArray();
                Output.EndTimed();

                Output.DisplayInfo($"Found {xsdFiles.Length} XSD files");
                if (_args.Verbose)
                {
                    Output.DisplaySubList(xsdFiles);
                }

                if (wsdlFiles.Length == 0 && xsdFiles.Length == 0)
                {
                    throw new Exception("No files found!");
                }

                Output.DisplayInfo("Starting Compilation");

                var allFiles = wsdlFiles.Concat(xsdFiles).ToArray();
                var fileManager = new FileManager(_args.OutPath, allFiles);

                var compiler = new Compiler(_args.Namespace, fileManager, Path.GetDirectoryName(allFiles.First()), new CodePostProcessorFactory());
                var xsdcomps = compiler.GetXsdCompilands();
                foreach (var compiland in xsdcomps)
                {
                    using (compiland)
                    {
                        Output.Line();
                        Output.Action("Compiling", compiland.OutputPath);
                        Output.Line();

                        compiler.CompileXsd(compiland);
                    }
                }

                var wsdlcomps = compiler.GetWsdlCompilands();
                foreach (var compiland in wsdlcomps)
                {
                    using (compiland)
                    {
                        Output.Line();
                        Output.Action("Compiling", fileManager.GetFileName(compiland.OutputPath));
                        Output.Line();

                        compiler.CompileWsdl(compiland);
                    }
                }

                
                //using (new WsdlCompiler(new CodePostProcessorFactory(), _args.Namespace, new FileManager(_args.OutPath, wsdlFiles.Concat(xsdFiles))))
                //{
                //}

                Output.Line();
                Output.Success("Compilation completed successfully!");
                Console.WriteLine();
            }
            catch (Exception exception)
            {
                Output.Line();
                Output.DisplayError(exception.Message, (_args?.StackTraces ?? false) ? exception.StackTrace : string.Empty);
                
                Environment.ExitCode = -500;
            }

            Console.CursorVisible = true;
            if (_args?.Interactive ?? Debugger.IsAttached)
            {
                Output.AnyKey();
            }

            Output.Reset();
        }
    }
}
