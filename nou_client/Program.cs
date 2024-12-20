﻿using System;

namespace NoUC;

public class Program
{
    
    public static void Main(string[] args)
    {
        string nickname = "";
        string ip = "127.0.0.1";
        int port = 3108;

        // if(args.Length > 0) nickname = args[0]; // nicname input
        if(args.Length > 0) ip = args[0]; // ip input
        else if(args.Length > 1) port = int.Parse(args[1]); // port input

        // Nickname input
        Console.Clear();
        Console.WriteLine("Enter nickname: ");
        do{ nickname = Console.ReadLine();
        } while(nickname.Length < 2 || nickname.Length > 8 || nickname.Contains(":"));
        Console.Clear();

        // Game start
        GameEngine game = new GameEngine(nickname, ip, port);
        game.Run();
    }
}