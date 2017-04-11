using System.Collections.Generic;
using System.Linq;
using System.Web.Services.Description;
using System.Xml.Schema;

namespace wcg.WebFiles
{
    internal class WsdlCollection
    {
        private readonly string _basePath;
        private readonly FileManager _files;

        private readonly XsdCollection _xsds;

        public WsdlCollection(string basePath, FileManager files, XsdCollection xsds)
        {
            _basePath = basePath;
            _files = files;
            _xsds = xsds;
        }

        private IDictionary<string, WsdlFile> Files { get; } = new Dictionary<string, WsdlFile>();

        public IEnumerable<KeyValuePair<string, WsdlFile>> AllFiles => Files;

        public void Compose()
        {
            foreach (var path in _files.Wsdls.ToArray())
            {
                Add(path);
            }
        }

        public string Add(string path)
        {
            path = _files.GetImportPath(_basePath, path);

            if (!Files.ContainsKey(path))
            {
                var wsdl = new WsdlFile(path, _files.GetOutputPath(path));
                Files.Add(path, wsdl);

                LinkSchemas(wsdl);
                LinkWsdls(wsdl);
            }

            return path;
        }

        private void LinkWsdls(WsdlFile wsdl)
        {
            foreach (var import in wsdl.Imports)
            {
                var importPath = Add(import);

                wsdl.Includes.Add(_files.GetFileName(importPath));

                var imported = Find(importPath);

                foreach (var importedInclude in imported.Includes)
                {
                    wsdl.Includes.Add(importedInclude);
                }
            }
        }

        private void ImportSchema(WsdlFile wsdl, string path)
        {
            var importedPath = _xsds.Add(path);

            wsdl.Includes.Add(_files.GetFileName(importedPath));

            var imported = _xsds.Find(importedPath);

            foreach (var importedInclude in imported.Includes)
            {
                wsdl.Includes.Add(importedInclude);
            }
        }

        private void LinkSchemas(WsdlFile wsdl)
        {
            foreach (XmlSchema wsdlSchema in wsdl.ServiceDescription.Types.Schemas)
            {
                foreach (XmlSchemaObject externalSchema in wsdlSchema.Includes)
                {
                    var import = externalSchema as XmlSchemaImport;

                    if (import != null)
                    {
                        ImportSchema(wsdl, import.SchemaLocation);
                    }
                }
            }
        }

        public WsdlFile Find(string path)
        {
            path = _files.GetImportPath(_basePath, path);

            return Files.TryGetValue(path, out var value) ? value : null;
        }

        public IEnumerable<ServiceDescription> GetServiceDescriptions()
        {
            foreach (var wsdlFile in Files.Values)
            {
                var wsdl = wsdlFile.ServiceDescription;

                foreach (var schemaImport in wsdlFile.Imports)
                {
                    var importNamespace = wsdlFile.ImportNamespace(schemaImport);

                    var nested = Find(schemaImport).ServiceDescription;
                    //nested.Namespaces.Add("wsdl", importNamespace);
                    yield return nested;
                }

                yield return wsdl;
            }
        }

        public ServiceDescriptionImporter GetImporter(string path)
        {
            var wsdl = Files[path];

            var wsdlImporter = new ServiceDescriptionImporter();
            
            wsdlImporter.AddServiceDescription(wsdl.ServiceDescription, null, null);

            foreach (var import in wsdl.ServiceImports)
            {
                var serviceDescription = Find(import.Location).ServiceDescription;
                //serviceDescription.Namespaces.Add("wsdl", import.Namespace);
                wsdlImporter.AddServiceDescription(serviceDescription, null, null);
            }
        
            return wsdlImporter;
        }
    }
}
