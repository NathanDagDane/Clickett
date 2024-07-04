using System;
using Velopack;

namespace Clickett
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {

                // It's important to Run() the VelopackApp as early as possible in app startup.
                VelopackApp.Build()
                    .WithFirstRun((v) => { /* Your first run code here */ })
                    .Run();

                // We can now launch the WPF application as normal.
                var app = new App();
                app.InitializeComponent();
                app.Run();

            }
            catch (Exception ex)
            {
                //MessageBox.Show("Unhandled exception: " + ex.ToString());
            }
        }
    }
}
