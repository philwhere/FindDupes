using System;
using System.IO;

namespace FindDupes
{
    public static class FileComparer
    {
        private const int BytesToRead = sizeof(long);

        public static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            if (first.Length != second.Length)
                return false;

            var iterations = (int)Math.Ceiling((double)first.Length / BytesToRead);

            using (var fs1 = first.OpenRead())
            using (var fs2 = second.OpenRead())
            {
                var one = new byte[BytesToRead];
                var two = new byte[BytesToRead];

                for (var i = 0; i < iterations; i++)
                    if (!CompareBytes(fs1, one, fs2, two))
                        return false;
            }

            return true;
        }

        private static bool CompareBytes(FileStream fs1, byte[] one, FileStream fs2, byte[] two)
        {
            fs1.Read(one, 0, BytesToRead);
            fs2.Read(two, 0, BytesToRead);

            return BitConverter.ToInt64(one, 0) == BitConverter.ToInt64(two, 0);
        }
    }
}
