using System;
using System.IO;

namespace wcg.CodeGeneration
{
    internal static class RelativePaths
    {
        public static string GetRelativePath(string relativeTo, string filePath)
        {
            string dir = NormalizeDir(Path.GetDirectoryName(relativeTo) ?? Directory.GetCurrentDirectory());

            string fileDir = NormalizeDir(Path.GetDirectoryName(filePath) ?? dir);
            string fileName = Path.GetFileName(filePath);

            if (dir.Equals(fileDir, StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            if (fileDir.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
            {
                string partial = fileDir.Substring(dir.Length);

                return Path.Combine(partial, fileName);
            }

            return filePath;
        }

        private static string NormalizeDir(string dir)
        {
            if (Path.AltDirectorySeparatorChar != Path.DirectorySeparatorChar)
            {
                dir = dir.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            dir = dir.EndsWith(Path.DirectorySeparatorChar.ToString()) ? dir : dir + Path.DirectorySeparatorChar;

            return dir;
        }
    }
}
