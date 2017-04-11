using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Services.Description;
using System.Xml.Schema;
using System.Xml.Serialization;
using Microsoft.CSharp;
using wcg.CodeGeneration;
using wcg.WebFiles;

namespace wcg
{
    /*
    internal class WsdlCompiler : IDisposable
    {
        private readonly IDictionary<string, XmlSchema> _xsds = new Dictionary<string, XmlSchema>();

        private readonly IDictionary<string, ServiceDescription> _wsdls = new Dictionary<string, ServiceDescription>();
        
        private readonly string _namespace;
        
        private readonly XmlSchemas _imported = new XmlSchemas();
        private XmlSchemaImporter _schemaImporter;

        private readonly IPostProcessor _postProcessor;
        private readonly FileManager _files;
        private readonly CodeDomProvider _codeProvider;

        private readonly CodeGeneratorOptions _codeGeneratorOptions;

        private readonly CodeGenerationOptions _generationOptions;

        private readonly ImportContext _importContext;

        public WsdlCompiler(IPostProcessor postProcessor, string codeNamespace, FileManager files)
        {
            _postProcessor = postProcessor;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _namespace = codeNamespace ?? string.Empty;
            
            _importContext = new ImportContext(new CodeIdentifiers(), false);
            
            _codeProvider = new CSharpCodeProvider();

            _generationOptions = CodeGenerationOptions.GenerateProperties | CodeGenerationOptions.GenerateOldAsync | CodeGenerationOptions.GenerateNewAsync;

            _codeGeneratorOptions = new CodeGeneratorOptions
            {
                BlankLinesBetweenMembers = true,
                BracingStyle = "C",
                ElseOnClosing = false,
                IndentString = "    ",
                VerbatimOrder = false
            };
            
            foreach (string xsd in _files.Xsds.ToArray())
            {
                Parse(xsd);
            }
            
            foreach (string wsdl in _files.Wsdls.ToArray())
            {
                Parse(wsdl);
            }

            foreach (var xsd in _xsds)
            {
                CompileSchema(xsd.Key, xsd.Value);
            }

            foreach (var wsdl in _wsdls)
            {
                CompileWsdl(wsdl.Key, wsdl.Value);
            }
        }
        
        private void Parse(string path)
        {
            if (_files.IsWsdl(path))
            {
                Console.WriteLine($"Parsing {path}");

                ServiceDescription wsdl = ServiceDescription.Read(path);
                
                _wsdls.Add(path, wsdl);
                
                foreach (Import import in wsdl.Imports)
                {
                    string importFile = _files.GetImportPath(path, import.Location);

                    if (_files.TryAddPath(importFile))
                    {
                        Parse(importFile);
                    }
                }
            }
            else
            {
                Console.WriteLine($"Parsing {path}");

                using (var s = File.OpenRead(path))
                {
                    var schema = XmlSchema.Read(s, null);
                    
                    _xsds.Add(path, schema);

                    _imported.Add(schema);

                    foreach (var import in schema.Includes.OfType<XmlSchemaImport>())
                    {
                        var importFile = _files.GetImportPath(path, import.SchemaLocation);

                        if (_files.TryAddPath(importFile))
                        {
                            Parse(importFile);
                        }
                    }
                }
            }
        }
        
        private IEnumerable<XmlTypeMapping> ImportTypeMappings(XmlSchema schema)
        {
            foreach (XmlSchemaElement element in schema.Elements.Values)
            {
                Output.ContinuedWith("Import Type Mapping", element.Name);
                yield return _schemaImporter.ImportTypeMapping(element.QualifiedName);
            }
        }

        private IEnumerable<XmlTypeMapping> ImportComplexTypes(XmlSchema schema)
        {
            foreach (XmlSchemaComplexType element in schema.Items.OfType<XmlSchemaComplexType>())
            {
                Output.ContinuedWith("Import Complex Types", element.Name);
                yield return _schemaImporter.ImportSchemaType(element.QualifiedName);
            }
        }

        private IEnumerable<XmlTypeMapping> ImportSimpleTypes(XmlSchema schema)
        {
            foreach (XmlSchemaSimpleType element in schema.Items.OfType<XmlSchemaSimpleType>())
            {
                Output.ContinuedWith("Import Simple Types", element.Name);
                yield return _schemaImporter.ImportSchemaType(element.QualifiedName);
            }
        }

        private XmlSchemaImporter CreateImporter()
        {
            var xsds = new XmlSchemas();

            foreach (var xsd in _xsds.Values)
            {
                xsds.Add(xsd);
            }

            xsds.Compile(null, true);

            return new XmlSchemaImporter(xsds, _generationOptions, _codeProvider, _importContext);
        }

        private void CompileSchema(string path, XmlSchema schema)
        {
            Output.Line();
            Output.Action("Compiling", path);
            Output.Line();

            CodeNamespace codeNamespace = new CodeNamespace(_namespace);
            XmlCodeExporter codeExporter = new XmlCodeExporter(codeNamespace);
            
            _schemaImporter = CreateImporter();
            

            var types = ImportTypeMappings(schema).Concat(ImportComplexTypes(schema)).Concat(ImportSimpleTypes(schema));

            foreach (XmlTypeMapping map in types)
            {
                codeExporter.ExportTypeMapping(map);
            }

            GenerateCode(codeNamespace, path, schema.TargetNamespace);
        }

        private void GenerateCode(CodeNamespace codeNamespace, string path, string targetNamespace)
        {
            if (_postProcessor != null)
            {
                _postProcessor.SchemaNamespace = targetNamespace;
                _postProcessor.PostProcess(codeNamespace);
            }

            using (var fileStream = new FileStream(_files.GetOutputPath(path), FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var output = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    _codeProvider.GenerateCodeFromNamespace(codeNamespace, output, _codeGeneratorOptions);
                }
            }
        }

        private void ImportIncludes(ServiceDescription wsdl, string path, ServiceDescriptionImporter wsdlImporter)
        {
            foreach (XmlSchema wsdlSchema in wsdl.Types.Schemas)
            {
                foreach (XmlSchemaObject externalSchema in wsdlSchema.Includes)
                {
                    var import = externalSchema as XmlSchemaImport;

                    if (import != null)
                    {
                        string importPath = import.SchemaLocation;

                        foreach (var schema in LoadSchemas(path, importPath))
                        {
                            if (!wsdlImporter.Schemas.Contains(schema))
                            {
                                wsdlImporter.Schemas.Add(schema);
                            }
                        }
                    }
                }
            }
        }

        private void AddServiceDescriptions(ServiceDescription wsdl, string path, ServiceDescriptionImporter wsdlImporter)
        {
            wsdlImporter.AddServiceDescription(wsdl, null, null);

            foreach (var schemaImport in wsdl.Imports.OfType<Import>())
            {
                foreach (var serviceDescription in LoadWsdls(path, schemaImport.Namespace, schemaImport.Location))
                {
                    wsdlImporter.AddServiceDescription(serviceDescription, null, null);
                }
            }
        }

        private void CompileWsdl(string path, ServiceDescription wsdl)
        {
            Output.Line();
            Output.Action("Compiling", path);
            Output.Line();
    
            var wsdlImporter = new ServiceDescriptionImporter
            {
                //ProtocolName = "Soap12", //new Soap12ProtocolImporter().ProtocolName
                ProtocolName = "Soap", // new SoapProtocolImporter().ProtocolName
                Style = ServiceDescriptionImportStyle.Client,
                CodeGenerationOptions = _generationOptions
            };


            // Add any imported files
            ImportIncludes(wsdl, path, wsdlImporter);
            AddServiceDescriptions(wsdl, path, wsdlImporter);
            
            CodeNamespace codeNamespace = new CodeNamespace(_namespace);
            CodeCompileUnit codeUnit = new CodeCompileUnit();
            codeUnit.Namespaces.Add(codeNamespace);

            
            var importWarning = wsdlImporter.Import(codeNamespace, codeUnit);
            
            if (importWarning == 0)
            {
                GenerateCode(codeNamespace, path, wsdl.TargetNamespace);
            }
            else
            {
                if (importWarning.HasFlag(ServiceDescriptionImportWarnings.NoCodeGenerated))
                {
                    Output.Warning("No code could be generated!");
                }
                else
                {
                    Output.Warning("ERROR: " + importWarning);
                }
            }
        }


        private IEnumerable<XmlSchema> LoadSchemas(string path, string import)
        {
            string importPath = _files.GetImportPath(path, import);

            XmlSchema xmlSchema;
            if (_xsds.TryGetValue(importPath, out xmlSchema))
            {
                yield return xmlSchema;

                foreach (var nested in xmlSchema.Includes.OfType<XmlSchemaImport>().Where(s => !string.IsNullOrEmpty(s.SchemaLocation)))
                {
                    foreach (var schema in LoadSchemas(path, nested.SchemaLocation))
                    {
                        yield return schema;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"Unable to import schema: {import} at {importPath}");
            }
        }

        private IEnumerable<ServiceDescription> LoadWsdls(string path, string importNamespace, string import)
        {
            string importPath = _files.GetImportPath(path, import);

            ServiceDescription serviceDescription;
            if (_wsdls.TryGetValue(importPath, out serviceDescription))
            {
                serviceDescription.Namespaces.Add("wsdl", importNamespace);

                yield return serviceDescription;

                foreach (var schemaImport in serviceDescription.Imports.OfType<Import>())
                {
                    foreach (var nested in LoadWsdls(path, schemaImport.Namespace, schemaImport.Location))
                    {
                        yield return nested;
                    }
                }
            }
        }

        public void Dispose()
        {
            _codeProvider.Dispose();
        }
    }*/
}
