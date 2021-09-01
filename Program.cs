using System;
using System.IO;
using System.Collections.Generic;
using System.Timers;
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
                Console.Write("\nExample: autojoin input_list.aj output.html\n");

                return;
            }

            if (args.Length == 1)
            {
                Console.Write("\nSpecify output file\n");
                Console.Write("\nExample: autojoin input_list.aj output.html\n");
                return;
            }
            

            bool watch = 
                   args.Length % 2 == 1
                && args[args.Length-1] == "-watch";


            if (watch)
                Console.Write("\n");


            s_builds = new List<Build>();

            var i = 0;
            while (args.Length >= i + 2)
            { 
                var build = new Build(args[i], args[i+1]);
                if (watch) build.Watch();
                s_builds.Add(build);
                i += 2;
            }


            if (watch)
            { 
                Console.Write("\n");
                Console.ReadKey();
            }
        }
    }
}
