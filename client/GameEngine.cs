using System;
using System.Collections.Generic;
using System.Drawing;

namespace NoU;

public class GameEngine
{
    #region Variables
    public Client client;
    public bool isRunning = false;

    private GameState gameState = new GameState();
    private Player player = new Player();
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Functions
    public GameEngine(string ip, int port)
    {
        #region Client init
        // Client init
        this.client = new Client(ip, port);
        this.client.OnGameStateUpdate += OnGameStateUpdate;

        Console.Clear();
        Console.CancelKeyPress += (sender, e) => client.Disconnect();
        #endregion

        #region  Nickname input
        DrawFrame();
        #endregion

        // Game loop
        this.isRunning = true;
        while (isRunning && gameState.currentScreen != Screen.Game)
        {
            Input();
        }
    }

    private void Input()
    {
        ConsoleKeyInfo key = Console.ReadKey(true);

        switch (key.Key)
        {
            case ConsoleKey.Q:
                client.Disconnect();
                isRunning = false;
                break;

            case ConsoleKey.L:
                int i=0;
                Console.WriteLine("---Players:---");
                foreach (Player player in gameState.players){
                    i++;
                    Console.WriteLine($"{i}. {player.name}#{player.id}");
                }
                break;
            
            case ConsoleKey.D:
                client.TCP("LOG", $"Ayo it's me {player.name}#{client.id}");
                break;
            
            case ConsoleKey.A:
                Console.Clear();
                break;

            case ConsoleKey.S:
                if(!player.isHost) return;
                client.TCP("START", gameState);
                break;

            default:
                break;
        }
    }

    public void DrawFrame()
    {
        Console.Clear();

        Console.WriteLine($"═══════ NoU: {gameState.currentScreen} ═══════");

        // Frames
        switch(gameState.currentScreen){

            case Screen.NicknameInput:
                // Nickname input
                Console.Write("Enter nick: ");
                string nickname;
                do{
                    nickname = Console.ReadLine();
                }while(string.IsNullOrEmpty(nickname) || nickname.Length < 0);

                client.TCP("NICK", nickname);
                break;

            case Screen.Lobby:
                for (int i = 0; i < gameState.players.Count; i++) {
                    if(player.id == gameState.players[i].id) 
                        Console.Write($"*{gameState.players[i].name}#{gameState.players[i].id}*");
                    else Console.Write($"{gameState.players[i].name}#{gameState.players[i].id}");

                    if(i != gameState.players.Count - 1) Console.Write(", ");
                    if(i >= 1) Console.WriteLine();
                }
                if(player.isHost) Console.WriteLine("\nPress S to start the game");
                break;

            case Screen.Game:
                DrawGameFrame();
                break;

            case Screen.MainMenu:
                break;
        
            default:
                Console.WriteLine("Unknown screen");
                break;
        }

        Console.WriteLine("══════════════════════════");
        // Console.WriteLine("═══════════════════");
    }

    private void DrawGameFrame() {
        // Console.WriteLine($"You are {player.name}#{player.id}");
        // Console.WriteLine($"Cards: {player.deck.Count}");

        // foreach (Card card in player.deck) {
        //     Console.WriteLine($"Card: {card.color} {card.type}");
        // }

        #region Players list
        for(int i=0;i<gameState.players.Count;i++){
            Player p = gameState.players[i];
            string nameToDisplay = gameState.currentPlayer == p.id ? $"*{p.name}*" : $"{p.name}";
            nameToDisplay += i<gameState.players.Count-1 ? ", " : "";
            Console.Write($"{nameToDisplay}");
        }
        #endregion

        #region Top card
        Console.WriteLine($"\n\nTop card: {GetCardString(gameState.topCard)}");
        #endregion

        Console.WriteLine("══════════════════════════");

        #region Players deck
        Console.WriteLine("Your deck:");
        for(int i=0;i<player.deck.Count;i++){
            Card card = player.deck[i];
            Console.Write($"[{i+1}] {GetCardString(card)} ");

            if ((i + 1) % 3 == 0) Console.WriteLine();        
        }
        #endregion

        #region Chat
        Console.WriteLine("\n══════════ Chat ══════════");
        foreach (ChatMessage msg in gameState.chat) {
            Console.WriteLine($"{msg.sender}: {msg.message}");
        }
        #endregion
    }

    private string GetCardString(Card card) {
        string topCardColor = card.color == CardColor.None ? "⚫️" : 
            card.color == CardColor.Red ? "🔴" :
            card.color == CardColor.Green ? "🟢" :
            card.color == CardColor.Blue ? "🔵" :
            card.color == CardColor.Yellow ? "🟡" : "Unknown";

        string topCardType = card.type == CardType.Number ? card.value.ToString() : card.type.ToString();
        
        return $"{topCardColor} {topCardType}";
    }
    #endregion

    //--------------------------------------------------------------------------------------------

    public void OnGameStateUpdate(GameState state) {
        if(!isRunning) return;

        this.gameState = state;
        this.player = state.players.Find(p => p.id == client.id);

        DrawFrame();
    }

}