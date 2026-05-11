using System.IO.Pipes;
using System.Text;
using elevator_simulation.ViewModels;

namespace elevator_simulation.Services
{
    /// <summary>
    /// MAUI uygulamasından gelen komutları Named Pipe üzerinden dinler
    /// ve MainViewModel komutlarını tetikler.
    /// Protokol:
    ///   CALL:5        → 5. kattan asansör çağır
    ///   DEST1:3       → Asansör 1'i 3. kata gönder
    ///   DEST2:7       → Asansör 2'yi 7. kata gönder
    ///   STOP          → Acil durdur
    ///   STATUS?       → Mevcut durumu geri gönder
    /// </summary>
    public class ElevatorPipeServer
    {
        public const string PipeName = "ElevatorSimPipe";

        private CancellationTokenSource _cts = new();
        private MainViewModel? _viewModel;

        public void Start(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            Task.Run(ListenLoopAsync);
        }

        public void Stop() => _cts.Cancel();

        private async Task ListenLoopAsync()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    using var pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Message,
                        PipeOptions.Asynchronous);

                    await pipe.WaitForConnectionAsync(_cts.Token);

                    var buffer = new byte[256];
                    int bytesRead = await pipe.ReadAsync(buffer, _cts.Token);
                    string command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

                    string response = HandleCommand(command);

                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await pipe.WriteAsync(responseBytes, _cts.Token);
                }
                catch (OperationCanceledException) { break; }
                catch { /* Bağlantı kesilirse yeni bağlantı bekle */ }
            }
        }

        private string HandleCommand(string command)
        {
            if (_viewModel == null) return "ERROR:NoViewModel";

            try
            {
                if (command.StartsWith("CALL:") && int.TryParse(command[5..], out int callFloor))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        _viewModel.CallElevatorCommand.Execute(callFloor));
                    return $"OK:CALL:{callFloor}";
                }
                else if (command.StartsWith("DEST1:") && int.TryParse(command[6..], out int dest1))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        _viewModel.SelectDestinationCommand.Execute(dest1));
                    return $"OK:DEST1:{dest1}";
                }
                else if (command.StartsWith("DEST2:") && int.TryParse(command[6..], out int dest2))
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        _viewModel.SelectDestinationCommand2.Execute(dest2));
                    return $"OK:DEST2:{dest2}";
                }
                else if (command == "STOP")
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        _viewModel.EmergencyStopCommand.Execute(null));
                    return "OK:STOP";
                }
                else if (command == "STATUS?")
                {
                    var state1 = (_viewModel.ElevatorStateDisplay ?? "").Replace(";", ",");
                    var state2 = (_viewModel.ElevatorStateDisplay2 ?? "").Replace(";", ",");
                    return $"FLOOR1:{_viewModel.CurrentFloor};FLOOR2:{_viewModel.CurrentFloor2};STATE1:{state1};STATE2:{state2}";
                }

                return "ERROR:UnknownCommand";
            }
            catch (Exception ex)
            {
                return $"ERROR:{ex.Message}";
            }
        }
    }
}
