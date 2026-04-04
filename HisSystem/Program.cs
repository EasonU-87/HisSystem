using System;
using System.Windows.Forms;

namespace HisSystem
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 确保这里写的是你想先看到的窗体名字
            // 如果你的登录窗叫 FrmLogin，这里就写 new FrmLogin()
            Application.Run(new Form1());
        }
    }
}