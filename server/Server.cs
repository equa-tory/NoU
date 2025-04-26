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
    private Dictionary<string, Action<int, string>> actions = new Dictionary<string, Action<int, string>>();

    // Game
    //lobbies, players...
    private GameState gameState = new GameState();
    private GameLogic gameLogic;
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Main functions
    public Server(string ip, int port)
    {
        this.ip = ip;
        this.port = port;

        Console.Clear();
        Console.CancelKeyPress += (sender, e) => Stop();
        Init();
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
            // BroadcastTCP("STATE", gameState); // << recieve everyone except connected player

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

            #region Debug recieved data ========================================================
            // Debug recieved data
            // Console.WriteLine($"[TCP] {data}");
            #endregion

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
            if (actions.TryGetValue(message.Type, out var action)) action?.Invoke(message.ID, message.Data.ToString());
        }

        DisconnectTcpClient(client);
    }

    private void DisconnectTcpClient(TcpClient client)
    {
        if (client == null || !client.Connected) return;

        // Reset game if no players
        if(gameState.players.Count == 0){
            gameState.currentScreen = Screen.Lobby;
            gameState.players.Clear();
            gameLogic = null;
        }

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

        #region In Game actions
        Player player = gameState.players.FirstOrDefault(x => x.id == clientId);
        if(player == null) return;

        gameState.players.Remove(player);

        // Host Migration
        if(player.isHost){
            gameState.players[0].isHost = true;
        }

        #region  TODO: REDO MIGRATION TO NEXT PLAYER, NOT FIRST!!!
        // Current Player Migration
        if(gameState.currentPlayer == clientId){
            gameState.currentPlayer = gameState.players[0].id;
        }
        #endregion
        #endregion


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

        #region TODO: Broadcast disconnect in chat
        // BroadcastTCP("LOG", $"[LOG] Client (ID: {clientId}) disconnected.");
        BroadcastTCP("STATE", gameState);
        #endregion
    }
    #endregion

    #region Sending
    public void BroadcastTCP(string type, object message)
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
    private void Init()
    {
        actions = new Dictionary<string, Action<int, string>>
        {
            { "LOG", Log },
            { "NICK", Nick },
            { "START", StartGame },
            { "CMD-CHAT", CommandChat },
            // { "CMD-CHAT", ChatMessage },
        };

        gameState.currentScreen = Screen.Lobby;
    }

    private void Log(int id, string message)
    {
        Console.WriteLine($"[LOG] {message}");
        BroadcastTCP("LOG", message);
    }

    private void Nick(int id, string message)
    {
        // Console.WriteLine($"[LOG] Player {id} changed name to {message}");
        var p = new Player();
        p.id = id;
        p.name = message;
        p.isHost = gameState.players.Count == 0;
        gameState.players.Add(p);

        BroadcastTCP("STATE", gameState);
        DirectTCP("STATE", gameState, clients[id]); // Somewhy doesn't update on new client w/o this
    }

    private void StartGame(int id, string message)
    {
        var obj = Utils.Deserialize<GameState>(message); // unnecessary, but just in case

        // check if host
        var player = gameState.players.FirstOrDefault(x => x.id == id);
        if (player == null) return;
        if (player.isHost == false) return;

        gameState.currentScreen = Screen.Game;

        // Generate cards, giveaway them to players
        gameLogic = new GameLogic(gameState, this);

        // Commands help
        gameState.chat.Add(new ChatMessage("Commands", "play card_num;\ncolor red_green_blue_yellow;\nchat your_message; quit"));

        BroadcastTCP("STATE", gameState);
    }
    
    private void CommandChat(int id, string message)
    {
        var obj = Utils.Deserialize<ChatMessage>(message);
        gameState.chat.Add(obj);
        // remove old messages if more than 3
        if (gameState.chat.Count > 3) gameState.chat.RemoveAt(0);
        BroadcastTCP("STATE", gameState);
    }
    #endregion

}