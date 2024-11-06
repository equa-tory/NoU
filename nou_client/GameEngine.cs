using System;

namespace NoUC;

public class GameEngine
{
    private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    private bool isRunning = true;
    public int frameRate = 100;

    private Client client;
    private Player localPlayer;
    public List<Player> players = new List<Player>();
    private List<Card> deck = new List<Card>();

    private bool gameStarted = false;
    
    
    // ======================================================


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
        client.OnStartCardsReceived += RecieveDeck;
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
                if(!gameStarted && players.Count >= 2 && players.Count <= 10 && localPlayer.id == players[0].id)
                    client.TCP("start::");
                break;

            case ConsoleKey.D1:
            case ConsoleKey.D2:
            case ConsoleKey.D3:
            case ConsoleKey.D4:
            case ConsoleKey.D5:
            case ConsoleKey.D6:
            case ConsoleKey.D7:
                if(!gameStarted) break;
                int numberPressed = (int)char.GetNumericValue(key.KeyChar);
                Card card = deck[numberPressed];
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(card);
                client.TCP($"play::{json}");
                deck.RemoveAt(numberPressed);
                DrawGame();
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
    
    private void UpdateLobby(List<Player> tmp_players)
    {
        players.Clear();
        foreach (Player p in tmp_players) players.Add(p);
        DrawLobby();
    }
    private void GameStart()
    {
        gameStarted = true;
        DrawGame();
    }
    private void RecieveDeck(List<Card> startDeck)
    {
        deck.Clear();
        int i = 0;
        foreach (Card c in startDeck) { 
            deck.Add(c);
            i++;
            Console.Write($"{i}.");

            // Card color in console
            // Console.ForegroundColor = c.color == Card.CardColor.red ? ConsoleColor.Red : c.color == Card.CardColor.blue ? ConsoleColor.Blue : c.color == Card.CardColor.green ? ConsoleColor.Green : c.color == Card.CardColor.yellow ? ConsoleColor.Yellow : ConsoleColor.White;

            switch(c.type)
            {
                case Card.CardType.number:
                    Console.WriteLine($"[{c.num.ToString().ToLower()[0]}]({c.color.ToString().ToLower()[0]})");
                    break;
                
                case Card.CardType.wild:
                    Console.WriteLine($"[{c.type.ToString().ToLower()[0]}]");
                    break;
                
                case Card.CardType.wildDrawFour:
                    Console.WriteLine($"[+4w]");
                    break;

                case Card.CardType.drawTwo:
                    Console.WriteLine($"[+2]({c.color.ToString().ToLower()[0]})");
                    break;

                default:
                    Console.WriteLine($"[{c.type.ToString().ToLower()[0]}]({c.color.ToString().ToLower()[0]})");
                    break;
            }
            // Reset color
            // Console.ResetColor();

        }
    }
    private void UpdateTopCard(Card topCard)
    {
        DrawGame(topCard);
    }
    #endregion
   
    // ======================================================

    #region Frames
    private void DrawLobby()
    {
        Console.Clear();
        // if players >2 and < 10 and local player is host
        if(players.Count >= 2 && players.Count <= 10 && localPlayer.id == players[0].id) 
            Console.WriteLine("Press S to start the game!");
        Console.WriteLine(" === Lobby === ");

        // Print player names
        Console.WriteLine(Underline(localPlayer.name));
        foreach (Player player in players) 
            if(localPlayer.id != player.id) Console.WriteLine(player.name);
    }

    private void DrawGame(Card topCard = null)
    {
        Console.Clear();
        Console.WriteLine($"Top Card: {topCard?.type} {topCard?.color}");
        Console.WriteLine(" === Deck === ");
        foreach(var c in deck) Console.WriteLine($"{c.type} {c.color}");
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
        client.OnStartCardsReceived -= RecieveDeck;
        client.OnTopCardUpdated -= UpdateTopCard;
    }

    private string Underline(string text) { return "\x1B[4m"+text+"\x1B[0m"; }

    private void Log(string msg)
    {
        File.AppendAllText("log.txt", Environment.NewLine + DateTime.Now + " # " + msg);
    }
    #endregion
}