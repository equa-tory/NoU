using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoUC;

public class Client
{

    #region Variables

    private string ip;
    private int port;
    private bool isConnected = false;
    private Player localPlayer;
    
    private TcpClient client;
    private NetworkStream stream;

    public event Action<List<Card>, int> OnGameStart;
    public event Action<List<Player>> OnLobbyUpdated;
    public event Action<Card, Player, List<Card>> OnTopCardUpdated;
    
    #endregion

    // ======================================================

    #region Skip1

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

    #endregion

    private void ReceiveTCP()
    {
        #region Skip2
        byte[] buffer = new byte[1024];
        while (isConnected)
        {
            int byteCount = stream.Read(buffer, 0, buffer.Length);
            if (byteCount == 0) continue;
            string data = Encoding.UTF8.GetString(buffer, 0, byteCount);
            //--------------------------------------------------------------------------------------------

            // Console.WriteLine("[SERVER] " + data);

        #endregion
            // TODO - better way to do this + move to function
            // ====================== TCP MESSAGE COMMANDS ======================
            string[] parts = data.Split("::");
            switch(parts[0]){
                case "updatelobby":
                    List<Player> players = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Player>>(parts[1]);
                    OnLobbyUpdated?.Invoke(players);
                    break;

                case "start":
                    List<Card> startDeck = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Card>>(parts[1]);
                    int startPlayerId = int.Parse(parts[2]);
                    OnGameStart?.Invoke(startDeck, startPlayerId);
                    break;

                case "updatetopcard":
                    Card topcard = Newtonsoft.Json.JsonConvert.DeserializeObject<Card>(parts[1]);
                    Player nextPlayer = Newtonsoft.Json.JsonConvert.DeserializeObject<Player>(parts[2]);
                    List<Card> sendedCards = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Card>>(parts[3]);
                    OnTopCardUpdated?.Invoke(topcard, nextPlayer, sendedCards);
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