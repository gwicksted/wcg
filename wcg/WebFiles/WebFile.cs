using System.Collections.Generic;

namespace wcg.WebFiles
{
    internal abstract class WebFile
    {
        protected WebFile(string inputPath, string outputPath)
        {
            InputPath = inputPath;
            OutputPath = outputPath;

        }
        public string InputPath { get; }

        public string OutputPath { get; }

        public string[] Imports { get; protected set; }

        public IList<string> Includes { get; } = new List<string>();
    }
}
