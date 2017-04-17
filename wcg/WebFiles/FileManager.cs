using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace wcg.WebFiles
{
    internal class FileManager
    {
        private const string WebservicePostfix = "Endpoint";
        private const string GeneratedExtension = ".cs";

        private readonly HashSet<string> _xsds = new HashSet<string>();

        private readonly HashSet<string> _wsdls = new HashSet<string>();

        private readonly string _output;

        public FileManager(string outputDirectory, IEnumerable<string> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            if (string.IsNullOrEmpty(outputDirectory))
            {
                throw new ArgumentNullException(nameof(outputDirectory));
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            _output = outputDirectory;
            
            foreach (string path in paths)
            {
                TryAddPath(path);
            }
        }

        public IEnumerable<string> Xsds => _xsds.ToArray();

        public IEnumerable<string> Wsdls => _wsdls.ToArray();

        public bool IsWsdl(string path)
        {
            return Path.GetExtension(path).Equals(".wsdl", StringComparison.OrdinalIgnoreCase);
        }

        public bool TryAddPath(string path)
        {
            path = Path.GetFullPath(path);

            if (!File.Exists(path))
            {
                return false;
            }

            if (IsWsdl(path))
            {
                return _wsdls.Add(path);
            }

            return _xsds.Add(path);
        }

        public string GetImportPath(string path, string import)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (string.IsNullOrEmpty(import))
            {
                throw new ArgumentNullException(nameof(import));
            }

            path = Path.GetFullPath(path);

            string dir = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            
            string importFile = Path.GetFileName(import);
            string importDir = Path.GetDirectoryName(import);

            if (string.IsNullOrEmpty(importFile))
            {
                throw new ArgumentNullException(nameof(import));
            }

            if (!string.IsNullOrEmpty(importDir))
            {
                if (Path.IsPathRooted(importDir))
                {
                    string fullPath = Path.GetFullPath(Path.Combine(importDir, importFile));

                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
                else
                {
                    string fullPath = Path.GetFullPath(Path.Combine(dir, importDir, importFile));

                    if (File.Exists(fullPath))
                    {
                        return fullPath;
                    }
                }
            }

            return Path.GetFullPath(Path.Combine(dir, importFile));
        }

        public string GetFileName(string path, string ext = WebservicePostfix)
        {
            var fileName = Path.GetFileNameWithoutExtension(path);

            if (IsWsdl(path) && !fileName.EndsWith(WebservicePostfix, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName + ext;
            }

            return fileName;
        }

        public string GetOutputPath(string path, string ext = GeneratedExtension)
        {
            var fileName = GetFileName(path);

            return Path.GetFullPath(Path.Combine(_output, fileName + ext));
        }
    }
}
