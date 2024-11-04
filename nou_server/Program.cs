using System;
using System.Net;
using System.Net.Sockets;

namespace NoUS;

public class Program
{
    public static void Main(string[] args)
    {
        Server server = new Server("127.0.0.1", 3108);
        server.Start();
    }
}