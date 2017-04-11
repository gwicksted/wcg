using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace wcg.WebFiles
{
    internal class XsdCollection
    {
        private readonly string _basePath;
        private readonly FileManager _files;

        public XsdCollection(string basePath, FileManager files)
        {
            _basePath = basePath;
            _files = files;
        }

        private IDictionary<string, XsdFile> Files { get; } = new Dictionary<string, XsdFile>();

        public IEnumerable<KeyValuePair<string, XsdFile>> AllFiles => Files;

        public void Compose()
        {
            foreach (var path in _files.Xsds.ToArray())
            {
                Add(path);
            }
        }

        public string Add(string path)
        {
            path = _files.GetImportPath(_basePath, path);

            if (!Files.ContainsKey(path))
            {
                var xsd = new XsdFile(path, _files.GetOutputPath(path));
                Files.Add(path, xsd);

                foreach (var import in xsd.Imports)
                {
                    ImportSchema(xsd, import);
                }
            }

            return path;
        }

        private void ImportSchema(XsdFile xsd, string path)
        {
            string importPath = Add(path);
            xsd.Includes.Add(_files.GetFileName(importPath));
            
            var included = Find(importPath);

            foreach (var includedImport in included.Imports)
            {
                xsd.Includes.Add(_files.GetFileName(includedImport));
            }
        }

        public XsdFile Find(string path)
        {
            path = _files.GetImportPath(_basePath, path);

            return Files.TryGetValue(path, out var value) ? value : null;
        }

        public XmlSchemas GetSchemas(XsdFile scope)
        {
            var xsds = new XmlSchemas();

            IncludeSchemas(scope, xsds);

            xsds.Compile(null, true);

            return xsds;
        }

        private void IncludeSchemas(XsdFile scope, XmlSchemas xsds)
        {
            xsds.Add(scope.Schema);

            foreach (var import in scope.Imports)
            {
                var xsd = Find(import);
                IncludeSchemas(xsd, xsds);
            }
        }
    }
}
