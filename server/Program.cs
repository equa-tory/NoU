using System;

namespace NoU;

public class Program
{
    static void Main(string[] args)
    {
        string ip = "127.0.0.1";
        int port = 5000;

        var server = new Server(ip, port);
        server.Run();
    }
}