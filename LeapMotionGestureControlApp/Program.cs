using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LeapMotionGestureControlApp
{
    static class Program
    {

        static LeapMotionGestureControl leapMotionGestureControl;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.ApplicationExit += new EventHandler(Program.OnApplicationExit);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show the system tray icon.
            using (ProcessIcon pi = new ProcessIcon())
            {
                pi.Display();

                leapMotionGestureControl = new LeapMotionGestureControl();

                // Make sure the application runs!
                Application.Run();

            }

        }

        static void OnApplicationExit(object sender, EventArgs e)
        {
            try
            {
                leapMotionGestureControl.destroy();
            }
            catch { }
        }

    }
}
