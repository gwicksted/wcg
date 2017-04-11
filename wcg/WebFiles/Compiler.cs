using System.Collections.Generic;
using System.Linq;
using wcg.CodeGeneration;

namespace wcg.WebFiles
{
    internal class Compiler
    {
        private readonly FileManager _fileManager;
        private readonly XsdCollection _xsds;
        private readonly WsdlCollection _wsdls;

        private readonly XsdCompiler _xsdc;
        private readonly WsdlCompiler _wsdlc;

        private readonly string _namespace;

        private readonly IPostProcessor _postProcessor;

        public Compiler(string codeNamespace, FileManager fileManager, string basePath, IPostProcessor postProcessor)
        {
            _namespace = codeNamespace;
            _fileManager = fileManager;
            _xsds = new XsdCollection(basePath, _fileManager);
            _wsdls = new WsdlCollection(basePath, _fileManager, _xsds);
            _xsdc = new XsdCompiler(_xsds);
            _wsdlc = new WsdlCompiler(_wsdls, _xsds);
            _postProcessor = postProcessor;
        }

        public IEnumerable<Compiland> GetXsdCompilands()
        {
            _xsds.Compose();

            foreach (var xsdFile in _xsds.AllFiles)
            {
                yield return new Compiland(_namespace + "." + _fileManager.GetFileName(xsdFile.Key), xsdFile.Value.Schema.TargetNamespace, xsdFile.Key, _fileManager.GetOutputPath(xsdFile.Key), xsdFile.Value.Includes.Select(i => _namespace + "." + i).ToArray());
            }
        }

        public IEnumerable<Compiland> GetWsdlCompilands()
        {
            _wsdls.Compose();

            foreach (var wsdlFile in _wsdls.AllFiles)
            {
                yield return new Compiland(_namespace + "." + _fileManager.GetFileName(wsdlFile.Key), wsdlFile.Value.ServiceDescription.TargetNamespace, wsdlFile.Key, _fileManager.GetOutputPath(wsdlFile.Key), wsdlFile.Value.Includes.Select(i => _namespace + "." + i).ToArray());
            }
        }

        public void CompileXsd(Compiland compiland)
        {
            Output.Timed("Compilation");
            _xsdc.Compile(compiland);
            Output.EndTimed();

            Output.Timed("Post Processing");
            _postProcessor.SchemaNamespace = compiland.SourceNamespace;
            _postProcessor.PostProcess(compiland.CodeNamespace);
            Output.EndTimed();

            Output.Timed("Linking");
            _xsdc.Link(compiland);
            Output.EndTimed();

            Output.Timed("Writing");
            _xsdc.Write(compiland);
            Output.EndTimed();
        }

        public void CompileWsdl(Compiland compiland)
        {
            Output.Timed("Compilation");
            _wsdlc.Compile(compiland);
            Output.EndTimed();

            Output.Timed("Post Processing");
            _postProcessor.SchemaNamespace = compiland.SourceNamespace;
            _postProcessor.PostProcess(compiland.CodeNamespace);
            Output.EndTimed();

            Output.Timed("Linking");
            _wsdlc.Link(compiland);
            Output.EndTimed();

            Output.Timed("Writing");
            _wsdlc.Write(compiland);
            Output.EndTimed();
        }
    }
}
