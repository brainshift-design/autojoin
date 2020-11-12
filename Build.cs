using System;
using System.Timers;
using System.Collections.Generic;
using System.IO;


namespace autojoin
{
    class Build
    {
        public string       InputFile;
        public string       InputDir;

        public List<string> InputFiles;

        public string       OutputFile;
                            

        string              m_lastChangedFile;
                              
        public Timer        m_dupTimer;
        FileSystemWatcher   m_watcher;

        public Build(string inFile, string outFile)
        {
            InputFile = Path.GetFullPath(inFile);

            if (!File.Exists(InputFile))
            {
                Console.Write("\nInvalid input list file\n");
                return;
            }


            InputDir = Environment.CurrentDirectory;
            UpdateInputFiles();

            OutputFile = Path.GetFullPath(Path.Combine(InputDir, outFile));
            JoinFiles(false);
        }

        public void UpdateInputFiles()
        {
            InputFiles = new List<string>();
            ReadFile(InputFile, ref InputFiles);
        }


        void ReadFile(string inputFile, ref List<string> inputFiles)
        {
            var sr = new StreamReader(new FileStream(
                inputFile,
                FileMode  .Open,
                FileAccess.Read,
                FileShare .ReadWrite));

            while (!sr.EndOfStream)
            {
                var file = sr.ReadLine().Trim();

                if (file == "")
                    continue;

                if (   file.Length >= 2
                    && file.Substring(0, 2) == "//")
                    continue;


                if (   file[0            ] == '['
                    && file[file.Length-1] == ']')
                {
                    file = file.Substring(1, Math.Max(0, file.Length-2));

                    ReadFile(
                        Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, file)), 
                        ref inputFiles);
                }
                
                else
                    inputFiles.Add(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, file)));
            }
        }


        public void JoinFiles(bool same)
        {
            using (var output = new StreamWriter(OutputFile, false))
            { 
                var hasErrors = false;
    
                for (int i = 0; i < InputFiles.Count; i++)
                {
                    var file = InputFiles[i];

                    if (File.Exists(file))
                    {
                        var sr = new StreamReader(new FileStream(
                            file,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.ReadWrite));

                        output.Write(sr.ReadToEnd());

                        if (i < InputFiles.Count - 1)
                            output.Write("\n\n\n");
                    }
                    else if (!same)
                    {
                        if (!hasErrors) 
                        {
                            Console.Write("\n");
                            hasErrors = true;
                        }

                        Console.Write("  Missing file [" + file + "]\n");
                    }
                }

                if (hasErrors) 
                    Console.Write("\n");
            }
        }

        void OnFileChanged(object src, FileSystemEventArgs e)
        {
            if (e.FullPath == InputFile)
                UpdateInputFiles();

            else if (!InputFiles.Contains(e.FullPath))
                return;

            var same = e.Name == m_lastChangedFile;

            if (!same)
            {
                Console.Write("* " + e.Name + " -> " + Path.GetFileName(OutputFile) + "\n");
                StartLastFileTimer();
            }

            m_lastChangedFile = e.Name;

            JoinFiles(!same);
        }


        public void Watch()
        {
            m_watcher = new FileSystemWatcher();

            m_watcher.Path                  = InputDir;
            m_watcher.Filter                = "*.*";
            m_watcher.NotifyFilter          = NotifyFilters.LastWrite;
            m_watcher.EnableRaisingEvents   = true;
            m_watcher.IncludeSubdirectories = true;

            m_watcher.Changed += OnFileChanged;

            Console.Write("Watching [" + Path.GetFileName(InputFile) + "] for file changes...\n");
        }


        void StartLastFileTimer()
        {
            m_dupTimer = new Timer(500);

            m_dupTimer.AutoReset = false;
            m_dupTimer.Enabled   = true;

            m_dupTimer.Elapsed += OnTimer;
        }

        void OnTimer(Object src, ElapsedEventArgs e)
        {
            m_lastChangedFile = "";
        }
    }
}
