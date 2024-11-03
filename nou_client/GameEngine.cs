using System;

namespace NoUC;

public class GameEngine
{
    private static readonly CancellationTokenSource cts = new CancellationTokenSource();
    private bool isRunning = true;
    private int frameRate = 100;
    

    // ======================================================

    public GameEngine()
    {
        Console.CancelKeyPress += (sender, args) => Exit();
    }

    public void Run() { while (isRunning) Update(); }
    private void Update()
    {
        // DrawFrame

        if(Console.KeyAvailable) Input();

        Thread.Sleep(frameRate);
        Console.Clear();
    }
    
    // ======================================================

    private void Input()
    {
        ConsoleKeyInfo key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.Q:
                isRunning = false;
                Exit();
                return;

            case ConsoleKey.DownArrow:
                break;

            case ConsoleKey.Spacebar:
                break;
                
            default:
                break;
        }
    }
    
    private void Exit()
    {

        //Log("Exit!");

        Console.WriteLine("Exiting...");
        cts.Cancel();
        Console.Clear();
    }
    private void Log(string msg)
    {
        File.AppendAllText("log.txt", System.Environment.NewLine + System.DateTime.Now + " # " + msg);
    }
}