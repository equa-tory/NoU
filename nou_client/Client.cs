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
    private List<Player> players;
    

    public Client(string ip, int port, Player localPlayer, List<Player> players){
        this.ip = ip;
        this.port = port;
        this.localPlayer = localPlayer;
        this.players = players;
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

            TCP($"connect::{localPlayer.id}::{localPlayer.name}");

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

            // TODO - better way to do this
            // ====================== TCP MESSAGE COMMANDS ======================
            string[] parts = data.Split("::");

            switch(parts[0]){
                // case "msg":
                //     // TODO
                //     Console.WriteLine("[SERVER] msg: " + parts[1]);
                //     break;

                case "playerlist":
                    // if(int.Parse(parts[2]) != localPlayer.id) players.Add(new Player(name: parts[1], id: int.Parse(parts[2])));
                    // ge.frameRate = 100000;

                    // TODO - make via function event to update player list
                    List<Player> tmp = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Player>>(parts[1]);
                    players.Clear();
                    foreach (Player p in tmp) players.Add(p);
                    foreach(var p in tmp) Console.WriteLine($"new: {p.name} id: {p.id}");
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