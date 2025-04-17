using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoU;

public class Client
{
    #region Variables
    private bool isRunning = false;
    private string ip = "";
    private int port = 0;

    // TCP
    private TcpListener tcpServer;
    private List<TcpClient> clients = new List<TcpClient>();
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

        Console.CancelKeyPress += (sender, e) => Stop();
        InitActions();
    }

    public void Run()
    {
        tcpServer = new TcpListener(IPAddress.Parse(ip), port);
        tcpServer.Start();
        isRunning = true;

        Console.WriteLine($"[LOG] Server started on {ip}:{port}");

        Thread acceptThread = new Thread(AcceptTCP);
        acceptThread.Start();
    }

    private void Stop()
    {
        isRunning = false;
        tcpServer.Stop();
        Console.Clear();
    }
    #endregion

    //--------------------------------------------------------------------------------------------

    #region TCP
    private void AcceptTCP() // TODO: Secure id send
    {
        while (isRunning)
        {
            TcpClient client = tcpServer.AcceptTcpClient();
            lock (clients) clients.Add(client);
            Console.WriteLine($"[LOG] Client connect: {client.Client.RemoteEndPoint}");

            Task clientThread = new Task(() => HandleClient(client));
            clientThread.Start();

            Thread.Sleep(100);
        }
    }

    private void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while (client.Connected)
        {
            int bytesCount = stream.Read(buffer, 0, buffer.Length);
            if (bytesCount == 0)
            {
                // Client disconnected
                Console.WriteLine($"[LOG] Client disconnect: {client.Client.RemoteEndPoint}");
                break;
            }

            string data = Encoding.UTF8.GetString(buffer, 0, bytesCount);

            BaseMessage message = Utils.TrimData(data);

            // TODO: Secure Check
            // int clientId = message.ID;
            // lock (clients)
            // {
            //     if (clients.FindIndex(c => c.Client.RemoteEndPoint == client.Client.RemoteEndPoint) != clientId)
            // }

            // Actions
            if (actions.TryGetValue(message.Type, out var action)) action?.Invoke(message.Data.ToString());
        }

        DisconnectTcpClient(client);
    }

    private void DisconnectTcpClient(TcpClient client)
    {
        if (client == null || !client.Connected) return;

        clients.Remove(client);

        // Console.WriteLine($"[TCP] Client {client.Client.RemoteEndPoint} (ID: {clientId}) disconnecting...");

        try
        {
            client.Client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[ERR] Error disconnecting client {client.Client.RemoteEndPoint}: {e.Message}");
        }

        // Console.WriteLine($"[TCP] Client {client.Client.RemoteEndPoint} (ID: {clientId}) disconnected.");
    }
    #endregion

    #region Sending
    private void BroadcastTCP(string type, object message)
    {
        string data = Utils.CreateMessage(-16, type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock (clients)
        {
            foreach (TcpClient client in clients)
                client.GetStream().Write(buffer, 0, buffer.Length);
        }
    }

    // Send data to specific client
    private void DirectTCP(string type, object message, TcpClient client)
    {
        string data = Utils.CreateMessage(-16, type, message);
        data += "\n";
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock (clients)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Write(buffer, 0, buffer.Length);
            }
            catch
            {
                Console.WriteLine($"[ERR] Error direct sending data to client {client.Client.RemoteEndPoint}");
            }
        }
    }
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Actions
    private void InitActions()
    {
        actions = new Dictionary<string, Action<string>>
        {
            { "LOG", Log },
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
    #endregion

}