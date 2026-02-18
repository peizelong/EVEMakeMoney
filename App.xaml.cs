using System.Windows;
using EVEMakeMoney.Data;

namespace EVEMakeMoney;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DbInitializer.Initialize();
    }
}

