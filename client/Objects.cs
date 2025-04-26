using Newtonsoft.Json;
using System;

namespace NoU;

public static class Utils
{
    public static string Serialize<T>(T obj)
    {
        try
        {
            return JsonConvert.SerializeObject(obj);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Serialization err: {ex.Message}");
            return string.Empty;
        }
    }
    public static T Deserialize<T>(string data)
    {
        try
        {
            return JsonConvert.DeserializeObject<T>(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Deserialization error: {ex.Message}");
            return default(T);
        }
    }
    public static string CreateMessage(int id, string type, object message)
    {
        return Serialize(new BaseMessage(id, type, message));
    }

    public static BaseMessage TrimData(string data)
    {
        string[] messages = data.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int brackets = 0;
        foreach (char i in messages[0])
        {
            if (i == '{') brackets++;
            else if (i == '}') brackets--;
        }
        if (brackets != 0) return null;
        BaseMessage msg = Deserialize<BaseMessage>(messages[0]);
        return msg;
    }
}

public class BaseMessage
{
    public BaseMessage(int id, string type, object message)
    {
        this.ID = id;
        this.Type = type;
        this.Data = message;
    }

    public int ID { get; set; }
    public string Type { get; set; }
    public object Data { get; set; }
}

//--------------------------------------------------------------------------------------------

public enum Screen
{
    NicknameInput,
    Lobby,
    Game,
    MainMenu
}

public enum CardColor
{
    None,
    Red,
    Green,
    Blue,
    Yellow
}

public enum CardType
{
    Number,
    Skip,
    Reverse,
    DrawTwo,
    Wild,
    WildDrawFour
}

public class Card
{
    public int id;
    public CardColor color;
    public CardType type;
    public int value;
}

public class Player
{
    public int id;
    public bool isHost = false;
    public string name;
    public List<Card> deck = new List<Card>();
}

public class GameState
{
    public Screen currentScreen = Screen.NicknameInput;
    public List<Player> players = new List<Player>();
    public int currentPlayer;
    public Card topCard;

    public List<ChatMessage> chat = new List<ChatMessage>();
}

public class ChatMessage
{
    public string sender;
    public string message;

    public ChatMessage(string sender, string message) {
        this.sender = sender;
        this.message = message;
    }
}