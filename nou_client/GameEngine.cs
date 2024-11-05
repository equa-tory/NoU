using System;

namespace NoUC;

public class GameEngine
{
    private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    private bool isRunning = true;
    private int frameRate = 100;

    private Client client;
    public List<Player> players = new List<Player>();

    private bool gameStarted = false;
    
    
    // ======================================================


    // Start()
    public GameEngine(string nickname, string ip, int port)
    {
        Console.CancelKeyPress += (sender, args) => Exit();

        Player localPlayer = new Player(nickname);
        client = new Client(ip, port, localPlayer, players);
        players.Add(localPlayer);
    }

    public void Run() { while (isRunning) Update(); }
    private void Update()
    {
        // DrawFrame
        if(!gameStarted) DrawLobby();

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

            case ConsoleKey.UpArrow:
                Console.WriteLine("\t"+Underline("Luzevg"));
                Console.WriteLine("Qdness"+"\t\t"+"Equa");
                Console.WriteLine("\t"+"Axlamon");
                break;

            case ConsoleKey.RightArrow:
                Console.WriteLine("\tLuzevg");
                Console.WriteLine("Qdness\t\t"+Underline("Equa"));
                Console.WriteLine("\tAxlamon");
                break;
                
            default:
                break;
        }
    }
    #endregion
   
    // ======================================================

    #region Frames
    private void DrawLobby()
    {
        if(players.Count >= 2) Console.WriteLine("Press S to start the game!");
        Console.WriteLine("=== Lobby: ===");

        int i = 0;
        foreach (Player player in players) {
            if(i == 0) Console.WriteLine(Underline(player.name));
            else Console.WriteLine(player.name);
            i++;
        }
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