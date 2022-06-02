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

            var devices = MediaDevice.GetDevices();
            //const string deviceDescription = "SM-G980F";
            //using var device = devices.First(d => d.Description == deviceDescription);
            using var device = devices.First();
            Console.WriteLine("Connecting to device...");
            device.Connect();
            Console.WriteLine("Getting directory info for Camera folder...");
            var androidDirectoryToSearch = device.GetDirectoryInfo(@"\Internal Storage\DCIM\Camera");
            const string pcDirectoryToSearch = "S:\\Photos\\Phone Photos";
            const string pcOutputDirectoryRoot = "S:\\Photos\\Phone Photos";

            ProcessFilesMissingOnPc(SearchOption.TopDirectoryOnly, androidDirectoryToSearch, pcDirectoryToSearch, device, pcOutputDirectoryRoot);

            device.Disconnect();
        }

        private static void ProcessFilesMissingOnPc(SearchOption searchOption, MediaDirectoryInfo androidDirectory,
            string pcDirectoryToSearch, MediaDevice device,
            string pcOutputDirectoryRoot)
        {
            Console.WriteLine("Enumerating android files...");
            var androidFiles = androidDirectory.EnumerateFiles("*.*", searchOption).ToList();
            Console.WriteLine("Enumerating PC files...");
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
                CopyFilesToPc(device, filesNotOnPc, pcOutputDirectoryRoot);
            }

            //var onPc = join
            //    .Where(pair => pair.Item2 != null)
            //    .ToList();
            //WriteToConsole("On PC", onPc);
        }

        private static void CopyFilesToPc(MediaDevice device, IList<MediaFileInfo> filesNotOnPc,
            string pcOutputDirectoryRoot)
        {
            var pb = new ProgressBar(PbStyle.SingleLine, filesNotOnPc.Count, 66);

            foreach (var file in filesNotOnPc)
            {
                using var memoryStream = new MemoryStream();
                device.DownloadFile(file.FullName, memoryStream);

                var outputFilePath = GetOutputFilePath(pcOutputDirectoryRoot, file.Name);
                pb.Refresh(filesNotOnPc.IndexOf(file), $"Writing to {outputFilePath} ...");
                File.WriteAllBytes(outputFilePath, memoryStream.ToArray());
            }

            pb.Refresh(filesNotOnPc.Count, $"Copied {filesNotOnPc.Count} files.");
        }

        private static string GetOutputFilePath(string pcOutputDirectoryRoot, string fileName)
        {
            var year = fileName.Substring(0, 4);
            var outputFilePath = $"{pcOutputDirectoryRoot}\\{year}\\{fileName}";
            return outputFilePath;
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
