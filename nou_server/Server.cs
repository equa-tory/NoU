using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NoUS;

// public class PlayerData
// {
//     public int id { get; set; }
//     public float x { get; set; }
//     public float y { get; set; }
//     public float z { get; set; }
// }

public class Server
{
    private bool isRunning;

    // TCP
    private TcpListener tcpServer;
    private List<TcpClient> tcpClients = new List<TcpClient>();


    public Server(string ip, int port){

        tcpServer = new TcpListener(IPAddress.Parse(ip), port);
    }

    public void Start()
    {
        tcpServer.Start();
        isRunning = true;
        Console.WriteLine("[LOG] Server started...");

        Thread acceptThread = new Thread(AcceptTCP);
        acceptThread.Start();
    }
    public void Stop(){
        isRunning = false;
        udpServer.Close();
        tcpServer.Stop();
        Console.WriteLine("[LOG] Server stopped...");
    }

    #region TCP
    public void AcceptTCP()
    {
        while (isRunning)
        {
            TcpClient client = tcpServer.AcceptTcpClient();
            lock (tcpClients) tcpClients.Add(client); // lock to prevent race condition
            Console.WriteLine($"[LOG] TCP connect: {client.Client.RemoteEndPoint}");
            
            Task clientThread = new Task(() => HandleClient(client)); // Create thread for new client   TODO-delete_on_exit
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client){ // Function for every client
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while(client.Connected){
            int bytesCount = stream.Read(buffer, 0, buffer.Length);
            if (bytesCount == 0) break; // Client disconnected

            string data = Encoding.UTF8.GetString(buffer, 0, bytesCount);
            
            //                                                                                 TODO-speed+security
            // tcp message commands
            string[] parts = data.Split("::"); 
            // string json = "";
            switch(parts[0]){
                
                case "connect":
                    // PlayerData newPlayer = new PlayerData(){ id = int.Parse(parts[1]) };
                    // players.Add(newPlayer);
                    // Console.WriteLine($"[TCP] client connected: {newPlayer.id} from {client.Client.RemoteEndPoint}");
                    
                    // // send all players updated list
                    // json = Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()); 
                    // TcpBroadcast(json);
                    break;

                case "disconnect":
                    // foreach(PlayerData p in players){ //                                                            TODO-speed
                    //     if(p.id == int.Parse(parts[1])){
                    //         Console.WriteLine($"[TCP] client disconnected: {p.id} from {client.Client.RemoteEndPoint}");
                    //         players.Remove(p);
                    //         break;
                    //     }
                    // }
                    break;
            }
            Console.WriteLine("[MSG] " + data);
        }
        DisconnectTCPClient(client);
        // lock(client) { // lock to prevent race condition (disconnect)
        //     TcpBroadcast(Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()));
        //     tcpClients.Remove(client);
        // }
        // client.Close();
        // Console.WriteLine("[LOG] Client disconnected!");
    }
    
    private void TcpBroadcast(string data){ //                                                         SEND TCP
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock(tcpClients)
        {
            foreach(TcpClient client in tcpClients)
            {
                try{
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch{

                }
            }
        }
    }
    #endregion

    #region Stuff
    private void DisconnectTCPClient(TcpClient client)
    {
        try{
            if(client.Connected) 
            {
                client.Client.Shutdown(SocketShutdown.Both);
                tcpClients.Remove(client);
                // client.GetStream().Close();
                client.Close();
                Console.WriteLine("[LOG] Client disconnected!");

                // TcpBroadcast(Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()));
            }
        }
        catch(Exception e){
            Console.WriteLine("[ERR] TCP Disconnect: " + e);
        }
    }
    #endregion

}