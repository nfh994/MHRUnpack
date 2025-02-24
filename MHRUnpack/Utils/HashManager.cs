using MHRUnpack.Hashing;
using System.Collections.Concurrent;
using System.IO;

namespace MHRUnpack.Utils
{
    class HashManager
    {
        public static string ListPath = "./Lists";
        public static HashSet<string> LoadedCache = new HashSet<string>();
        public static ConcurrentDictionary<UInt64, string> HashList = new ConcurrentDictionary<UInt64, string>();

        public static void LoadHashList(string path)
        {
            path = $"{ListPath}/{path}";
            if (LoadedCache.Contains(path))
            {
                return;
            }
            LoadedCache.Add(path);
            string[] lines = File.ReadAllLines(path);
            int ThreadCount = 1;
            if (Properties.Settings.Default.多线程)
            {
                ThreadCount = Properties.Settings.Default.线程数;
            }
            int linesPerThread = lines.Length / ThreadCount;
            Task[] tasks = new Task[ThreadCount];
            for (int i = 0; i < ThreadCount; i++)
            {
                int startLine = i * linesPerThread;
                int endLine = (i == ThreadCount - 1) ? lines.Length : (startLine + linesPerThread);
                tasks[i] = Task.Run(() => ProcessLines(startLine, endLine, lines));
            }
            Task.WaitAll(tasks);
        }

        static void ProcessLines(int startLine, int endLine, string[] lines)
        {
            for (int i = startLine; i < endLine; i++)
            {
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                //line = line.Replace("\\", "/").ToLower();
                //if (HashList.ContainsValue(line))
                //{
                //    continue;
                //}
                line = line.Replace("\\", "/");
                UInt32 dwHashLower = Murmur3.iGetStringHash(line.ToLower());
                UInt32 dwHashUpper = Murmur3.iGetStringHash(line.ToUpper());
                UInt64 dwHash = (UInt64)dwHashUpper << 32 | dwHashLower;
                if (HashList.ContainsKey(dwHash))
                {
                    //HashList.TryGetValue(dwHash, out string old);
                    continue;
                }
                HashList.TryAdd(dwHash, line);
            }
        }
    }
}
