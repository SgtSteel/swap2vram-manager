using System;
using System.IO;

namespace swap2vrammanager
{
    class MainClass
    {
        static SwapManager swapManager;
        static bool ExitRequested = false;

        public static void Main(string[] args)
        {
            // check if root
            if (Util.RunProcess("whoami", null, out string user) == 0)
            {
                if (!user.Trim().Equals("root", StringComparison.OrdinalIgnoreCase))
                {
                    Util.Die(Util.ExitCodes.ErrorNotRoot, "This program really needs to be run as root. Please provide ultimate cow powers.");// https://web.archive.org/web/20190322061230/https://plus.google.com/113373031907493914258/posts/jGBx4hA26nv
                }
            }



            // check if our dependencies can be reached
            if (!File.Exists(Util.PathToGetAvailableVram))
            {
                Util.Die(Util.ExitCodes.MissingDependencies, "This program really needs '" + Util.PathToGetAvailableVram + "' file to be present.");
            }

            if (!File.Exists(Util.PathToVramfs))
            {
                Util.Die(Util.ExitCodes.MissingDependencies, "This program really needs '" + Util.PathToVramfs + "' file to be present.");
            }




            int size = 256; //MiB
            int maxareas = 8;
            int timerInterval = 30; // seconds
            int minAvailableVram = 256; // MiB

            for(int i = 0; i < args.Length; i++)
            {
                switch(args[i])
                {
                    case "-s":
                        if (++i >= args.Length || !int.TryParse(args[i], out size))
                        {
                            PrintHelp();
                            Util.Die(Util.ExitCodes.ErrorWrongUsage);
                        }
                        break;
                    case "-a":
                        if (++i >= args.Length || !int.TryParse(args[i], out maxareas))
                        {
                            PrintHelp();
                            Util.Die(Util.ExitCodes.ErrorWrongUsage);
                        }
                        break;
                    case "-t":
                        if (++i >= args.Length || !int.TryParse(args[i], out timerInterval))
                        {
                            PrintHelp();
                            Util.Die(Util.ExitCodes.ErrorWrongUsage);
                        }
                        break;
                    case "-m":
                        if (++i >= args.Length || !int.TryParse(args[i], out minAvailableVram))
                        {
                            PrintHelp();
                            Util.Die(Util.ExitCodes.ErrorWrongUsage);
                        }
                        break;
                    case "--help":
                        PrintHelp();
                        return;
                }
            }

            if (maxareas > 32)
            {
                Console.WriteLine("The linux kernel only allowes up to 32 swap areas! See 'man mkswap' for more info. Quitting.\n");
                Util.Die(Util.ExitCodes.ErrorWrongUsage);
            }


            //handles SIGINT (CTRL+C)
            Console.CancelKeyPress += Console_CancelKeyPress;

            swapManager = new SwapManager(timerInterval, size, maxareas, minAvailableVram);

            // <3
            while(!ExitRequested)
            {
                Console.ReadLine();
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("This program needs to be run as root.\n" +
                "Parameters:\n" +
                "\t-s <size>\tSize in MiB of each swap area. Default = 256\n" +
                "\t-a <max>\tMaximun number of enabled swap areas at any given time. Default = 8. Must be less than 32\n" +
                "\t-t <interval>\tInterval, in seconds, of the available vram check timer. Default = 30\n" +
                "\t-m <size>\tThe least amount of vram to be kept free for other applications. Default = 256");
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            swapManager.DisableAll();
            ExitRequested = true;
            Console.WriteLine("Bye bye!");
        }

    }
}
