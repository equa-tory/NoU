using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoU;

public class Server
{
    #region Variables
    private bool isRunning = false;
    private string ip = "";
    private int port = 0;
    private int nextClientId = 1;

    // TCP
    private TcpListener tcpServer;
    private Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
    private Dictionary<string, Action<string>> actions = new Dictionary<string, Action<string>>();

    // Game
    //lobbies, players...
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Main functions
    public Server(string ip, int port)
    {
        this.ip = ip;
        this.port = port;

        Console.Clear();
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
    private void AcceptTCP()
    {
        while (isRunning)
        {
            TcpClient client = tcpServer.AcceptTcpClient();

            nextClientId++;
            lock (clients) clients[nextClientId] = client;
            DirectTCP("CLID", nextClientId, client);

            Console.WriteLine($"[LOG] Client connect: {client.Client.RemoteEndPoint}, with id: {nextClientId}");

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
            // Reading
            int bytesCount = stream.Read(buffer, 0, buffer.Length);
            if (bytesCount == 0)
            {
                // Client disconnected
                Console.WriteLine($"[LOG] No reading from client: {client.Client.RemoteEndPoint}, disconnecting...");
                break;
            }

            string data = Encoding.UTF8.GetString(buffer, 0, bytesCount);

            // Debug recieved data
            // Console.WriteLine($"[TCP] {data}");

            // Acting
            BaseMessage message = Utils.TrimData(data);

            #region Secure Check
            // OLD \/
            // int clientId = message.ID;
            // Console.WriteLine($"Sec check id: {clientId} to {clients.FirstOrDefault(x => x.Value.Equals(client)).Key}");
            // if (clients.TryGetValue(clientId, out var existingClient))
            // {
            //     if (!existingClient.Equals(client))
            //     {
            //         Console.WriteLine($"[ALERT, TCP] !!! ATTEMPT TO SEND UDP WITH SPOOFED ID {clientId} from {client.Client.RemoteEndPoint} (Expected: {existingClient.Client.RemoteEndPoint}) !!!");
            //         continue; // Secure Check: Ignore spoofing attempts
            //     }
            // }

            int messageId = message.ID;
            int clientId = clients.FirstOrDefault(x => x.Value.Equals(client)).Key;
            if (messageId != clientId)
            {
                Console.WriteLine($"[ALERT, TCP] !!! ATTEMPT TO SEND UDP WITH SPOOFED ID {messageId} from {client.Client.RemoteEndPoint} (Expected: {clientId}) !!!");
                continue;
            }
            #endregion

            // Actions
            if (actions.TryGetValue(message.Type, out var action)) action?.Invoke(message.Data.ToString());
        }

        DisconnectTcpClient(client);
    }

    private void DisconnectTcpClient(TcpClient client)
    {
        if (client == null || !client.Connected) return;

        int clientId;
        lock (clients)
        {
            clientId = clients.FirstOrDefault(x => x.Value.Equals(client)).Key;
            if (clientId <= 1)
            {
                Console.WriteLine($"[TCP] Attempted to disconnect unknown client.");
                return;
            }
            clients.Remove(clientId);
        }

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

        #region TODO: Broadcast disconnect
        // BroadcastTCP("LOG", new Log($"[LOG] Client (ID: {clientId}) disconnected."));
        #endregion
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
            foreach (TcpClient client in clients.Values)
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
        BroadcastTCP("LOG", new Log($"[LOG] {obj.message}"));
    }
    #endregion

}