using System;
using System.Timers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;



namespace autojoin
{
    class Build
    {
        public string              InputFile;
        public string              InputDir;
                                   
                                   
        public List<string>        InputFiles;
                                   
        public string              OutputFile;
                                   
                                   
        string                     m_lastChangedFile;
                              
        public System.Timers.Timer m_dupTimer;
        FileSystemWatcher          m_watcher;



        public Build(string inFile, string outFile, string[] minifyList = null, string[] replaceList = null, string[] minifyIgnore = null, List<string> minifyMap = null)
        {
            InputFile  = Path.GetFullPath(inFile);

            if (!File.Exists(InputFile))
            {
                Console.Write("\nInvalid input list file\n");
                return;
            }


            InputDir = Path.GetDirectoryName(InputFile);
            UpdateInputFiles();


            var outPath = outFile.Split(
                new char[] {'/', '\\' }, 
                StringSplitOptions.RemoveEmptyEntries);

            if (outPath[0] == "..")
            { 
                InputDir = Directory.GetParent(InputDir).FullName;
                outFile = outFile.Substring(3);
            }

            OutputFile = Path.GetFullPath(Path.Combine(InputDir, outFile));

            JoinFiles(false, minifyList, replaceList, minifyIgnore, minifyMap);


            if (minifyMap != null)
            {
                var mapPath = Path.GetFullPath(Path.Combine(InputDir, "minify.map"));


                // sort map

                minifyMap.Sort((token1, token2) =>
                {
                    var tok1 = token1.Split(',')[0];
                    var tok2 = token2.Split(',')[0];

                    int num1;
                    int num2;

                    int.TryParse(tok1.Substring(1), out num1);
                    int.TryParse(tok2.Substring(1), out num2);

                    return num1 - num2;
                });

                
                File.WriteAllLines(mapPath, minifyMap);
            }
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
                    && file.Substring(0, 2) == "//") // comment
                    continue;


                if (   file[0            ] == '['
                    && file[file.Length-1] == ']')
                {
                    var _file = file.Substring(1, Math.Max(0, file.Length-2));

                    var parentDir = GetParentDir(
                        Path.GetDirectoryName(inputFile), 
                        ref _file);

                    try 
                    {
                        ReadFile(
                            Path.GetFullPath(Path.Combine(parentDir, _file)), 
                            ref inputFiles);
                    }
                    catch (Exception)
                    {
                        Console.Write("\nError reading file [" + file + "]\n\n");
                        return;
                    }
                }
                
                else
                {
                    var parentDir = GetParentDir(
                        Path.GetDirectoryName(inputFile), 
                        ref file);

                    var path = file.Split(
                        new char[] {'/', '\\' }, 
                        StringSplitOptions.RemoveEmptyEntries);

                    var name  = path[path.Length-1];
                    var parts = name.Split('.');

                    if (   parts.Length == 2
                        && parts[0].Length > 0
                        && parts[0][0] == '*')
                    {
                        var first = parts[0].Trim();
                        var ex    = new List<string>();

                        if (   first.Length > 1
                            && first[1] == '!')
                        {
                            if (   first.Length < 3
                                || first[2] != '(')
                            {
                                Console.Write("\nError: ! must be followed by (ex1, ex2, ...)\n\n");
                                return;
                            }

                            if (first[first.Length-1] != ')')
                            {
                                Console.Write("\nError: exception list must end with )\n\n");
                                return;
                            }

                            var exceptions = first.Substring(3, first.Length-4);
                            ex = exceptions.Split(',').Select(s => s.Trim()).ToList();
                        }

                        var dir = Path.GetFullPath(Path.Combine(parentDir, file.Substring(0, file.Length-name.Length-1)));

                        var files = Directory.EnumerateFiles(dir, "*." + parts[1], SearchOption.TopDirectoryOnly);

                        foreach (var f in files)
                        {
                            var fn = Path.GetFileNameWithoutExtension(f);
                            if (!ex.Contains(fn)) inputFiles.Add(f);
                        }
                    }

                    else
                        inputFiles.Add(Path.GetFullPath(Path.Combine(parentDir, file)));
                }
            }
        }



        public static string GetParentDir(string inputDir, ref string file)
        {
            var path = file.Split(
                new char[] {'/', '\\' }, 
                StringSplitOptions.RemoveEmptyEntries);

            var parentDir = Environment.CurrentDirectory;

            if (path[0] == "..")
            { 
                parentDir = Directory.GetParent(inputDir).FullName;
                file      = file.Substring(3);
            }

            return parentDir;
        }



        //public static string GetFullParentDir(string inputDir, ref string file)
        //{
        //    var path = file.Split(
        //        new char[] {'/', '\\' }, 
        //        StringSplitOptions.RemoveEmptyEntries);

        //    var parentDir = Environment.CurrentDirectory;

        //    if (path[0] == "..")
        //    { 
        //        parentDir = Directory.GetParent(inputDir).FullName;
        //        file      = file.Substring(3);
        //    }

        //    return parentDir;
        //}



        public void JoinFiles(bool same, string[] minifyList = null, string[] replaceList = null, string[] minifyIgnore = null, List<string> minifyMap = null)
        {
            int maxRetries = 5;
            int retryDelay = 500; // milliseconds

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
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
                                    FileMode  .Open,
                                    FileAccess.Read,
                                    FileShare .ReadWrite));

                                var code = sr.ReadToEnd();

                                if (   minifyList  != null
                                    && replaceList != null
                                    && (    minifyIgnore == null
                                        || !minifyIgnore.Contains(file)))
                                    code = Minify(code, minifyList, replaceList, minifyMap);

                                output.Write(code);

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

                                Console.Write("Error: missing file [" + file + "]\n");
                            }
                        }

                        if (hasErrors) 
                            Console.Write("\n");
                    }
                }
                catch (IOException)
                {
                    if (attempt == maxRetries - 1)
                        throw; // re-throw the exception if it's the last attempt

                    Thread.Sleep(retryDelay); // wait before retrying
                }
            }
        }



        public string Minify(string code, string[] minifyList, string[] replaceList, List<string> minifyMap)
        {
            var minified = code;


            // remove single-line comments

            var lines = minified.Split('\n');

            minified = "";

            foreach (var line in lines)
            {
                minified +=
                    Regex.IsMatch(line, @"^.*https?:.*$")
                    ? line + "\n"
                    : Regex.Replace(
                        line, 
                        @"(?<!http:.*?)//.*?(?=\r?$)", 
                        "");
            }


            // remove multiline comments

            minified = Regex.Replace(minified, @"/\*(.*?)\*/", "", RegexOptions.Singleline);


            // replace tokens

            for (var i = 0; i < minifyList.Length; i++)
            {
                var token   = minifyList[i];
                var replace = replaceList[i];

                minified = Regex.Replace(
                    minified, 
                    $@"(?<!\w|')(?<!\w-){Regex.Escape(token)}(?!\w|')", 
                    match =>
                    {
                        var mapping = $"{replace},{match}";

                        if (    minifyMap != null
                            && !minifyMap.Contains(mapping))
                            minifyMap.Add(mapping);

                        return replace;
                    });
            }


            // remove white space

            minified = Regex.Replace(minified, @"\r(?!\n)", " ");


            return minified;
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
                Console.Write(DateTime.Now.ToString("HH:mm:ss") + " | " + e.Name + " -> " + Path.GetFileName(OutputFile) + "\n");
                StartLastFileTimer();
            }

            m_lastChangedFile = e.Name;

            JoinFiles(!same);
        }



        public void Watch()
        {
            m_watcher = new FileSystemWatcher()
            { 
                Path                  = InputDir,
                Filter                = "*.*",
                NotifyFilter          = NotifyFilters.LastWrite,
                EnableRaisingEvents   = true,
                IncludeSubdirectories = true
            };
    
            m_watcher.Changed += OnFileChanged;

            Console.Write("Watching [" + Path.GetFileName(InputFile) + "] for file changes...\n");
        }



        void StartLastFileTimer()
        {
            m_dupTimer = new System.Timers.Timer(500)
            { 
                AutoReset = false,
                Enabled   = true
            };

            m_dupTimer.Elapsed += OnTimer;
        }



        void OnTimer(Object src, ElapsedEventArgs e)
        {
            m_lastChangedFile = "";
        }
    }
}
