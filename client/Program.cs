using System;

namespace NoU;

public class Program
{
    static void Main(string[] args)
    {
        string ip = "127.0.0.1";
        int port = 5000;

        var client = new Client(ip, port);
        // client();
    }
}