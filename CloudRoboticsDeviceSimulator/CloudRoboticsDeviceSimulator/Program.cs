using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CloudRoboticsDeviceSimulator
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 5)
                Application.Run(new DeviceSimulatorForm(args[0], args[1], args[2], args[3], args[4]));
            else
                Application.Run(new DeviceSimulatorForm());
        }
    }
}
