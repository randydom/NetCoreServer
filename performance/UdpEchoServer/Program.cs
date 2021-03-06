﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetCoreServer;
using NDesk.Options;
using System.Threading.Tasks;

namespace UdpEchoServer
{
    class EchoServer : UdpServer
    {
        public EchoServer(IPAddress address, int port) : base(address, port) {}

        protected override void OnStarted()
        {
            // Start receive datagrams
            Receive();
        }

        protected override void OnReceived(IPEndPoint endpoint, byte[] buffer, long size)
        {
            // Echo the message back to the sender
            SendAsync(endpoint, buffer, 0, size);
        }

        protected override void OnSent(IPEndPoint endpoint, long sent)
        {
            // Continue receive datagrams. 
            // Important: Receive using thread pool is neccessary here to avoid stack overflow with Socket.ReceiveFromAsync() method!
            ThreadPool.QueueUserWorkItem(o => { Receive(); } );
        }

        protected override void OnError(SocketError error)
        {
            Console.WriteLine($"Server caught an error with code {error}");
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            bool help = false;
            int port = 3333;

            var options = new OptionSet()
            {
                { "h|?|help",   v => help = v != null },
                { "p|port=", v => port = int.Parse(v) }
            };

            try
            {
                options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("Command line error: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `--help' to get usage information.");
                return;
            }

            if (help)
            {
                Console.WriteLine("Usage:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            Console.WriteLine($"Server port: {port}");

            // Create a new echo server
            var server = new EchoServer(IPAddress.Any, port);
            server.OptionReuseAddress = true;
            server.OptionReusePort = true;

            // Start the server
            Console.Write("Server starting...");
            server.Start();
            Console.WriteLine("Done!");

            Console.WriteLine("Press Enter to stop the server or '!' to restart the server...");

            // Perform text input
            for (;;)
            {
                string line = Console.ReadLine();
                if (line == string.Empty)
                    break;

                // Restart the server
                if (line == "!")
                {
                    Console.Write("Server restarting...");
                    server.Restart();
                    Console.WriteLine("Done!");
                }
            }

            // Stop the server
            Console.Write("Server stopping...");
            server.Stop();
            Console.WriteLine("Done!");
        }
    }
}
