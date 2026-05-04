namespace StrongTypes.Wpf.TestApp;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        this.UseStrongTypes();
        base.OnStartup(e);
    }
}
