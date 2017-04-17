using System;
using System.Collections.Generic;
using System.IO;

namespace wcg.CodeGeneration
{
    internal class SolutionFileGenerator
    {
        private readonly string _file;

        private readonly string _projectFile;
        private readonly Guid _projectGuid;

        public SolutionFileGenerator(string fileName, string projectFile, Guid projectGuid)
        {
            _file = fileName;
            _projectFile = projectFile;
            _projectGuid = projectGuid;
            SolutionGuid = Guid.NewGuid();
        }

        public Guid SolutionGuid { get; }

        public void Compile()
        {
            // TODO: handle relative nested paths and use absolute paths where neccessary

            using (var stream = new FileStream(_file, FileMode.Create, FileAccess.ReadWrite, FileShare.Read))
            {
                using (var writer = new StreamWriter(stream))
                {
                    string projectGuid = _projectGuid.ToString().ToUpper();
                    writer.WriteLine(@"
Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 15
VisualStudioVersion = 15.0.26403.3
MinimumVisualStudioVersion = 10.0.40219.1
Project(""{" + SolutionGuid.ToString().ToUpper() + @"}"") = """ + Path.GetFileNameWithoutExtension(_projectFile) + @""", """ + RelativePaths.GetRelativePath(_file, _projectFile) + @""", ""{" + projectGuid + @"}""
EndProject
Global
    GlobalSection(SolutionConfigurationPlatforms) = preSolution
        Debug|Any CPU = Debug|Any CPU
        Release|Any CPU = Release|Any CPU
    EndGlobalSection
    GlobalSection(ProjectConfigurationPlatforms) = postSolution
        {" + projectGuid + @"}.Debug|Any CPU.ActiveCfg = Debug|Any CPU
        {" + projectGuid + @"}.Debug|Any CPU.Build.0 = Debug|Any CPU
        {" + projectGuid + @"}.Release|Any CPU.ActiveCfg = Release|Any CPU
        {" + projectGuid + @"}.Release|Any CPU.Build.0 = Release|Any CPU
    EndGlobalSection
    GlobalSection(SolutionProperties) = preSolution
        HideSolutionNode = FALSE
    EndGlobalSection
EndGlobal
");
                }
            }
        }
    }
}
