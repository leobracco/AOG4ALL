using System;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.IO;
using System.Media;
using static WebSocketClient;

public class WebSocketClient
{
    private Control _uiControl; // Referencia a un control de la interfaz de usuario que maneja Invoke
    private ClientWebSocket _webSocket = new ClientWebSocket();
    private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    public WebSocketClient(Control uiControl) // Constructor que recibe un control UI
    {
        _uiControl = uiControl;
    }

    public async Task ConnectAsync(string uri)
    {
        await _webSocket.ConnectAsync(new Uri(uri), _cancellationTokenSource.Token);
        StartListening();
    }

    private async void StartListening()
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    LogReceivedMessage(message); // Registra el mensaje recibido
                    ProcessMessage(message);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await HandleReconnection(); // Manejar reconexión
                }
            }
        }
        catch (WebSocketException ex)
        {
            Console.WriteLine($"WebSocket exception: {ex.Message}");
            if (_webSocket.State != WebSocketState.Open)
            {
                await HandleReconnection(); // Intenta reconectar si la conexión se pierde
            }
        }
    }
    private async Task HandleReconnection()
    {
        Console.WriteLine($"{DateTime.Now}: Attempting to reconnect...");
        await Task.Delay(5000); // Espera 5 segundos antes de reconectar
        _webSocket.Dispose();
        _webSocket = new ClientWebSocket();
        await ConnectAsync("ws://127.0.0.1:8080"); // Reemplaza con la URL de tu servidor
    }
    private void LogReceivedMessage(string message)
    {
        string logMessage = $"{DateTime.Now}: Received message - {message}";
        File.AppendAllText("WebSocketMessages.txt", logMessage + Environment.NewLine);
    }
    public class SensorStatus
    {
        public int SectionId { get; set; }
        public string Status { get; set; }
        public string EventType { get; set; }
    }

    private void ProcessMessage(string message)
    {
        // Loguea el mensaje completo recibido para asegurarte de que contiene el estado esperado.
        Console.WriteLine($"{DateTime.Now}: Message received - {message}");

        try
        {
            var sensorStatus = JsonConvert.DeserializeObject<SensorStatus>(message);
            if (sensorStatus != null)
            {
                Console.WriteLine($"{DateTime.Now}: Processing with SectionId={sensorStatus.SectionId}, Status={sensorStatus.Status}, EventType={sensorStatus.EventType}");
                UpdateButtonColor(sensorStatus);
            }
            else
            {
                Console.WriteLine($"{DateTime.Now}: Deserialization failed, sensorStatus is null.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"{DateTime.Now}: Error in ProcessMessage: {ex.Message}");
        }
    }

    public void Close()
    {
        _cancellationTokenSource.Cancel();
        _webSocket.Dispose();
    }

    private void UpdateButtonColor(SensorStatus sensorStatus)
    {
        // Método para llamar desde cualquier hilo
        if (_uiControl.InvokeRequired)
        {
            _uiControl.Invoke(new Action(() => UpdateButtonColor(sensorStatus)));
        }
        else
        {
            try
            {
                // Buscar el botón por nombre
                Button btn = _uiControl.Controls.Find($"AGPSeed{sensorStatus.SectionId}", true).FirstOrDefault() as Button;
                if (btn != null)
                {
                    // Cambia el color del botón basado en el status
                    
                    switch (sensorStatus.Status)
                    {
                        case "BuenaSingulacion":
                            btn.BackColor = Color.Green;

                            break;
                        case "TuboObstruido":
                            btn.BackColor = Color.Black;
                            break;
                        case "FalloSensor":
                            btn.BackColor = Color.Red;
                            //PlayAlarmSound();
                            break;
                        case "DesajusteDensidad":
                            btn.BackColor = Color.Blue;
                            break;
                        default:
                            btn.BackColor = Color.Gray;
                            break;
                    }
                }
                else
                {
                    // Loguear si el botón no se encuentra
                    LogError($"{DateTime.Now}: Button {$"AGPSeed{sensorStatus.SectionId}"} not found.");
                }
            }
            catch (Exception ex)
            {
                LogError($"Error updating button color: {ex.Message}");
            }
        }
    }

    private void PlayAlarmSound()
    {
        try
        {
            // Asegúrate de que el camino al archivo .wav sea accesible
            using (SoundPlayer player = new SoundPlayer(@"alarm_sound.wav"))
            {
                player.Play();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error al reproducir el sonido: " + ex.Message);
        }
    }

    private void LogStatusChange(string buttonName, string color, string status)
    {
        string message = $"{DateTime.Now}: {buttonName} changed to {color} with status {status}";
        File.AppendAllText("LogStatusChanges.txt", message + Environment.NewLine);
    }

    private void LogError(string error)
    {
        string message = $"{DateTime.Now}: {error}";
        File.AppendAllText("LogErrors.txt", message + Environment.NewLine);
    }
}
