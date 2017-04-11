using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Services.Description;
using System.Xml.Schema;

namespace wcg.WebFiles
{
    internal class WsdlCompiler
    {
        private readonly WsdlCollection _wsdls;
        private readonly XsdCollection _xsds;

        public WsdlCompiler(WsdlCollection wsdls, XsdCollection xsds)
        {
            _xsds = xsds;
            _wsdls = wsdls;
        }

        public void Compile(Compiland compiland)
        {
            var wsdlImporter = new ServiceDescriptionImporter
            {
                //ProtocolName = "Soap12", //new Soap12ProtocolImporter().ProtocolName
                ProtocolName = "Soap", // new SoapProtocolImporter().ProtocolName
                Style = ServiceDescriptionImportStyle.Client,
                CodeGenerationOptions = compiland.GenerationOptions
            };
            
            var wsdl = _wsdls.Find(compiland.InputPath);
            
            ImportIncludes(wsdl.ServiceDescription, wsdlImporter);
            AddServiceDescriptions(wsdl, wsdlImporter);


            var importWarning = wsdlImporter.Import(compiland.CodeNamespace, compiland.CreateCodeUnit());

            if (importWarning == 0)
            {
                //GenerateCode(codeNamespace, path, wsdl.TargetNamespace);
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

        private IEnumerable<ServiceDescription> LoadWsdls(string path, string importNamespace)
        {
            var wsdl = _wsdls.Find(path);

            ServiceDescription serviceDescription = wsdl.ServiceDescription;
            //serviceDescription.Namespaces.Add("wsdl", importNamespace);

            yield return serviceDescription;

            foreach (var schemaImport in wsdl.Imports)
            {
                foreach (var nested in LoadWsdls(schemaImport, wsdl.ImportNamespace(schemaImport)))
                {
                    yield return nested;
                }
            }
        }

        private void AddServiceDescriptions(WsdlFile wsdl, ServiceDescriptionImporter wsdlImporter)
        {
            wsdlImporter.AddServiceDescription(wsdl.ServiceDescription, null, null);

            foreach (var schemaImport in wsdl.ServiceImports)
            {
                foreach (var serviceDescription in LoadWsdls(schemaImport.Location, schemaImport.Namespace))
                {
                    Output.ContinuedWith("Importing external wsdl", schemaImport.Location);
                    wsdlImporter.AddServiceDescription(serviceDescription, null, null);
                }
            }
        }

        private void ImportSchema(string importPath, ServiceDescriptionImporter wsdlImporter)
        {
            var schema = _xsds.Find(importPath);

            if (!wsdlImporter.Schemas.Contains(schema.Schema))
            {
                Output.ContinuedWith("Importing schema", importPath);

                wsdlImporter.Schemas.Add(schema.Schema);
            }

            foreach (var schemaImport in schema.Imports)
            {
                ImportSchema(schemaImport, wsdlImporter);
            }
        }

        private void ImportIncludes(ServiceDescription wsdl, ServiceDescriptionImporter wsdlImporter)
        {
            foreach (XmlSchema wsdlSchema in wsdl.Types.Schemas)
            {
                foreach (XmlSchemaObject externalSchema in wsdlSchema.Includes)
                {
                    var import = externalSchema as XmlSchemaImport;

                    if (import != null)
                    {
                        ImportSchema(import.SchemaLocation, wsdlImporter);
                    }
                }
            }
        }

        public void Link(Compiland compiland)
        {
            compiland.Link();
        }

        public void Write(Compiland compiland)
        {
            compiland.WriteCode();
        }
    }
}
