using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FindDupes
{
    public class Program
    {
        private static readonly string[] MainDirs = [
            @"S:\S Hub",
            @"G:\E Hub",
            @"G:\G Hub",
            @"G:\T Hub 2",
            @"G:\temporary T drive",
            @"T:\T Hub",
            @"C:\Users\philw\Desktop\convert",
            @"C:\Users\philw\Desktop\_PROCCCCC",
            @"C:\Users\philw\Downloads",
        ];

        static async Task Main(string[] args)
        {
            var stopwatch = Stopwatch.StartNew();
            var files = GetFiles(MainDirs);
            var dupes = GetDupes(files);
            await WriteDupesToConsole(dupes);
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
                var videoExtensions = new[] { "mp4", "mkv", "wmv", "flv", "avi", "mov", "mpg", "mpeg", "m4v", "3gp", "webm" };
                var patterns = videoExtensions.Select(e => $"*.{e}");
                foreach ( var pattern in patterns) {
                    var files = Directory.GetFiles(directoryPath, pattern, SearchOption.AllDirectories);
                    foreach (var file in files)
                        filePaths.Add(file);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Console.WriteLine($"Unauthorized access to {directoryPath}");
                throw;
            }
        }

        private static async Task WriteDupesToConsole(IList<IGrouping<long, FileInfo>> dupes)
        {
            if (!dupes.Any())
                return;

            Console.WriteLine("Dupes\n---------------");
            foreach (var dupeGroup in dupes)
            {
                Console.WriteLine($"{dupeGroup.Key}\n---------------");
                foreach (var fileInfo in dupeGroup)
                {
                    Console.WriteLine(fileInfo.FullName);
                    Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
                    await Task.Delay(2500);
                }
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
    }
}
