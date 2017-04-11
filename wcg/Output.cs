using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace wcg
{
    internal static class Output
    {
        private const string Indent = "  ";
        private const char LineChar = '=';

        private static readonly ConsoleColor FieldNameColor = ConsoleColor.Cyan;
        private static readonly ConsoleColor FieldDescriptionColor = ConsoleColor.Green;
        private static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
        private static readonly ConsoleColor SectionColor = ConsoleColor.Yellow;
        private static readonly ConsoleColor ImportantColor = ConsoleColor.White;
        private static readonly ConsoleColor InfoColor = ConsoleColor.Gray;
        private static readonly ConsoleColor LineColor = ConsoleColor.Gray;
        private static readonly ConsoleColor SubInfoColor = ConsoleColor.DarkGray;

        private static readonly Stopwatch _stopwatch = new Stopwatch();

        public static void Warning(string message)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(message);
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static void DisplayError(string message, string trace)
        {
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(message);

            if (!string.IsNullOrEmpty(trace))
            {
                Console.WriteLine(trace);
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
        }

        public static void AnyKey()
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("Press any key to exit");
            Console.ReadKey(true);
        }

        public static void Line()
        {
            Console.ForegroundColor = LineColor;
            Console.WriteLine(new string(LineChar, Console.WindowWidth - 5));
        }

        public static void Reset()
        {
            Console.CursorVisible = true;

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ResetColor();
        }

        public static void Success(string info)
        {
            Console.ForegroundColor = SuccessColor;
            Console.WriteLine(info);
        }

        public static void SetTitle(string title)
        {
            Console.CursorVisible = false;

            Console.Title = title;
            Console.OutputEncoding = Encoding.UTF8;
        }

        public static void DisplayImportant(string info, bool indented = false)
        {
            Console.ForegroundColor = ImportantColor;
            Console.WriteLine($"{(indented ? Indent : string.Empty)}{info}");
        }

        public static void DisplayInfo(string info)
        {
            Console.ForegroundColor = InfoColor;
            Console.WriteLine(info);
        }

        public static void DisplaySubInfo(string info)
        {
            Console.ForegroundColor = SubInfoColor;
            Console.WriteLine(info);
            Console.WriteLine();
        }

        public static void ContinuedWith(string action, string item)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.WriteLine($"... {action} {item}");
        }

        public static void Added(string item, string description)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("[+] ");
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.Write(item);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" {description}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Removed(string item, string description)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("[-] ");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.Write(item);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($" {description}");
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static void Action(string action, string item)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($"{action} ");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(item);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static bool YesNo(string question, string description, string yesDoes, string noDoes)
        {
            while (true)
            {
                DisplayInfo(question);
                DisplaySubInfo(description);
                DisplayImportant($"[y] {yesDoes}, [n] {noDoes}");
                Console.WriteLine();
                Console.CursorVisible = true;
                Console.ForegroundColor = InfoColor;
                Console.Write("> ");
                var key = Console.ReadKey(true);

                if (key.KeyChar == 'y' || key.KeyChar == 'Y')
                {
                    Console.CursorVisible = false;
                    return true;
                }

                if (key.KeyChar == 'n' || key.KeyChar == 'N')
                {
                    Console.CursorVisible = false;
                    return false;
                }

                DisplaySubInfo("Try again.");
            }
        }

        public static void DisplaySubList(IEnumerable<string> list)
        {
            Console.ForegroundColor = SubInfoColor;
            foreach (var item in list)
            {
                Console.WriteLine($"{Indent}{item}");
            }
        }

        public static void Timed(string section)
        {
            Console.ForegroundColor = SectionColor;
            Console.WriteLine($"{section} ...");
            Console.ForegroundColor = InfoColor;
            _stopwatch.Restart();
        }

        public static void EndTimed()
        {
            _stopwatch.Stop();
            var ms = _stopwatch.ElapsedMilliseconds;
            Console.ForegroundColor = SectionColor;
            Console.WriteLine($"== {ms}ms ==");
            Console.ForegroundColor = InfoColor;
        }

        public static void WriteSection(string section)
        {
            Console.WriteLine();
            Console.ForegroundColor = SectionColor;
            Console.WriteLine($"{section}:");
            Console.ForegroundColor = InfoColor;
        }

        public static void EndSection()
        {
            Console.WriteLine();
            Console.ForegroundColor = InfoColor;
        }

        public static void WriteIndented(string prefix, string name, string equals, string separator, string shortNamePrefix, string shortName, string shortNameEquals, string value, string description)
        {
            string ln = !string.IsNullOrEmpty(name) ? $"{prefix ?? string.Empty}{name}{equals ?? string.Empty}{value ?? string.Empty}" : string.Empty;
            string sn = !string.IsNullOrEmpty(shortName) ? $"{shortNamePrefix ?? string.Empty}{shortName}{shortNameEquals ?? string.Empty}{value ?? string.Empty}" : string.Empty;

            string sep = !string.IsNullOrEmpty(ln) && !string.IsNullOrEmpty(sn) ? separator : string.Empty;

            Console.ForegroundColor = FieldNameColor;
            Console.Write($"{Indent}{sn}{sep}{ln}");
            Console.CursorLeft = Console.WindowWidth / 3;
            Console.ForegroundColor = FieldDescriptionColor;
            Console.WriteLine(description);
        }

        public static void NameValue(string name, string value)
        {
            Console.ForegroundColor = FieldNameColor;
            int offset = (Console.WindowWidth / 3) - ((name?.Length ?? 0) + 2);
            offset = offset > 0 ? offset : 0;

            Console.CursorLeft = offset;
            Console.Write($"{name ?? string.Empty}  ");
            Console.CursorLeft = Console.WindowWidth / 3;
            Console.ForegroundColor = FieldDescriptionColor;
            Console.WriteLine(value);
        }

        public static void WriteIndented(string prefix, string name, string equals, string value, string description)
        {
            Console.ForegroundColor = FieldNameColor;
            Console.Write($"{Indent}{prefix ?? string.Empty}{name ?? string.Empty}{equals ?? string.Empty}{value ?? string.Empty}");
            Console.CursorLeft = Console.WindowWidth / 3;
            Console.ForegroundColor = FieldDescriptionColor;
            Console.WriteLine(description);
        }
    }
}
