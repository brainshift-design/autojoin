using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Reflection.Emit;
//using System.Threading;

namespace autojoin
{
    /*
        NOTE: this tool must be run from the main project directory
    */

    class Program
    {
        static List<Build> s_builds;



        static void Main(string[] args)
        {
            if (args.Length == 0)
            { 
                Console.Write("\nSpecify input list file\n");
                Console.Write("\n\t-watch : Watch for changes and update the output file in real time.\n");
                Console.Write(  "\t-minify <text file containing a list of keywords to minify>\n");
                Console.Write(  "\t-minifyIgnore <text file containing a list of files to ignore>\n");
                Console.Write(  "\t-minifyMap : Create a lookup map for minified keywords for debugging.\n");
                Console.Write("\nExample for debug build (autojoin.bat): \n");
                Console.Write("\n\tautojoin/figma.aj     ../output/main.ts ^\n");
                Console.Write(  "\tautojoin/generator.aj ../output/generator.html ^\n");
                Console.Write(  "\tautojoin/ui.aj        ../output/ui.html ^\n");
                Console.Write(  "\t-watch\n");
                Console.Write("\nExample for release build (autojoin_minify.bat): \n");
                Console.Write("\n\tautojoin/figma.aj     ../output/main.ts ^\n");
                Console.Write(  "\tautojoin/generator.aj ../output/generator.html ^\n");
                Console.Write(  "\tautojoin/ui.aj        ../output/ui.html ^\n");
                Console.Write(  "\t-minify       autojoin/minify.aj ^\n");
                Console.Write(  "\t-minifyIgnore autojoin/nominify.aj ^\n");
                Console.Write(  "\t-minifyMap\n");
                return;
            }

            if (args.Length == 1)
            {
                Console.Write("\nSpecify output file (path is relative to input)\n");
                Console.Write("\nExample: autojoin input_list.aj output.html\n");
                return;
            }
            

            var watch        = args.Contains("-watch");
            var minifyList   = GetMinifyList(args);
            var minifyIgnore = GetMinifyIgnore(args);
            var minifyMap    = GetMinifyMap(args);


            var replaceList = 
                minifyList != null
                ? CreateReplacementTokens(minifyList.Length)
                : null;


            if (watch)
                Console.Write("\n");


            s_builds = new List<Build>();

            var i = 0;
            while (args.Length >= i + 2
                && args[i][0] != '-')
            {
                var build = new Build(
                    args[i], 
                    args[i+1], 
                    minifyList, 
                    replaceList, 
                    minifyIgnore,
                    minifyMap);

                if (watch) 
                    build.Watch();

                s_builds.Add(build);

                i += 2;
            }


            if (watch)
            { 
                Console.Write("\n");
                Console.ReadKey();
            }
        }



        static string[] GetMinifyList(string[] args)
        {
            string[] minifyList = null;


            var minifyIndex = Array.IndexOf(args, "-minify");

            if (   minifyIndex > -1
                && args.Length > minifyIndex+1)
            {
                var minifyPath = args[minifyIndex+1];

                if (!File.Exists(minifyPath))
                {
                    Console.Write("\nError: minify list not found\n");
                    return null;
                }


                var minifyText = File.ReadAllText(minifyPath);

                minifyList = minifyText.Split('\n');
                minifyList = minifyList.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                minifyList = minifyList.Select(s => s.Trim()).ToArray();
            }


            return minifyList;
        }



        static string[] GetMinifyIgnore(string[] args)
        {
            string[] ignoreLines = null;

            var ignoreFiles = new List<string>();


            var ignoreIndex = Array.IndexOf(args, "-minifyIgnore");

            if (   ignoreIndex > -1
                && args.Length > ignoreIndex+1)
            {
                var ignorePath = args[ignoreIndex+1];

                if (!File.Exists(ignorePath))
                {
                    Console.Write("\nError: minify ignore list not found\n");
                    return null;
                }


                var ignoreText = File.ReadAllText(ignorePath);

                ignoreLines = ignoreText.Split('\n');
                ignoreLines = ignoreLines.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                ignoreLines = ignoreLines.Select(s => s.Trim()).ToArray();


                foreach (var ignoreLine in ignoreLines)
                {
                    string file = ignoreLine;


                    var parentDir = Build.GetParentDir(
                        Path.GetDirectoryName(ignoreLine),
                        ref file);

                    var path = file.Split(
                        new char[] { '/', '\\' },
                        StringSplitOptions.RemoveEmptyEntries);

                    var name = path[path.Length - 1];


                    if (   name.Length > 0
                        && name[0] == '*')
                    {
                        var dir = Path.GetFullPath(Path.Combine(parentDir, file.Substring(0, file.Length-name.Length-1)));

                        var files = Directory.EnumerateFiles(dir, "*", SearchOption.TopDirectoryOnly);

                        foreach (var f in files)
                            ignoreFiles.Add(f);
                    }

                    else
                        ignoreFiles.Add(Path.GetFullPath(Path.Combine(parentDir, file)));
                }
            }


            return ignoreFiles.ToArray();
        }



        static List<string> GetMinifyMap(string[] args)
        {
            var mapIndex = Array.IndexOf(args, "-minifyMap");

            return
                mapIndex > -1
                ? new List<string>()
                : null;
        }



        static string[] CreateReplacementTokens(int count)
        {
            var replace = new List<string>();


            var random = new Random();

            for (var i = 0; i < count; i++)
                replace.Add((char)('a' + random.Next(26)) + $"{i}");

            return replace.ToArray();
        }
    }
}
