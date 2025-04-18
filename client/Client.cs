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
    private int clientId = 0;
    private TcpClient client;
    private NetworkStream stream;
    private Dictionary<string, Action<string>> actions = new Dictionary<string, Action<string>>();

    // Game
    //lobbies, players...
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Main functions
    public Client(string ip, int port)
    {
        this.ip = ip;
        this.port = port;

        Console.CancelKeyPress += (sender, e) => Disconnect();
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

            #region TODO: move to game engine
            while (isConnected)
            {
                LobbyInput();
            }
            #endregion
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
        byte[] buffer = new byte[4096];
        while (isConnected)
        {
            // Reading
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount == 0) continue;
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);

            // Debug recieved data
            // Debug.Log($"[TCP] {data}");

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
    }

    public void TCP(string type, object message)
    {
        // if (clientId == 0) return;
        string data = Utils.CreateMessage(clientId, type, message);
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
            // { "RPC", RPC },
            // { "VIEW_UPD", ViewUpdate },
            // { "VIEW_DEL", ViewDelete },
        };
    }

    private void Log(string message)
    {
        var obj = Utils.Deserialize<Log>(message);
        Console.WriteLine($"[LOG] {obj.message}");
    }

    private void CLID(string data)
    {
        var obj = Utils.Deserialize<int>(data);
        clientId = obj;
        Console.WriteLine($"My id is {clientId}");
    }
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Input TODO: move to game engine
    private void LobbyInput()
    {
        ConsoleKeyInfo key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.Q:
                TCP("LOG", new Log("🟢 5   [2] 🔴 7   [3] 🔵 Skip"));
                break;
            case ConsoleKey.D:
                TCP("LOL", new Log($"Ayo it's me {clientId}"));
                break;

            // case ConsoleKey.UpArrow:
            //     Console.WriteLine("\t"+Underline("Luzevg"));
            //     Console.WriteLine("Qdness"+"\t\t"+"Equa");
            //     Console.WriteLine("\t"+"Axlamon");
            //     break;q

            // case ConsoleKey.RightArrow:
            //     Console.WriteLine("\tLuzevg");
            //     Console.WriteLine("Qdness\t\t"+Underline("Equa"));
            //     Console.WriteLine("\tAxlamon");
            //     break;

            default:
                break;
        }
    }
    #endregion

}