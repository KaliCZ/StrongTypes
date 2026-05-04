namespace StrongTypes.Wpf.TestApp;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        StrongTypes.Wpf.StrongTypesWpf.Register();
        StrongTypes.Wpf.StrongTypesWpf.Register<Positive<int>>();
        base.OnStartup(e);
    }
}
