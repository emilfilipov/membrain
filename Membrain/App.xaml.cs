using System;
using System.Linq;
using Velopack;

namespace Membrain;

public partial class App : System.Windows.Application
{
    [STAThread]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        if (args.Any(arg => arg.StartsWith("--veloapp-", StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run(new MainWindow());
    }
}
