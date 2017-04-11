using System.Linq;
using System.Web.Services.Description;

namespace wcg.WebFiles
{
    internal class WsdlFile : WebFile
    {
        public WsdlFile(string inputPath, string outputPath) : base(inputPath, outputPath)
        {
            ServiceDescription = ServiceDescription.Read(InputPath);

            ServiceImports = ServiceDescription.Imports.OfType<Import>().ToArray();

            Imports = ServiceImports.Select(import => import.Location).ToArray();

            foreach (var import in ServiceImports)
            {
                ServiceDescription.Namespaces.Add("wsdl", import.Namespace);
            }
        }

        public ServiceDescription ServiceDescription { get; }

        public Import[] ServiceImports { get; }

        public string ImportNamespace(string import)
        {
            return ServiceImports.Where(i => i.Location == import).Select(i => i.Namespace).FirstOrDefault();
        }
    }
}
