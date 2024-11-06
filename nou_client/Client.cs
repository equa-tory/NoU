using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace NoUC;

public class Client
{
    private string ip;
    private int port;
    private bool isConnected = false;
    private Player localPlayer;
    
    private TcpClient client;
    private NetworkStream stream;

    public event Action OnGameStart;
    public event Action<List<Player>> OnLobbyUpdated;
    public event Action<List<Card>> OnStartCardsReceived;
    public event Action<Card> OnTopCardUpdated;
    

    public Client(string ip, int port, Player localPlayer){
        this.ip = ip;
        this.port = port;
        this.localPlayer = localPlayer;
        ConnectToServer();
    }

    private void ConnectToServer()
    {
        try
        {
            client = new TcpClient(ip, port);
            stream = client.GetStream();
            isConnected = true;
            Console.WriteLine("[LOG] Connecting to server!");

            TCP($"connect::{localPlayer.id}::{localPlayer.name}"); // maybe add start cards amount

            Task t = new Task(ReceiveTCP);
            t.Start();
        }
        catch(SocketException e)
        {
            Console.WriteLine("[ERR] TCP Connect: " + e);
        }
    }

    public void TCP(string message)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(message);
        stream.Write(buffer, 0, buffer.Length);
    }

    private void ReceiveTCP()
    {
        byte[] buffer = new byte[1024];
        while (isConnected)
        {
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount == 0) continue;
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
            //--------------------------------------------------------------------------------------------

            // Console.WriteLine("[SERVER] " + data);

            // TODO - better way to do this + move to function
            // ====================== TCP MESSAGE COMMANDS ======================
            string[] parts = data.Split("::");
            switch(parts[0]){
                case "updatelobby":
                    // TODO - make via function event to update player list
                    List<Player> tmp_players = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Player>>(parts[1]);
                    OnLobbyUpdated?.Invoke(tmp_players);
                    break;

                case "start":
                    OnGameStart?.Invoke();
                    break;

                case "startdeck":
                    // TODO - via event
                    List<Card> tmp_startDeck = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Card>>(parts[1]);
                    OnStartCardsReceived?.Invoke(tmp_startDeck);
                    break;

                case "updatetopcard":
                    Card tmp_topcard = Newtonsoft.Json.JsonConvert.DeserializeObject<Card>(parts[1]);
                    OnTopCardUpdated?.Invoke(tmp_topcard);
                    break;
            }
        }
    }

    public void Disconnect(){
        if (client != null)
        {
            TCP($"disconnect::{localPlayer.id}");
            isConnected = false;
            TCPDisconnect();
        }
    }
    private void TCPDisconnect(){
        try{
            client.Client.Shutdown(SocketShutdown.Both);
            stream?.Close();
            client?.Close();
        }catch(Exception e){
            Console.WriteLine($"[ERR] TCP Disconnect: {e}");
        }
    }

}