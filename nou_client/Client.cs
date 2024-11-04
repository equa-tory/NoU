using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoUC;

public class Client
{
    private string ip;
    private int port;
    private bool isConnected = false;
    private Player localPlayer;
    
    private TcpClient client;
    private NetworkStream stream;
    

    public Client(string ip, int port, string nickname){
        this.ip = ip;
        this.port = port;
        this.localPlayer = new Player(nickname);
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

            Console.WriteLine("[SERVER] TCP: " + data);

            // better way                                                                                         TODO
            // ====================== TCP MESSAGE COMMANDS ======================
            string[] parts = data.Split("::"); 

            switch(parts[0]){
                case "msg":
                    // TODO
                    Console.WriteLine("[SERVER] msg: " + parts[1]);
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