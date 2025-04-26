using System;
using System.Collections.Generic;
using System.Linq;

namespace NoU;

public class GameLogic
{
    #region Variables
    private Server server;

    private List<Card> drawDeck = new List<Card>();
    private GameState gameState;
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Main Functions
    public GameLogic(GameState gameState, Server server)
    {
        this.gameState = gameState;
        this.server = server;

        GenerateCards();
        GiveawayCards();
        // Select random player to start
        int index = new Random().Next(gameState.players.Count);
        gameState.currentPlayer = gameState.players[index].id;

        // Top card select
        gameState.topCard = drawDeck[new Random().Next(drawDeck.Count)];
        drawDeck.Remove(gameState.topCard);

        server.BroadcastTCP("STATE", gameState);
    }

    private void GenerateCards()
    {
        drawDeck.Clear();
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };
        int idCounter = 0;

        foreach (var color in colors)
        {
            // One 0 card
            drawDeck.Add(new Card { id = idCounter++, color = color, type = CardType.Number, value = 0 });

            // Two of each 1-9
            for (int i = 1; i <= 9; i++)
            {
                drawDeck.Add(new Card { id = idCounter++, color = color, type = CardType.Number, value = i });
                drawDeck.Add(new Card { id = idCounter++, color = color, type = CardType.Number, value = i });
            }

            // Two Skip, Reverse, DrawTwo
            for (int i = 0; i < 2; i++)
            {
                drawDeck.Add(new Card { id = idCounter++, color = color, type = CardType.Skip });
                drawDeck.Add(new Card { id = idCounter++, color = color, type = CardType.Reverse });
                drawDeck.Add(new Card { id = idCounter++, color = color, type = CardType.DrawTwo });
            }
        }

        // Four Wild and WildDrawFour
        for (int i = 0; i < 4; i++)
        {
            drawDeck.Add(new Card { id = idCounter++, color = CardColor.None, type = CardType.Wild });
            drawDeck.Add(new Card { id = idCounter++, color = CardColor.None, type = CardType.WildDrawFour });
        }

        // Shuffle
        var rand = new Random();
        drawDeck = drawDeck.OrderBy(c => rand.Next()).ToList();
    }

    private void GiveawayCards()
    {
        foreach (var player in gameState.players)
        {
            player.deck = drawDeck.GetRange(0, 7);
            drawDeck.RemoveRange(0, 7);
        }
    }

    #region  TODO: REDO MIGRATION TO NEXT PLAYER, NOT FIRST!!!
    public void NextTurn()
    {
        gameState.currentPlayer++;
        if (gameState.currentPlayer >= gameState.players.Count) gameState.currentPlayer = 0;
        server.BroadcastTCP("STATE", gameState);
    }
    #endregion
    #endregion

    //--------------------------------------------------------------------------------------------

    #region Gameplay Functions
    #endregion

}