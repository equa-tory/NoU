using System;

namespace NoUC;

public enum CardColor
{
    red,
    green,
    blue,
    yellow,
    black
}

public class Card
{
    protected string name { get; set; }
    protected string description;
    protected CardColor color { get; set; }

    public Card(CardColor color)
    {
        this.color = color;
    }

    public virtual void Use()
    {
        Console.WriteLine("Using card: " + name + " Color: " + color);
    }
}
public class NumCard : Card
{
    private int number { get; set; }

    public NumCard(int number, CardColor color) : base(color)
    {
        this.number = number;
    }

    public override void Use()
    {
        Console.WriteLine("Using number card: " + number + " Color: " + color);
    }
}
public class ActionCard : Card
{
    public ActionCard(string name, CardColor color) : base(color)
    {
        this.name = name;
    }

    public override void Use()
    {
        Console.WriteLine("Using action card: " + name + " Color: " + color);
    }
}

public class Player
{
    private string name { get; set; }
    private List<Card> cards { get; set; }

    public Player(string name, List<Card> cards)
    {
        this.name = name;
        cards = new List<Card>();
        this.cards = cards;
    }
}