using System;

namespace NoUC;

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

    public void Use()
    {
        Console.WriteLine("Using card: " + type.ToString() + " Color: " + color);
    }
}

public class Player
{
    public int id { get; set; }
    public string name { get; set; }
    public int cardsLeft { get; set; }

    public Player(string name, int cardsLeft = 7)
    {
        this.name = name;
        this.cardsLeft = cardsLeft;
        Random r = new Random();
        this.id = r.Next(0, 10000);
    }
}