using System;
using System.Diagnostics;

namespace swap2vrammanager
{
    public static class Util
    {
        public const int KiB = 1024;
        public const int MiB = 1024*KiB;

        public const string PathToGetAvailableVram = "get_available_vram";
        public const string PathToVramfs = "vramfs";

        public enum ExitCodes
        {
            OK, // = 0
            ErrorNotRoot, // = 1
            MissingDependencies, // = 2
            ErrorWrongUsage, // = 3
        }



        /// <summary>
        /// Runs a file and waits until it exits
        /// </summary>
        /// <returns>Task's exit code</returns>
        /// <param name="filename">Path to the file</param>
        /// <param name="args">Arguments to pass</param>
        /// <param name="output">Program's standard output</param>
        /// <param name="redirectStdErr">If enabled, the program's stderr won't be included in output</param>
        public static int RunProcess(string filename, string args, out string output, bool redirectStdErr = false)
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo(filename, args)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = redirectStdErr,
                    UseShellExecute = false
                }
            };

            p.Start();
            p.WaitForExit();
            output = p.StandardOutput.ReadToEnd();
#if DEBUG
            //Console.WriteLine("#DEBUG! \"" + filename + ' ' + args + "\" => " + output);
#endif
            return p.ExitCode;
        }


        /// <summary>
        /// Runs the process async. Please use Process.Exited event to handle unexpected termination
        /// </summary>
        /// <returns>Reference to the process.</returns>
        /// <param name="filename">Filename.</param>
        /// <param name="args">Arguments.</param>
        public static Process RunProcessAsync(string filename, string args)
        {
            Process p = new Process()
            {
                StartInfo = new ProcessStartInfo(filename, args)
                { 
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false
                },
                EnableRaisingEvents = true
            };

            p.Start();

            return p;
        }


        /// <summary>
        /// Prints the specified message (if any) and dies with the specified exit code
        /// </summary>
        /// <param name="exitCode">Exit code.</param>
        /// <param name="message">Message to print on the standard error</param>
        public static void Die(ExitCodes exitCode, string message = null)
        {
            if (message != null)
            {
                Console.Error.WriteLine(message);
            }

            Environment.Exit((int)exitCode);
        }


        /// <summary>
        /// Gets the available vram in MiB
        /// </summary>
        /// <returns>The available vram in MiB</returns>
        public static int GetAvailableVram()
        {
            int exitCode = RunProcess(PathToGetAvailableVram, "", out string data);

            if (exitCode == 0)
            {
                int available_vram = 0;
                int.TryParse(data, out available_vram);
                return available_vram;
            }
            else
            {
                return 0;
            }
        }
    }
}
