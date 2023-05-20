using System.Windows;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace Clickett
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppCenter.Start("252e49f2-2782-4cff-af4b-50ad147d569f", typeof(Analytics), typeof(Crashes));

            base.OnStartup(e);
        }
    }
}
