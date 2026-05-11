using System.Windows;
using elevator_simulation.Services;
using elevator_simulation.ViewModels;
using elevator_simulation.Views;

namespace elevator_simulation
{
    public partial class App : Application
    {
        private readonly ElevatorPipeServer _pipeServer = new();
        private ElevatorHttpServer? _httpServer;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow mainWindow = new MainWindow();
            mainWindow.Loaded += (s, args) =>
            {
                if (mainWindow.DataContext is MainViewModel vm)
                {
                    // Named Pipe sunucusu
                    _pipeServer.Start(vm);

                    // HTTP sunucusu (Wi-Fi / QR erişimi için)
                    _httpServer = new ElevatorHttpServer(vm, Dispatcher);
                    _httpServer.Start();
                }
            };
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _pipeServer.Stop();
            _httpServer?.Stop();
            base.OnExit(e);
        }
    }
}
