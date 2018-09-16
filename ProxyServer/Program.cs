using System;
using System.Net;
using System.Threading;

namespace ProxyServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            Console.WriteLine("Listening...");
            while (true)
            {
                var ctx = listener.GetContext();
                new Thread(new Relay(ctx).ProcessRequest).Start();
            }
        } 
    }
}
