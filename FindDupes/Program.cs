using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaInfo;
using Microsoft.WindowsAPICodePack.Shell;

namespace FindDupes
{
    public class Program
    {

        private static readonly string[] MainDirs =
        [
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
            var exactDupes = GetExactDupes(files);
            await WriteDupesToConsoleWithExplorer(exactDupes);

            //var nameDupes = GetDupesByName(files);
            //await WriteDupesToConsoleWithExplorer(nameDupes);

            //var videoLengthDupes = GetDupesByVideoLength(files); // this one is a doozy to run. expect it to take a while
            //await WriteDupesToFile("video-length.json", videoLengthDupes);

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
                var videoExtensions = new[]
                    { "mp4", "mkv", "wmv", "flv", "avi", "mov", "mpg", "mpeg", "m4v" };
                var patterns = videoExtensions.Select(e => $"*.{e}");
                foreach (var pattern in patterns)
                {
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

        private static async Task WriteDupesToConsoleWithExplorer(IList<IGrouping<string, FileInfo>> dupes)
        {
            if (!dupes.Any())
                return;

            Console.WriteLine("Dupes\n---------------");
            int groupCount = 0;
            foreach (var dupeGroup in dupes)
            {
                groupCount++;
                Console.WriteLine($"{dupeGroup.Key}\n---------------");
                foreach (var fileInfo in dupeGroup)
                {
                    Console.WriteLine(fileInfo.FullName);
                    Process.Start("explorer.exe", $"/select,\"{fileInfo.FullName}\"");
                    await Task.Delay(2500);
                }

                Console.WriteLine();

                if (groupCount % 10 == 0)
                {
                    Console.WriteLine("Processed 10 groups. Press Enter to continue...");
                    Console.ReadLine();
                }
            }
        }

        private static async Task WriteDupesToFile(string outputFilePath, IList<IGrouping<string, FileInfo>> dupes)
        {
            if (!dupes.Any())
                return;

            var outputShape = dupes
                .Where(d => d.Key is not "Less than 5 minutes" and not "No")
                .Select(d => new
                {
                    Length = d.Key,
                    Files = d.Select(f => new
                    {
                        f.Name,
                        f.DirectoryName,
                        f.FullName,
                    })
                })
                .ToList();
            var json = JsonSerializer.Serialize(outputShape, new JsonSerializerOptions
            {
                WriteIndented = true,
            });
            await File.WriteAllTextAsync(outputFilePath, json);
            Process.Start("explorer.exe", $"/select,\"{outputFilePath}\"");
        }


        private static IList<IGrouping<string, FileInfo>> GetExactDupes(IList<FileInfo> fileInfos)
        {
            var dupesBySize = GetDupesByFileSize(fileInfos);
            return FilterDupesToExactMatches(dupesBySize);
        }

        private static IList<IGrouping<string, FileInfo>> GetDupesByName(IList<FileInfo> fileInfos)
        {
            var dupesByName = fileInfos.GroupBy(f => f.Name)
                .Where(g => g.Count() > 1).ToList();
            return dupesByName;
        }

        private static IList<IGrouping<string, FileInfo>> GetDupesByVideoLength(IList<FileInfo> files)
        {
            var videoLengths = new ConcurrentDictionary<FileInfo, string>();
            var processedFiles = 0;

            Parallel.ForEach(files, file =>
            {
                var duration = GetVideoDurationWithMediaInfo(file.FullName);
                videoLengths[file] = duration;
                Interlocked.Increment(ref processedFiles);
                Console.WriteLine($"Processed {processedFiles} of {files.Count}");
            });

            var dupesByVideoLength = videoLengths.GroupBy(kvp => kvp.Value)
                .Where(g => g.Count() > 1)
                .Select(g => g.Select(kvp => kvp.Key).ToList().GroupBy(f => g.Key).First())
                .ToList();

            return dupesByVideoLength;
        }

        private static List<IGrouping<string, FileInfo>> GetDupesByFileSize(IList<FileInfo> fileInfos)
        {
            var dupesBySize = fileInfos
                .GroupBy(i => i.Length.ToString())
                .Where(g => g.Count() > 1)
                .ToList();
            return dupesBySize;
        }

        private static IList<IGrouping<string, FileInfo>> FilterDupesToExactMatches(
            List<IGrouping<string, FileInfo>> dupesBySize)
        {
            var allExactlyEqualFiles = new List<FileInfo>();
            foreach (var sameSizeFileGroup in dupesBySize)
            {
                var exactlyEqualFilesInGroup = sameSizeFileGroup.Where(f1 =>
                    sameSizeFileGroup.Any(f2 => f1 != f2 && FileComparer.FilesAreEqual(f1, f2)));

                allExactlyEqualFiles.AddRange(exactlyEqualFilesInGroup);
            }

            var regroupedDupes = allExactlyEqualFiles.GroupBy(f => f.Length.ToString()).ToList();
            return regroupedDupes;
        }

        static string GetVideoDurationWithMediaInfo(string filePath)
        {
            using var mediaInfo = new MediaInfo.MediaInfo();
            mediaInfo.Open(filePath);
            var durationMs = mediaInfo.Get(StreamKind.Video, 0, "Duration");
            if (double.TryParse(durationMs, out var ms))
            {
                const int fiveMinutesInMs = 300000;
                if (ms < fiveMinutesInMs)
                    return "Less than 5 minutes";
                var duration = TimeSpan.FromMilliseconds(ms);
                return duration.ToString(@"hh\:mm\:ss");
            }
            return GetVideoLengthWithShell(filePath);
        }

        private static string GetVideoLengthWithShell(string filePath)
        {
            try
            {

                using var shell = ShellObject.FromParsingName(filePath);
                var durationProperty = shell.Properties.System.Media.Duration;
                if (durationProperty?.Value == null)
                    return "No";
                var durationInTicks = (long)durationProperty.Value;
                var duration = TimeSpan.FromTicks(durationInTicks);
                var videoLength = duration.ToString(@"hh\:mm\:ss");
                return videoLength;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting video length for {filePath}: {ex.Message}");
                throw ex;
            }
        }
    }
}
