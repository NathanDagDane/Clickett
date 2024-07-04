using System.Windows;
using Velopack;

namespace Clickett
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            if (Clickett.Properties.Settings.Default.FirstRun)
            {
                Clickett.Properties.Settings.Default.Upgrade();
                Clickett.Properties.Settings.Default.FirstRun = false;
                Clickett.Properties.Settings.Default.Save();
            }
            base.OnStartup(e);
        }
    }
}
