using System;
using System.Timers;
using System.Collections.Generic;

namespace swap2vrammanager
{
    public class SwapManager
    {
        private Timer AvailableVramCheckTimer;
        private List<Swapfile> SwapAreas;
        public int FileSizeMiB { get; private set; }
        public int MaxAreas { get; private set; }
        public int MinAvailableVram_MiB { get; private set; }
        const int PriorityBase = 100;
        const int PriorityMult = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:swap2vrammanager.SwapManager"/> class.
        /// </summary>
        /// <param name="vramCheckInterval">Available vram check interval in seconds. 0 to disable</param>
        /// <param name="fileSizeMiB">The size of each swap area</param>
        /// <param name="maxAreas">Max # of swap areas</param>
        /// <param name="minAvailableVram">Min quantity of vram te be kept available for other processes</param>
        public SwapManager(int vramCheckInterval, int fileSizeMiB, int maxAreas, int minAvailableVram)
        {
            if (maxAreas > 32)
            {
                throw new Exception("Too many swap areas! Max 32 allowed."); // According to man mkswap, "Presently, Linux allows 32 swap areas" (year 2020, kernel v5.4)
            }

            this.MaxAreas = maxAreas;
            this.FileSizeMiB = fileSizeMiB;
            this.MinAvailableVram_MiB = minAvailableVram;

            SwapAreas = new List<Swapfile>(MaxAreas);

            OptimizeTotalSwapSize();

            if (vramCheckInterval > 0)
            {
                AvailableVramCheckTimer = new Timer(vramCheckInterval * 1000);  // seconds to milliseconds
                AvailableVramCheckTimer.Elapsed += AvailableVramCheckTimer_Elapsed;
                AvailableVramCheckTimer.Start();
            }
        }

        public void OptimizeTotalSwapSize()
        {
            int availableVram = Util.GetAvailableVram();
#if DEBUG
            Console.WriteLine("Available vram: " + availableVram + " MiB");
#endif
            if (availableVram > MinAvailableVram_MiB + FileSizeMiB)
                MoreSwap();
            else if (availableVram < MinAvailableVram_MiB)
                LessSwap();
        }


        public void MoreSwap()
        {
            if (SwapAreas.Count >= MaxAreas)
                return;

            int id = SwapAreas.Count;
            int priority = (MaxAreas - id) * PriorityMult + PriorityBase;
            Swapfile swapfile = new Swapfile(id, FileSizeMiB, priority);
            SwapAreas.Add(swapfile);
            swapfile.Enable();
        }

        public void LessSwap()
        {
            if (SwapAreas.Count == 0)
                return;

            int index = SwapAreas.Count - 1; // last one
            Swapfile swapfile = SwapAreas[index];
            swapfile.Disable();
            SwapAreas.RemoveAt(index);
        }

        void AvailableVramCheckTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OptimizeTotalSwapSize();
        }

        public void DisableAll()
        {
            Console.WriteLine("Terminating all swap areas...");

            foreach (var sa in SwapAreas)
                sa.Disable();

            Console.WriteLine("All swap areas terminated.");
        }

        ~SwapManager()
        {
            if (AvailableVramCheckTimer != null)
                AvailableVramCheckTimer.Stop();

            foreach (var a in SwapAreas)
                if (a.Enabled)
                    a.Disable();
        }

    }
}
