using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NoUS;

public class Player
{
    private int id { get; set; }
    private string name { get; set; }
    private int cardsLeft { get; set; }

    public Player(string name, int cardsLeft = 7)
    {
        this.name = name;
        this.cardsLeft = cardsLeft;
    }
}
public class Card
{
    public enum CardColor { red, green, blue, yellow, none }
    public enum CardType { number, skip, reverse, drawTwo, wild, wildDrawFour }
    
    public CardColor color { get; set; }
    public CardType type { get; set; }
    public int? num { get; set; }

    public Card(CardColor color, CardType type, int? num = null)
    {
        this.color = color;
        this.type = type;
        this.num = num;
    }
}

public class Server
{
    private bool isRunning;

    // TCP
    private TcpListener tcpServer;
    private List<TcpClient> tcpClients = new List<TcpClient>();

    // Game
    private List<Card> drawDeck = new List<Card>();


    public Server(string ip, int port)
    {
        tcpServer = new TcpListener(IPAddress.Parse(ip), port);

        //      =============   TODO with foreach

        // add number cards
        for(int color=0;color<Card.CardColor.Count;color++)
        {
            for(int i=0;i<10;i++)
            {
                drawDeck.Add(Card.CardColor[color], Card.CardType.number, i);
                drawDeck.Add(Card.CardColor[color], Card.CardType.number, i);
            }
            drawDeck.Add(Card.CardColor[i], Card.CardType.number,0);
        }
        // add action cards
        for(int type=0;type<Card.CardType.Count;type++)
        {
            
        }
        // add wild cards
        for(int i=0;i<4;i++)
        {
            drawDeck.Add(Card.CardColor.none, Card.CardType.wild);
            drawDeck.Add(Card.CardColor.none, Card.CardType.wildDrawFour);
        }

        int a=0;
        for(int i=0;i<drawDeck.Count;i++)
        {
            if(drawDeck[i].type == Card.CardType.number && drawDeck[i].color == Card.CardColor.green) a++;
        }
        Console.WriteLine("Red cards: ",a);

        // for (int i = 0;i<10;i++) drawDeck.Add(new Card(CardColor.red, CardType.number, i));
        // for (int i = 0;i<10;i++) drawDeck.Add(new Card(CardColor.green, CardType.number, i));
        // for (int i = 0;i<10;i++) drawDeck.Add(new Card(CardColor.blue, CardType.number, i));
        // for (int i = 0;i<10;i++) drawDeck.Add(new Card(CardColor.yellow, CardType.number, i));
        // drawDeck.Add(new Card(CardColor.red,CardType.number, 0));
        // drawDeck.Add(new Card(CardColor.green,CardType.number, 0));
        // drawDeck.Add(new Card(CardColor.blue,CardType.number, 0));
        // drawDeck.Add(new Card(CardColor.yellow,CardType.number, 0));
        // drawDeck.Add(new Card(CardColor.red,Card.CardType.skip)); drawDeck.Add(new Card(CardColor.red,Card.CardType.skip));
        // drawDeck.Add(new Card(CardColor.green,Card.CardType.skip)); drawDeck.Add(new Card(CardColor.green,Card.CardType.skip));
        // drawDeck.Add(new Card(CardColor.blue,Card.CardType.skip)); drawDeck.Add(new Card(CardColor.blue,Card.CardType.skip));
        // drawDeck.Add(new Card(CardColor.yellow,Card.CardType.skip)); drawDeck.Add(new Card(CardColor.yellow,Card.CardType.skip));

        // drawDeck.Add(new Card(CardColor.red,Card.CardType.reverse)); drawDeck.Add(new Card(CardColor.red,Card.CardType.reverse));
        // drawDeck.Add(new Card(CardColor.green,Card.CardType.reverse)); drawDeck.Add(new Card(CardColor.green,Card.CardType.reverse));
        // drawDeck.Add(new Card(CardColor.blue,Card.CardType.reverse)); drawDeck.Add(new Card(CardColor.blue,Card.CardType.reverse));
        // drawDeck.Add(new Card(CardColor.yellow,Card.CardType.reverse)); drawDeck.Add(new Card(CardColor.yellow,Card.CardType.reverse));

        // drawDeck.Add(new Card(CardColor.red,Card.CardType.drawTwo)); drawDeck.Add(new Card(CardColor.red,Card.CardType.drawTwo));
        // drawDeck.Add(new Card(CardColor.green,Card.CardType.drawTwo)); drawDeck.Add(new Card(CardColor.green,Card.CardType.drawTwo));
        // drawDeck.Add(new Card(CardColor.blue,Card.CardType.drawTwo)); drawDeck.Add(new Card(CardColor.blue,Card.CardType.drawTwo));
        // drawDeck.Add(new Card(CardColor.yellow,Card.CardType.drawTwo)); drawDeck.Add(new Card(CardColor.yellow,Card.CardType.drawTwo));

        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wildDrawFour));
        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wildDrawFour));
        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wildDrawFour));
        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wildDrawFour));

        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wild));
        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wild));
        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wild));
        // drawDeck.Add(new Card(CardColor.none,Card.CardType.wild));

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

            // ====================== TCP MESSAGE COMMANDS ======================            
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

                case "msg":
                    TcpBroadcast(parts[1]);
                    break;
            }
            Console.WriteLine("[MSG] " + data);
        }

        // Disonnect if client disconnected
        DisconnectTCPClient(client);

        //== Send every updated list if someone disconnected
        // 
        // lock(client) { // lock to prevent race condition (disconnect)
        //     TcpBroadcast(Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()));
        //     tcpClients.Remove(client);
        // }
        // client.Close();
        // Console.WriteLine("[LOG] Client disconnected!");
    }
    
    // Send data to all connected clients
    private void TcpBroadcast(string data)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        lock(tcpClients)
        {
            foreach(TcpClient client in tcpClients)
            {
                try{
                    NetworkStream stream = client.GetStream();
                    stream.Write(buffer, 0, buffer.Length);
                }
                catch{}
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