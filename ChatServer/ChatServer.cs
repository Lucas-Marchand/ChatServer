using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections;
using System.Text.Json;
using System.Collections.Concurrent;

namespace ChatServer
{
    class ChatServer
    {

        private static Hashtable connectedClients = new Hashtable();

        private static Hashtable channels = new Hashtable();

        public static void Main(String[] args)
        {
            TcpListener server;

            // setup channels the user are allowed to join
            channels.Add(("General"), new ConcurrentBag<TcpClient>());
            channels.Add(("Test"), new ConcurrentBag<TcpClient>());
            channels.Add(("Memes"), new ConcurrentBag<TcpClient>());

            var ipAddress = Dns.GetHostEntry("localhost").AddressList[0];
            
            var port = 5005;

            server = new TcpListener(ipAddress, port);
            server.Start();

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(client);
            }
        }

        public static void HandleClient (Object obj)
        {
            TcpClient client = (TcpClient)obj;

            StreamWriter writer = new StreamWriter(client.GetStream(), Encoding.ASCII);
            TextWriter sWriter = writer;

            Console.WriteLine("I got a connection");

            while (client.Connected)
            {
                StreamReader reader = new StreamReader(client.GetStream(), Encoding.ASCII);
                TextReader sReader = reader;

                try
                {
                    ChatCommand command = JsonSerializer.Deserialize<ChatCommand>(sReader.ReadLine());

                
                switch (command.Command)
                {
                    case Command.nick:
                        if (!connectedClients.ContainsKey(command.User))
                        {
                            Console.WriteLine("We have a new user: " + command.Arguments);
                            connectedClients.Add(command.Arguments, client);
                        }
                        break;
                    case Command.list:
                        Console.WriteLine("they want to know what channels are available");

                        var index = channels.Keys.GetEnumerator();
                        var keys = new StringBuilder();

                        while (index.MoveNext())
                        {
                            keys.Append(index.Current);
                            keys.Append(", ");
                        }

                        keys.Remove(keys.Length - 2, 1);
                        command.Message = keys.ToString();
                        command.Command = Command.receive_message;
                        sWriter.WriteLine(JsonSerializer.Serialize<ChatCommand>(command));
                        sWriter.Flush();
                        break;
                    case Command.join:
                        Console.WriteLine($"User {command.User} would like to join a channel {command.Arguments}");
                        if (channels.ContainsKey(command.Arguments))
                        {
                            ConcurrentBag<TcpClient> list = (ConcurrentBag<TcpClient>)channels[command.Arguments];
                            list.Add(client);
                        }
                        break;
                    case Command.leave:
                        break;
                    case Command.quit:
                        break;
                    case Command.help:
                        break;
                    case Command.stats:
                        break;
                    case Command.send_message:

                        ChatCommand newCommand = new ChatCommand
                        {
                            User = "server",
                            Command = Command.receive_message,
                            Arguments = String.Empty,
                            Message = (String) command.Message.Clone()
                        };

                        Console.WriteLine($"user: {command.User}; Channel: {command.Arguments}; Message: {command.Message}");
                        ConcurrentBag<TcpClient> users = (ConcurrentBag<TcpClient>) channels[command.Arguments];
                        var currentTcpClient = users.GetEnumerator();
                        foreach (TcpClient tcp in users)
                        {
                            if (tcp != client)
                            {
                                TextWriter w = new StreamWriter(tcp.GetStream(), Encoding.ASCII);
                                w.WriteLine(JsonSerializer.Serialize<ChatCommand>(newCommand));
                                w.Flush();
                            }
                        }
                        break;
                    default:
                        break;
                }
                }
                catch (System.IO.IOException e)
                {
                    Console.Error.WriteLine(e.Message);
                }
            }
        }
    }
}