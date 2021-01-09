using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace swap2vrammanager
{
    public class Swapfile
    {
        public int ID { get; private set; }

        public string DirPath { get; private set; }
        public string FilePath 
        {
            get
            {
                return DirPath + "swapfile";
            }
        }
        public int SizeMiB { get; private set; }
        public bool Enabled { get; private set; }
        public int Priority { get; private set; }


        private Process vramfsProcess;
        private string LoopDevPath;

        
        /// <summary>
        /// Initializes a new instance of the <see cref="T:swap2vrammanager.Swapfile"/> class.
        /// </summary>
        /// <param name="id">Progressive identifier.</param>
        /// <param name="size">Size of the swapfile in MiB.</param>
        /// <param name="priority">Prioriry of this swapfile</param>
        public Swapfile(int id, int size, int priority)
        {
            this.ID = id;
            this.SizeMiB = size;
            this.Enabled = false;
            this.DirPath = "/tmp/vram/swap/" + ID + "/";
            this.Priority = priority;
        }

        /// <summary>
        /// Runs vramfs and sets the swapfile up
        /// </summary>
        public void Enable()
        {
            if (Enabled)
                return;

            Console.Write("Creating new swapfile (#" + ID + ", Size:" + SizeMiB + "MiB)...");
            try
            {
                Directory.CreateDirectory(DirPath);

                vramfsProcess = Util.RunProcessAsync(Util.PathToVramfs, DirPath + " " + SizeMiB + "M");

                string tmp;
                // waits for vramfs to write "allocating vram...\nmounted.\n"
                tmp = vramfsProcess.StandardOutput.ReadLine();
                tmp = vramfsProcess.StandardOutput.ReadLine();

                Util.RunProcess("dd", "if=/dev/zero of=" + FilePath + " bs=1M count=" + SizeMiB, out tmp, true);
                Util.RunProcess("chmod", "600 " + FilePath, out tmp);

                Util.RunProcess("losetup", "-f " + FilePath + " --show", out LoopDevPath);
                LoopDevPath = LoopDevPath.Trim(); // removes '\n' at the end
                Util.RunProcess("mkswap", LoopDevPath, out tmp);
                Util.RunProcess("swapon", LoopDevPath + " --priority " + Priority, out tmp);

                Enabled = true;
                Console.WriteLine(" OK");
            }
            catch (Exception exc)
            {
                Console.WriteLine(" FAILED: " + exc.Message);
            }
        }


        /// <summary>
        /// Runs swapoff and terminates the associated vramfs process
        /// </summary>
        public void Disable()
        {
            if (!Enabled)
                return;

            Console.Write("Terminating swapfile (#:" + ID + ", Size:" + SizeMiB + "MiB)...");
            try
            {
                string tmp;
                Util.RunProcess("swapoff", LoopDevPath, out tmp);
                Util.RunProcess("losetup", "-d " + LoopDevPath, out tmp);
                File.Delete(FilePath);
                vramfsProcess.Close();

                Enabled = false;
                Console.WriteLine(" OK");
            }
            catch (Exception exc)
            {
                Console.WriteLine(" FAILED: " + exc.Message);
            }
        }

        ~Swapfile()
        {
            if (Enabled)
                Disable();
        }
    }
}
