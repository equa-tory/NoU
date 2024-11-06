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
        client = new Client(ip, port, localPlayer, players);
        players.Add(localPlayer);

        // Events
        client.OnGameStart += GameStart;
        client.OnStartCardsReceived += RecieveDeck;
    }

    public void Run() { while (isRunning) Update(); }
    private void Update()
    {
        // DrawFrame
        if(!gameStarted) DrawLobby();
        else DrawGame();

        if(Console.KeyAvailable) Input();

        Thread.Sleep(frameRate);
        Console.Clear();
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
                client.TCP("start");
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
    
    private void GameStart()
    {
        client.OnGameStart -= GameStart;
        gameStarted = true;
    }
    private void RecieveDeck(List<Card> startDeck)
    {
        deck.Clear();
        foreach (Card c in startDeck) deck.Add(c);
    }
    #endregion
   
    // ======================================================

    #region Frames
    private void DrawLobby()
    {
        // if players >2 and < 10 and local player is host
        if(players.Count >= 2 && players.Count <= 10 && localPlayer.id == players[0].id) 
            Console.WriteLine("Press S to start the game!");
        Console.WriteLine("=== Lobby: ===");

        // Print player names
        Console.WriteLine(Underline(localPlayer.name));
        foreach (Player player in players) 
            if(localPlayer.id != player.id) Console.WriteLine(player.name);
    }

    private void DrawGame()
    {
        Console.WriteLine(" === Deck: ===");
        foreach(var c in deck) Console.WriteLine($"{c.type} {c.color}");
    }
    #endregion

    // ======================================================

    #region Utils
    private void Exit()
    {
        // Disconnect from server
        client.Disconnect();

        //Log("Exit!");

        Console.WriteLine("Exiting...");
        cts.Cancel();
        Console.Clear();
    }

    private string Underline(string text) { return "\x1B[4m"+text+"\x1B[0m"; }

    private void Log(string msg)
    {
        File.AppendAllText("log.txt", System.Environment.NewLine + System.DateTime.Now + " # " + msg);
    }
    #endregion
}