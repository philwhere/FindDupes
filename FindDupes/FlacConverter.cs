using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FindDupes
{
    public static class FlacConverter
    {
        public static void Convert(string directory)
        {
            var files = GetFiles(directory)
                .Where(f => f.Extension == ".flac");
            foreach (var fileInfo in files)
            {
                var strCmdText= $"/C ffmpeg -i \"{fileInfo.FullName}\" -ab 320k \"{fileInfo.FullName.Replace(".flac", ".mp3")}\"";
                System.Diagnostics.Process.Start("cmd.exe",strCmdText);
            }
        }


        private static IList<FileInfo> GetFiles(params string[] directoryPaths)
        {
            var filesPaths = new List<string>();
            directoryPaths.ToList().ForEach(p => AddFiles(p, filesPaths));
            var files = filesPaths.Select(f => new FileInfo(f)).ToList();
            return files;
        }

        private static void AddFiles(string directoryPath, ICollection<string> filesPaths)
        {
            try
            {
                Directory.GetFiles(directoryPath)
                    .ToList()
                    .ForEach(filesPaths.Add);

                Directory.GetDirectories(directoryPath)
                    .ToList()
                    .ForEach(d => AddFiles(d, filesPaths));
            }
            catch (UnauthorizedAccessException)
            { }
        }
    }
}
