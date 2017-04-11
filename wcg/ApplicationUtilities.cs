using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace wcg
{
    internal static class ApplicationUtilities
    {
        public static string ExeFile => Assembly.GetExecutingAssembly().Location;

        public static string ExeName => Path.GetFileNameWithoutExtension(ExeFile);

        public static string Version => Assembly.GetExecutingAssembly().GetName().Version.ToString();

        public static string Application => Assembly.GetExecutingAssembly().GetName().Name;

        public static string Company => FileVersionInfo.GetVersionInfo(ExeFile).CompanyName;

        public static string Description => ((AssemblyDescriptionAttribute)Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyDescriptionAttribute))).Description;

        public static string Copyright => FileVersionInfo.GetVersionInfo(ExeFile).LegalCopyright;
    }
}
