using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FindDupes
{
    public class Program
    {
        private static string[] MainDirs = { "E:\\Other", "S:\\S Hub"};
        private static string[] AllDrives = { "E:\\", "C:\\", "T:\\", "S:\\" };
        private static readonly List<string> ExclusionDirs = new List<string>
        {
            "C:\\$Recycle.Bin",
            //"E:\\Prog",
            "E:\\Music\\Missing Spotify Songs",
            "C:\\Users\\philw\\Source\\Repos",
            "C:\\Program Files",
            "C:\\Windows",
            "C:\\SteamLibrary",
            "C:\\Users\\philw\\AppData",
            "C:\\Users\\All Users",
            "C:\\ProgramData",
            "C:\\inetpub",
            "C:\\ffmpeg",
            "C:\\Users\\Default",
            "C:\\Users\\philw\\.",
            "C:\\Users\\philw\\Documents\\Visual Studio",
            "C:\\Users\\philw\\Desktop\\Code",
            "C:\\xampp",
            "C:\\SSMSTools",
            "C:\\Cakewalk",
            "C:\\Users\\philw\\Desktop\\Tools\\GAMES",
            "C:\\Users\\philw\\Documents\\Native Instruments",
            "C:\\Users\\philw\\Documents\\IK Multimedia",
            "C:\\Users\\philw\\Documents\\PositiveGrid",
            "C:\\Users\\Public\\Documents\\Overloud",
            "C:\\Users\\philw\\Desktop\\55\\Genesis",
            "C:\\Users\\philw\\Desktop\\Work",
            "C:\\Users\\philw\\Documents\\NFS Most Wanted",
        };
        private static readonly List<string> ExclusionExtensions = new List<string>
        {
            ".otf",
            ".auf",
            ".bmp",
            ".itc2",
            ".lnk",
            ".xml",
            ".json",
            ".igpi",
            ".xsl",
            ".log",
            ".csv",
            ".ini",
            ".search-ms",
            ".regtrans-ms",
            ".dll",
            ".tt",
            ".cs",
            ".nupkg",
            ".db"
        };
        private static readonly List<string> ExclusionText = new List<string>
        {
            ".git"
        };
        private const int _10MBish = 10000000;

        static void Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();
            var files = GetFiles(MainDirs);
            var dupes = GetDupes(files);
            var impactingDupes = dupes.Where(f => f.First().Length > _10MBish).ToList();
            WriteDupesToConsole(impactingDupes);
            //WriteDvdTitlesToConsole(files);
            Console.WriteLine("~ fin ~");
            Console.WriteLine($"Took {stopwatch.Elapsed.TotalSeconds} seconds");
        }


        private static IList<FileInfo> GetFiles(params string[] directoryPaths)
        {
            var filesPaths = new List<string>();
            directoryPaths.ToList().ForEach(p => AddFiles(p, filesPaths));
            var files = filesPaths.Select(f => new FileInfo(f)).ToList();
            return files;
        }

        private static void AddFiles(string directoryPath, ICollection<string> filePaths)
        {
            try
            {
                Directory.GetFiles(directoryPath)
                    .Where(f => !ExclusionExtensions.Any(f.EndsWith))
                    .Where(f => !ExclusionText.Any(f.Contains))
                    .ToList()
                    .ForEach(filePaths.Add);

                Directory.GetDirectories(directoryPath)
                    .Where(d => !ExclusionDirs.Any(d.StartsWith))
                    .ToList()
                    .ForEach(d => AddFiles(d, filePaths));
            }
            catch (UnauthorizedAccessException)
            { }
        }

        private static void WriteDupesToConsole(IList<IGrouping<long, FileInfo>> dupes)
        {
            if (!dupes.Any())
                return;

            Console.WriteLine("Dupes\n---------------");
            foreach (var dupeGroup in dupes)
            {
                Console.WriteLine($"{dupeGroup.Key}\n---------------");
                foreach (var fileInfo in dupeGroup)
                    Console.WriteLine(fileInfo.FullName);
                Console.WriteLine();
            }
        }

        private static IList<IGrouping<long, FileInfo>> GetDupes(IList<FileInfo> fileInfos)
        {
            var dupesBySize = GetDupesByLength(fileInfos);
            return FilterDupesToExactMatches(dupesBySize);
        }

        private static List<IGrouping<long, FileInfo>> GetDupesByLength(IList<FileInfo> fileInfos)
        {
            var dupesBySize = fileInfos
                .GroupBy(i => i.Length)
                .Where(g => g.Count() > 1)
                .ToList();
            return dupesBySize;
        }

        private static IList<IGrouping<long, FileInfo>> FilterDupesToExactMatches(List<IGrouping<long, FileInfo>> dupesBySize)
        {
            var allExactlyEqualFiles = new List<FileInfo>();
            foreach (var sameSizeFileGroup in dupesBySize)
            {
                var exactlyEqualFilesInGroup = sameSizeFileGroup.Where(f1 =>
                    sameSizeFileGroup.Any(f2 => f1 != f2 && FileComparer.FilesAreEqual(f1, f2)));

                allExactlyEqualFiles.AddRange(exactlyEqualFilesInGroup);
            }
            var regroupedDupes = allExactlyEqualFiles.GroupBy(f => f.Length).ToList();
            return regroupedDupes;
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
            return parts.Count > 1 ? parts[^2] : "";
        }
    }
}
