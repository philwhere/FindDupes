using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FindDupes
{
    public class Program
    {
        private const string MainDir = "E:\\Other";
        private static string[] AllDrives = { "E:\\", "C:\\", "T:\\", "S:\\" };
        private static readonly List<string> ExclusionDirs = new List<string>
        {
            "E:\\Prog",
            "E:\\Music\\Missing Spotify Songs",
            "C:\\Users\\philw\\Source\\Repos",
            "C:\\Program Files",
            "C:\\Windows",
            "C:\\SteamLibrary",
            "C:\\Users\\philw\\AppData",
            "C:\\Users\\All Users",
            "C:\\ProgramData",
            "C:\\Users\\philw\\.",
            "C:\\Users\\philw\\Documents\\Visual Studio",
            "C:\\Users\\philw\\Desktop\\Code",
            "C:\\xampp"
        };

        static void Main(string[] args)
        {
            var files = GetFiles(MainDir);
            var dupes = GetDupes(files);
            WriteDupesToConsole(dupes);
            //WriteDvdTitlesToConsole(files);
            Console.WriteLine("~ fin ~");
        }

        private static List<FileInfo> GetFiles(params string[] directoryPaths)
        {
            var filesPaths = new List<string>();
            directoryPaths.ToList().ForEach(p => AddFiles(p, filesPaths));
            var files = filesPaths.Select(f => new FileInfo(f)).ToList();
            return files;
        }

        private static void AddFiles(string path, IList<string> files)
        {
            try
            {
                Directory.GetFiles(path)
                    .ToList()
                    .ForEach(files.Add);

                Directory.GetDirectories(path)
                    .Where(d => !ExclusionDirs.Any(d.StartsWith))
                    .ToList()
                    .ForEach(s => AddFiles(s, files));
            }
            catch (UnauthorizedAccessException)
            { }
        }

        private static void WriteDupesToConsole(IList<IGrouping<long, FileInfo>> dupes)
        {
            if (!dupes.Any())
                return;

            Console.WriteLine("Dupes\n---------------");
            foreach (var dupe in dupes)
            {
                Console.WriteLine($"{dupe.Key}\n---------------");
                foreach (var fileInfo in dupe)
                    Console.WriteLine(fileInfo.FullName);
                Console.WriteLine();
            }
        }

        private static IList<IGrouping<long, FileInfo>> GetDupes(IEnumerable<FileInfo> fileInfos)
        {
            var dupesBySize = fileInfos.GroupBy(i => i.Length).Where(g => g.Count() > 1).ToList();
            //dupesBySize.RemoveAll(dupeGroup => !AllExactlyEqual(dupeGroup));

            return dupesBySize;
        }

        private static bool AllExactlyEqual(IGrouping<long, FileInfo> dupeGroup)
        {
            return dupeGroup.All(file =>
                dupeGroup.All(f => file == f || FileComparer.FilesAreEqual(file, f)));
        }

        private static void WriteDvdTitlesToConsole(IEnumerable<FileInfo> fileInfos)
        {
            var dvdTitles = fileInfos.GroupBy(i => GetDvdMatch(i.Name)).OrderBy(g => g.Key);
            foreach (var thing in dvdTitles)
                Console.WriteLine(thing.Key);
        }

        private static string GetDvdMatch(string fileName)
        {
            var parts = fileName.Split(" - ").ToList();
            var last = parts.IndexOf(parts.Last());
            return parts.Count > 1 ? parts[last - 1] : "";
        }
    }
}
