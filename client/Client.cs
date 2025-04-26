using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoU;

public class Client
{
    #region Variables
    private bool isConnected = false;
    private string ip = "";
    private int port = 0;

    // TCP
    public int id { get; private set; } = 0;
    private TcpClient client;
    private NetworkStream stream;
    private Dictionary<string, Action<string>> actions = new Dictionary<string, Action<string>>();

    // Game
    //lobbies, players...
    public event Action<GameState> OnGameStateUpdate;
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Main functions
    public Client(string ip, int port)
    {
        this.ip = ip;
        this.port = port;

        InitActions();
        ConnectToServer();
    }

    public void ConnectToServer()
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            isConnected = true;
            Console.WriteLine($"[LOG] Connecting to server {ip}:{port}...");

            Task t = new Task(ReceiveTCP);
            t.Start();
        }
        catch (SocketException e)
        {
            Console.WriteLine("[ERR] TCP Connection: " + e);
        }
    }
    #endregion

    //--------------------------------------------------------------------------------------------

    #region TCP
    private void ReceiveTCP()
    {
        byte[] buffer = new byte[1024];
        while (isConnected)
        {
            // Reading
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount == 0) {
                Console.WriteLine($"[TCP] No reading from server: {ip}:{port}");
                break;
            }
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);

            #region Debug recieved data ========================================================
            // Debug recieved data
            // Console.WriteLine($"[TCP] {data}");
            #endregion

            // Acting
            // Trim data if multiple messages in one
            BaseMessage message = Utils.TrimData(data);
            if (actions.TryGetValue(message.Type, out var action)) action?.Invoke(message.Data.ToString());
        }
    }

    public void Disconnect()
    {
        if (client != null && isConnected)
        {
            isConnected = false;
            try
            {
                client.Client.Shutdown(SocketShutdown.Both);
                stream?.Close();
                client?.Close();
            }
            catch (SocketException e)
            {
                Console.WriteLine($"[ERR] TCP Disconnect: {e}");
            }
        }

        #region TODO: move to game engine
        Console.Clear();
        #endregion
    }

    public void TCP(string type, object message)
    {
        if (id == 0) return;
        string data = Utils.CreateMessage(id, type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        stream.Write(buffer, 0, buffer.Length);
    }
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Actions
    private void InitActions()
    {
        actions = new Dictionary<string, Action<string>>
        {
            { "LOG", Log },
            { "CLID", CLID },
            { "STATE", State },
        };
    }

    private void Log(string message)
    {
        Console.WriteLine($"[LOG] {message}");
    }

    private void CLID(string data)
    {
        var obj = Utils.Deserialize<int>(data);
        id = obj;
        // Console.WriteLine($"My id is {id}");
    }

    private void State(string data)
    {
        var obj = Utils.Deserialize<GameState>(data);
        OnGameStateUpdate?.Invoke(obj);
    }
    #endregion

}