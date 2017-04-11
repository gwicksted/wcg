using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace wcg.WebFiles
{
    internal class XsdCompiler
    {
        private readonly XsdCollection _xsds;

        public XsdCompiler(XsdCollection xsds)
        {
            _xsds = xsds;
        }

        public void Compile(Compiland compiland)
        {
            var schemaFile = _xsds.Find(compiland.InputPath);

            var schema = schemaFile.Schema;

            var schemas = _xsds.GetSchemas(schemaFile);

            var importer = new XmlSchemaImporter(schemas, compiland.GenerationOptions, compiland.CodeProvider, compiland.ImportContext);

            var types = ImportTypeMappings(schema, importer).Concat(ImportComplexTypes(schema, importer)).Concat(ImportSimpleTypes(schema, importer));

            var exporter = compiland.CreateCodeExporter();

            foreach (XmlTypeMapping map in types)
            {
                exporter.ExportTypeMapping(map);
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
        
        private IEnumerable<XmlTypeMapping> ImportTypeMappings(XmlSchema schema, XmlSchemaImporter importer)
        {
            foreach (XmlSchemaElement element in schema.Elements.Values)
            {
                Output.ContinuedWith("Import Type Mapping", element.Name);
                yield return importer.ImportTypeMapping(element.QualifiedName);
            }
        }

        private IEnumerable<XmlTypeMapping> ImportComplexTypes(XmlSchema schema, XmlSchemaImporter importer)
        {
            foreach (XmlSchemaComplexType element in schema.Items.OfType<XmlSchemaComplexType>())
            {
                Output.ContinuedWith("Import Complex Types", element.Name);
                yield return importer.ImportSchemaType(element.QualifiedName);
            }
        }

        private IEnumerable<XmlTypeMapping> ImportSimpleTypes(XmlSchema schema, XmlSchemaImporter importer)
        {
            foreach (XmlSchemaSimpleType element in schema.Items.OfType<XmlSchemaSimpleType>())
            {
                Output.ContinuedWith("Import Simple Types", element.Name);
                yield return importer.ImportSchemaType(element.QualifiedName);
            }
        }
    }
}
