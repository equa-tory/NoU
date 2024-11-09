using System;

namespace NoUC;

public enum GameState
{
    Lobby,
    Started,
    Ended
}

public class GameEngine
{

    #region Variables

    private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    private bool isRunning = true;
    public int frameRate = 100;

    private Client client;
    private Player localPlayer;
    public List<Player> players = new List<Player>();
    private List<Card> deck = new List<Card>();
    private Card topCard = null;

    private GameState gameState = GameState.Lobby;

    #endregion
    
    // ======================================================

    #region Main
    // Start()
    public GameEngine(string nickname, string ip, int port)
    {
        Console.CancelKeyPress += (sender, args) => Exit();

        Random r = new Random();
        localPlayer = new Player(name: nickname, id: r.Next(0, 10000));
        client = new Client(ip, port, localPlayer);
        players.Add(localPlayer);

        // Events
        client.OnLobbyUpdated += UpdateLobby;
        client.OnGameStart += GameStart;
        client.OnTopCardUpdated += UpdateTopCard;

        DrawLobby();
    }

    public void Run() { while (isRunning) Update(); }
    private void Update()
    {
        // DrawFrame
        // if(!gameStarted) DrawLobby();
        // else DrawGame();

        if(Console.KeyAvailable) Input();

        Thread.Sleep(frameRate);
        // Console.Clear();
    }
    
    #endregion

    // ======================================================

    #region Game
    
    private void Input()
    {
        ConsoleKeyInfo key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.Q:
                isRunning = false;
                Exit();
                return;

            case ConsoleKey.S:
                if(gameState != GameState.Started && players.Count >= 2 && players.Count <= 10 && localPlayer.id == players[0].id)
                    client.TCP("start::");
                break;

            case ConsoleKey.D1:
            case ConsoleKey.D2:
            case ConsoleKey.D3:
            case ConsoleKey.D4:
            case ConsoleKey.D5:
            case ConsoleKey.D6:
            case ConsoleKey.D7:
                if(!localPlayer.isTurn) break;
                if(gameState != GameState.Started) break;

                int numberPressed = (int)char.GetNumericValue(key.KeyChar)-1;
                if(numberPressed >= deck.Count) break;

                // ===================== Turn logic =====================
                Card cc = deck[numberPressed];
                if(topCard != null) {
                    
                    if(topCard.color == cc.color) {
                        // Same color
                        if(cc.type != Card.CardType.wild && cc.type != Card.CardType.wildDrawFour) {
                            PlayCard(cc);
                            break;
                        }
                        // Same wild
                        else{
                            if(topCard.type == Card.CardType.wild) {PlayCard(cc);break;}
                            else if(topCard.type == Card.CardType.wildDrawFour) {PlayCard(cc);break;}
                        }
                    }
                    else if(topCard.type == cc.type) {
                        // Same number
                        if(cc.type == Card.CardType.number)
                        {
                            if(cc.num == topCard.num)
                            {
                                PlayCard(cc);
                                break;
                            }
                        }
                        // Same Type
                        else
                        {
                            PlayCard(cc);
                            break;
                        }
                    }

                }
                // First card
                else PlayCard(cc);
                break;


            // case ConsoleKey.UpArrow:
            //     Console.WriteLine("\t"+Underline("Luzevg"));
            //     Console.WriteLine("Qdness"+"\t\t"+"Equa");
            //     Console.WriteLine("\t"+"Axlamon");
            //     break;

            // case ConsoleKey.RightArrow:
            //     Console.WriteLine("\tLuzevg");
            //     Console.WriteLine("Qdness\t\t"+Underline("Equa"));
            //     Console.WriteLine("\tAxlamon");
            //     break;
                
            default:
                break;
        }
    }

    private void PlayCard(Card card)
    {
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(card);
        deck.Remove(card);
        localPlayer.cardsLeft--;
        localPlayer.isTurn = false;
        json += $"::{Newtonsoft.Json.JsonConvert.SerializeObject(localPlayer)}";
        client.TCP($"play::{json}");
    }
    
    #endregion

    #region Events

    private void UpdateLobby(List<Player> players)
    {
        this.players.Clear();
        foreach (Player p in players) this.players.Add(p);
        DrawLobby();
    }
    
    private void GameStart(List<Card> startDeck, int startPlayerId)
    {
        gameState = GameState.Started;
        this.deck.Clear();
        this.deck = startDeck;
        topCard = null;

        localPlayer.isTurn = startPlayerId == localPlayer.id;

        DrawGame();
    }

    private void UpdateTopCard(Card topCard, Player nextPlayer)
    {
        localPlayer.isTurn = nextPlayer.id == localPlayer.id;
        this.topCard = topCard;
        DrawGame();
    }
    
    #endregion
   
    #region Frames

    private void DrawLobby()
    {
        gameState = GameState.Lobby;
        Console.Clear();
        // if players > 2 and < 10 and local player is host
        if(players.Count >= 2 && players.Count <= 10 && localPlayer.id == players[0].id) 
            Console.WriteLine("Press S to start the game!");
        Console.WriteLine(" === Lobby === ");

        // Print player names
        Console.WriteLine(Underline(localPlayer.name));
        foreach (Player player in players) 
            if(localPlayer.id != player.id) Console.WriteLine(player.name);
    }

    private void DrawGame()
    {
        Console.Clear();
        Console.WriteLine($"Top Card: {CardToText(topCard)}");
        Console.WriteLine(" === Deck === ");

        int i = 0;
        foreach (Card c in deck) {
            i++;

            // Card color in console
            // Console.ForegroundColor = c.color == Card.CardColor.red ? ConsoleColor.Red : c.color == Card.CardColor.blue ? ConsoleColor.Blue : c.color == Card.CardColor.green ? ConsoleColor.Green : c.color == Card.CardColor.yellow ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.Write($"{i}.");
            Console.WriteLine(CardToText(c));
            // Reset color
            // Console.ResetColor();

        }
        Console.WriteLine($"Your turn: {localPlayer.isTurn}");
    }

    #endregion

    // ======================================================

    #region Utils

    private void Exit()
    {
        // Disconnect from server
        client.Disconnect();

        Console.WriteLine("Exiting...");
        cts.Cancel();
        Console.Clear();

        client.OnLobbyUpdated -= UpdateLobby;
        client.OnGameStart -= GameStart;
        client.OnTopCardUpdated -= UpdateTopCard;
    }

    private string Underline(string text) { return "\x1B[4m"+text+"\x1B[0m"; }

    private void Log(string msg)
    {
        File.AppendAllText("log.txt", Environment.NewLine + DateTime.Now + " # " + msg);
    }
 
    private string CardToText(Card c)
    {
        string res = "";
        if(c == null) return "";
        switch(c.type)
        {
            case Card.CardType.number:
                res = $"[{c.num.ToString().ToLower()[0]}]({c.color.ToString().ToLower()[0]})";
                break;
            
            case Card.CardType.wild:
                res = $"[{c.type.ToString().ToLower()[0]}]";
                break;
            
            case Card.CardType.wildDrawFour:
                res = $"[+4w]";
                break;

            case Card.CardType.drawTwo:
                res = $"[+2]({c.color.ToString().ToLower()[0]})";
                break;

            default:
                res = $"[{c.type.ToString().ToLower()[0]}]({c.color.ToString().ToLower()[0]})";
                break;
        }
        return res;
    }
    
    #endregion

}