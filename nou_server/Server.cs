using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NoUS;

public class Player
{
    public int id { get; set; }
    public string name { get; set; }
    public int cardsLeft { get; set; }
    public bool isTurn { get; set; } = false;

    public Player(int id,string name, int cardsLeft = 7)
    {
        this.id = id;
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

    #region Variables

    private bool isRunning;

    // TCP
    private TcpListener tcpServer;
    private List<TcpClient> tcpClients = new List<TcpClient>();

    // Game
    private List<Player> players = new List<Player>();
    private List<Card> drawDeck = new List<Card>();
    private bool turnForward = true;
    private Card topCard;

    #endregion

    //--------------------------------------------------------------------------------------------

    public Server(string ip, int port) {
        tcpServer = new TcpListener(IPAddress.Parse(ip), port);
        Console.CancelKeyPress += (sender, args) => Stop();
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
        // Console.WriteLine("[LOG] Server stopped...");
        Console.Clear();
    }

    #region TCP
    public void AcceptTCP()
    {
        while (isRunning)
        {
            TcpClient client = tcpServer.AcceptTcpClient();
            lock (tcpClients) tcpClients.Add(client); // lock to prevent race condition
            // Console.WriteLine($"[LOG] TCP connect: {client.Client.RemoteEndPoint}");
            
            Task clientThread = new Task(() => HandleClient(client)); // Create thread for new client   TODO-delete_on_exit
            clientThread.Start();
        }
    }

    private void HandleClient(TcpClient client){ // Function for every client
        #region Skip
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        while(client.Connected){
            int bytesCount = stream.Read(buffer, 0, buffer.Length);
            if (bytesCount == 0) break; // Client disconnected

            string data = Encoding.UTF8.GetString(buffer, 0, bytesCount);

        #endregion
            // TODO - make via function event to update player list !!!!!!!!!!!!!
            // ====================== TCP MESSAGE COMMANDS ======================
            // tcp message commands
            string[] parts = data.Split("::");
            string json = "";
            switch(parts[0]){

                case "connect": // Player connect
                    Player newPlayer = new Player(int.Parse(parts[1]), parts[2]);
                    players.Add(newPlayer);
                    Console.WriteLine($"[TCP] client \"{newPlayer.name}\" connected from {client.Client.RemoteEndPoint}");
                    
                    // send all players updated list
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()); 
                    TcpBroadcast($"updatelobby::{json}");
                    break;

                case "disconnect": // Player disconnect
                    foreach(Player p in players){
                        if(p.id == int.Parse(parts[1])){
                            Console.WriteLine($"[TCP] client \"{p.name}\" disconnected from {client.Client.RemoteEndPoint}");
                            players.Remove(p);
                            break;
                        }
                    }
                    // Send everyone updated list
                    json = Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()); 
                    TcpBroadcast($"updatelobby::{json}");
                    break;

                case "start": // Game start
                    GenerateDrawDeck();
                    
                    Random r = new Random();

                    // Select random player that turn
                    int playerThatTurnIndex = r.Next(0, players.Count);
                    Player playerThatTurn = players[playerThatTurnIndex];
                    playerThatTurn.isTurn = true;
                    
                    // sending decks
                    lock(tcpClients)
                    {
                        foreach(TcpClient c in tcpClients)
                        {
                            // get 7 random cards for each player
                            List<Card> tmp = new List<Card>();

                            for(int i=0;i<7;i++)
                            {
                                int rand = r.Next(0, drawDeck.Count);
                                tmp.Add(drawDeck[rand]);
                                drawDeck.RemoveAt(rand);
                            }
                            json = Newtonsoft.Json.JsonConvert.SerializeObject(tmp.ToArray());
                            
                            // Select player that turn
                            json += $"::{playerThatTurn.id}";

                            // sending
                            byte[] b = Encoding.UTF8.GetBytes($"start::{json}");
                            try{
                                NetworkStream s = c.GetStream();
                                s.Write(b, 0, b.Length);
                            }
                            catch{}
                        }
                    }
                    // TcpBroadcast("start::");
                    Console.WriteLine($"[TCP] Game started!");
                    break;
            
                case "play": // Player play card
                    // Card rightness check
                    Card cc = Newtonsoft.Json.JsonConvert.DeserializeObject<Card>(parts[1]);
                    topCard = cc;
                    if(topCard != null) {
                        
                        if(cc.type == Card.CardType.wild || cc.type == Card.CardType.wildDrawFour) {
                            // Wild card
                            PlayCard(parts, json);
                            return;
                        }
                        else if(topCard.color == cc.color) {
                            // Same color
                            PlayCard(parts, json);
                            break;
                        }
                        else if(topCard.type == cc.type) {
                            // Same number
                            if(cc.type == Card.CardType.number)
                            {
                                if(cc.num == topCard.num)
                                {
                                    PlayCard(parts, json);
                                    break;
                                }
                            }
                            // Same Type
                            else
                            {
                                PlayCard(parts, json);
                                break;
                            }
                        }

                    }
                    // First card
                    else {
                        PlayCard(parts, json);
                    }
                    break;
            }

        }

        #region Disconnect
        // Disonnect if client disconnected
        DisconnectTCPClient(client);

        // ================== bad idea ==================
        //== Send every updated list if someone disconnected
        // 
        // lock(client) { // lock to prevent race condition (disconnect)
        //     TcpBroadcast(Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()));
        //     tcpClients.Remove(client);
        // }
        // client.Close();
        // Console.WriteLine("[LOG] Client disconnected!");
        #endregion
    
    }

    private void PlayCard(string[] parts, string json)
    {
        #region Skip
        Player lastPlayer = Newtonsoft.Json.JsonConvert.DeserializeObject<Player>(parts[2]);
        // Player nextPlayer;
        int skipCount = 1;
        // for(int i=0;i<players.Count;i++) {
        //     if(players[i].id == lastPlayer.id) {
        //         // if(i == players.Count-1) nextPlayer = players[0];
        //         // else nextPlayer = players[i+1];
        //         break;
        //     }
        // }
        #endregion

        // CardLogic
        switch(topCard.type) {

            case Card.CardType.skip: // Skip
                NextTurn(parts, json, 2);
                break;

            case Card.CardType.drawTwo: // Draw Two
                List<Card> cardsToSend = new List<Card>();
                Random r = new Random();

                for(int i=0;i<2;i++){
                    int rand = r.Next(0, drawDeck.Count);
                    cardsToSend.Add(drawDeck[rand]);
                    drawDeck.RemoveAt(rand);
                }
                NextTurn(parts, json, cardsToSend:cardsToSend);
                break;

            default: // Num
                NextTurn(parts, json);
                break;
        }

    }

    private void NextTurn(string[] parts, string json, int skipCount = 1, List<Card> cardsToSend = null)
    {
        int nextPlayerIndex = 0;
        Player lastPlayer = Newtonsoft.Json.JsonConvert.DeserializeObject<Player>(parts[2]);
        foreach(Player p in players) {
            if(p.id == lastPlayer.id) {
                if(p.isTurn) {
                    p.isTurn = false;
                    p.cardsLeft--;
                    Console.WriteLine($"card played: {parts[1]}");
                    
                    if(turnForward) nextPlayerIndex+=skipCount;
                    else nextPlayerIndex-=skipCount;
                    
                    if(nextPlayerIndex > players.Count-1) {
                        if(skipCount>1) nextPlayerIndex = 1;
                        else nextPlayerIndex = 0;
                    }
                    else if(nextPlayerIndex < 0) {
                        if(skipCount>1) nextPlayerIndex = players.Count - 2;
                        else nextPlayerIndex = players.Count - 1;
                    }

                    players[nextPlayerIndex].isTurn = true;

                    // Max 9 cards per player so remove some if > 9
                    // if(players[nextPlayerIndex].cardsLeft + cardsToSend.Count > 9) for(int i=0;i<players[nextPlayerIndex].cardsLeft + cardsToSend.Count-9;i++) cardsToSend.RemoveAt(i);
                    players[nextPlayerIndex].cardsLeft += cardsToSend.Count;
                    
                    // send all players updated top card and next layer that turn
                    json = $"{parts[1]}";
                    json += $"::{Newtonsoft.Json.JsonConvert.SerializeObject(players[nextPlayerIndex])}";
                    json += $"::{Newtonsoft.Json.JsonConvert.SerializeObject(cardsToSend)}";
                    TcpBroadcast($"updatetopcard::{json}");
                    break;
                }
            }
            nextPlayerIndex++;
        }
    }

    #region Game
    
    private void GenerateDrawDeck()
    {
        drawDeck.Clear();
        // add number cards
        //for(int i=0;i<108;i++)drawDeck.Add(new Card(Card.CardColor.red, Card.CardType.drawTwo));
        foreach(Card.CardColor color in Enum.GetValues(typeof(Card.CardColor)))
        {
            for(int i=1;i<10;i++)
            {
                if(color == Card.CardColor.none) continue;
                drawDeck.Add(new Card(color, Card.CardType.number, i));
                drawDeck.Add(new Card(color, Card.CardType.number, i));
            }
            if(color != Card.CardColor.none) drawDeck.Add(new Card(color, Card.CardType.number, 0));
        }
        // add action cards
        foreach(Card.CardColor color in Enum.GetValues(typeof(Card.CardColor))){
            foreach(Card.CardType type in Enum.GetValues(typeof(Card.CardType)))
            {
                if(type == Card.CardType.wild || type == Card.CardType.wildDrawFour || color == Card.CardColor.none || type == Card.CardType.number) continue;
                drawDeck.Add(new Card(color, type));
                drawDeck.Add(new Card(color, type));
            }
        }
        // add wild cards
        for(int i=0;i<4;i++)
        {
            drawDeck.Add(new Card(Card.CardColor.none, Card.CardType.wild));
            drawDeck.Add(new Card(Card.CardColor.none, Card.CardType.wildDrawFour));
        }

        // Debug cards
        // for(int i=0;i<drawDeck.Count;i++) Console.WriteLine(i + ": " + drawDeck[i].color + " " + drawDeck[i].type + " " + drawDeck[i].num);
    }
    
    #endregion
    
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

    #region Disconnect
    private void DisconnectTCPClient(TcpClient client)
    {
        try{
            if(client.Connected) 
            {
                client.Client.Shutdown(SocketShutdown.Both);
                tcpClients.Remove(client);
                // client.GetStream().Close();
                client.Close();
                // Console.WriteLine("[LOG] Client disconnected!");

                // TcpBroadcast(Newtonsoft.Json.JsonConvert.SerializeObject(players.ToArray()));
            }
        }
        catch(Exception e){
            Console.WriteLine("[ERR] TCP Disconnect: " + e);
        }
    }
    #endregion

}