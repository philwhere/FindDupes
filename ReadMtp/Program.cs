using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Konsole;
using MediaDevices;

namespace ReadMtp
{
    class Program
    {
        static void Main(string[] args)
        {
            const string deviceDescription = "SM-G980F";

            var devices = MediaDevice.GetDevices();
            using var device = devices.First(d => d.Description == deviceDescription);
            device.Connect();
            var androidDirectoryToSearch = device.GetDirectoryInfo(@"\Phone\DCIM\Camera");
            const string pcDirectoryToSearch = "S:\\Photos\\Phone Photos";
            const string pcOutputDirectory = "S:\\Photos\\Phone Photos\\2022";

            ProcessFilesMissingOnPc(SearchOption.TopDirectoryOnly, androidDirectoryToSearch, pcDirectoryToSearch, device, pcOutputDirectory);

            device.Disconnect();
        }

        private static void ProcessFilesMissingOnPc(SearchOption searchOption, MediaDirectoryInfo androidDirectory,
            string pcDirectoryToSearch, MediaDevice device,
            string pcOutputDirectory)
        {
            var androidFiles = androidDirectory.EnumerateFiles("*.*", searchOption).ToList();
            var pcFiles = GetFiles(pcDirectoryToSearch);

            var join = (
                from android in androidFiles
                from pc in pcFiles.Where(f => android.Name == f.Name || (long)android.Length == f.Length).DefaultIfEmpty()
                select (android, pc)).ToList();

            var notOnPc = join
                .Where(pair => pair.pc == null)
                .ToList();

            var filesNotOnPc = notOnPc.Select(p => p.android).ToList();

            if (filesNotOnPc.Any())
            {
                WriteToConsole("Not On PC", notOnPc);
                CopyFilesToPc(device, filesNotOnPc, pcOutputDirectory);
            }

            //var onPc = join
            //    .Where(pair => pair.Item2 != null)
            //    .ToList();
            //WriteToConsole("On PC", onPc);
        }

        private static void CopyFilesToPc(MediaDevice device, IList<MediaFileInfo> filesNotOnPc,
            string pcOutputDirectory)
        {
            var pb = new ProgressBar(PbStyle.SingleLine, filesNotOnPc.Count, 75);

            foreach (var file in filesNotOnPc)
            {
                pb.Refresh(filesNotOnPc.IndexOf(file), $"Writing to {pcOutputDirectory}\\{file.Name} ...");

                using var memoryStream = new MemoryStream();
                device.DownloadFile(file.FullName, memoryStream);
                File.WriteAllBytes($@"{pcOutputDirectory}\{file.Name}", memoryStream.ToArray());
            }

            pb.Refresh(filesNotOnPc.Count, $"Copied {filesNotOnPc.Count} files.");
        }

        private static void WriteToConsole(string titleLine, List<(MediaFileInfo ph, FileInfo pc)> fileList)
        {
            Console.WriteLine(titleLine);
            Console.WriteLine("-------------------");
            Console.WriteLine($"Found {fileList.Count} files.");
            Console.WriteLine();

            fileList.ForEach(pair =>
            {
                Console.WriteLine(pair.ph.FullName);
                if (pair.pc == null)
                    return;
                Console.WriteLine(pair.pc.FullName);
                Console.WriteLine();
            });
            Console.WriteLine(Environment.NewLine);
        }


        private static IReadOnlyList<FileInfo> GetFiles(params string[] directoryPaths)
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
                    .ToList()
                    .ForEach(filePaths.Add);

                Directory.GetDirectories(directoryPath)
                    .ToList()
                    .ForEach(d => AddFiles(d, filePaths));
            }
            catch (UnauthorizedAccessException)
            { }
        }
    }
}
