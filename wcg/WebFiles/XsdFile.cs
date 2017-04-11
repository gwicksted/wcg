using System.IO;
using System.Linq;
using System.Xml.Schema;

namespace wcg.WebFiles
{
    internal class XsdFile : WebFile
    {
        public XsdFile(string inputPath, string outputPath) : base(inputPath, outputPath)
        {
            Schema = ReadSchema(InputPath);
            Imports = GetImports(Schema);
        }

        private static XmlSchema ReadSchema(string path)
        {
            using (var fileStream = File.OpenRead(path))
            {
                return XmlSchema.Read(fileStream, null);
            }
        }

        private static string[] GetImports(XmlSchema schema)
        {
            return schema.Includes.OfType<XmlSchemaImport>().Select(s => s.SchemaLocation).ToArray();
        }

        public XmlSchema Schema { get; }
    }
}
