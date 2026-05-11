using System.Net;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;
using elevator_simulation.ViewModels;

namespace elevator_simulation.Services
{
    public class ElevatorHttpServer
    {
        private readonly HttpListener _listener = new();
        private readonly MainViewModel _vm;
        private readonly Dispatcher _dispatcher;
        private bool _running;
        public int Port { get; } = 5001;

        public ElevatorHttpServer(MainViewModel vm, Dispatcher dispatcher)
        {
            _vm = vm;
            _dispatcher = dispatcher;
        }

        public void Start()
        {
            try
            {
                _listener.Prefixes.Clear();
                _listener.Prefixes.Add($"http://+:{Port}/");
                _listener.Start();
                _running = true;
                Task.Run(ListenLoopAsync);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"HTTP sunucusu baslatılamadı: {ex.Message}\n\nnetsh http add urlacl url=http://+:5001/ user=Everyone", "HTTP Sunucu Hatası", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }

        public void Stop()
        {
            _running = false;
            try { _listener.Stop(); } catch { }
        }

        private async Task ListenLoopAsync()
        {
            while (_running)
            {
                HttpListenerContext? ctx = null;
                try { ctx = await _listener.GetContextAsync(); }
                catch { break; }
                _ = Task.Run(() => HandleRequest(ctx));
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            var res = ctx.Response;
            string path = ctx.Request.Url?.AbsolutePath.ToLower().TrimEnd('/') ?? "";
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.ContentType = "application/json; charset=utf-8";
            string json;
            if (path == "/status")
            {
                int f1 = 0, f2 = 0; string s1 = "", s2 = "";
                _dispatcher.Invoke(() => { f1 = _vm.CurrentFloor; f2 = _vm.CurrentFloor2; s1 = _vm.ElevatorStateDisplay; s2 = _vm.ElevatorStateDisplay2; });
                json = System.Text.Json.JsonSerializer.Serialize(new { floor1 = f1, floor2 = f2, state1 = s1, state2 = s2 });
            }
            else if (path.StartsWith("/call/") && int.TryParse(path[6..], out int floor))
            {
                _dispatcher.Invoke(() => _vm.CallElevatorCommand?.Execute(floor));
                json = System.Text.Json.JsonSerializer.Serialize(new { ok = true, action = "call", floor });
            }
            else if (path == "/stop")
            {
                _dispatcher.Invoke(() => _vm.EmergencyStopCommand?.Execute(null));
                json = System.Text.Json.JsonSerializer.Serialize(new { ok = true, action = "stop" });
            }
            else
            {
                res.StatusCode = 404;
                json = System.Text.Json.JsonSerializer.Serialize(new { ok = false, error = "unknown endpoint" });
            }
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes);
            res.OutputStream.Close();
        }
    }
}
