using ChatServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace IRCServer
{
    class ChatClient
    {
        public static void Main(String[] args)
        {
            ConcurrentBag<ChatCommand> commandsToProcess = new ConcurrentBag<ChatCommand>();
            TcpClient socket = null;

            String nick = "";
            String channel = "";

            Thread acceptUserInput = new Thread(new ParameterizedThreadStart(AcceptUserInput));
            acceptUserInput.Start(commandsToProcess);
            
            ChatCommand currentCommand;

            while (true) 
            {
                if (commandsToProcess.TryTake(out currentCommand))
                {
                    switch (currentCommand.Command)
                    {
                        case Command.connect:
                            socket = new TcpClient(currentCommand.Arguments, 5005);
                            break;
                        case Command.list:
                            SendCommandToServer(socket, currentCommand);
                            break;
                        case Command.nick:
                            nick = currentCommand.Arguments;
                            SendCommandToServer(socket, currentCommand);
                            break;
                        case Command.join:
                            SendCommandToServer(socket, currentCommand);
                            channel = currentCommand.Arguments;
                            break;
                        case Command.send_message:
                            currentCommand.User = nick;
                            currentCommand.Channel = channel;
                            SendCommandToServer(socket, currentCommand);
                            break;
                    }
                }
            }
        }

        static string[] ParseArguments(string commandLine)
        {
            char[] parmChars = commandLine.ToCharArray();
            bool inQuote = false;
            for (int index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"')
                {
                    inQuote = !inQuote;
                }
                if (!inQuote && parmChars[index] == ' ')
                {
                    parmChars[index] = '\n';
                }
            }
            return (new string(parmChars)).Split('\n');
        }

        private static void SendCommandToServer(TcpClient server, ChatCommand currentCommand)
        {
            using (TextWriter sWriter = new StreamWriter(server.GetStream(), Encoding.ASCII))
            {
                sWriter.WriteLine(JsonSerializer.Serialize<ChatCommand>(currentCommand));
                sWriter.Flush();
            }
        }

        private static void AcceptIncomingMessage(TcpClient server)
        {
            TextReader reader = new StreamReader(server.GetStream(), Encoding.ASCII);
            while (server.Connected)
            {
                String readCommand = reader.ReadLine();
                if (readCommand != "" && readCommand != null)
                {
                    Console.WriteLine(JsonSerializer.Deserialize<ChatCommand>(readCommand).Message);
                }
            }
        }

        static void AcceptUserInput(Object obj)
        {
            ConcurrentBag<ChatCommand> bag = (ConcurrentBag<ChatCommand>) obj;

            String nick = "";
            while (true)
            {
                String originalS = Console.ReadLine();
                String[] s = ParseArguments(originalS);

                switch (s[0])
                {
                    case "/connect":
                        bag.Add(
                            new ChatCommand
                            {
                                User = nick,
                                Command = Command.connect,
                                Arguments = s[1],
                                Message = String.Empty
                            }
                        );
                        break;
                    case "/nick":
                        bag.Add(
                            new ChatCommand
                            {
                                User = nick,
                                Arguments = s[1],
                                Command = Command.nick,
                                Message = String.Empty
                            }
                        );
                        break;
                    case "/list":
                        bag.Add(
                            new ChatCommand
                            {
                                User = nick,
                                Arguments = "",
                                Command = Command.list,
                                Message = String.Empty
                            }
                        );
                        break;
                    case "/join":
                        bag.Add(
                            new ChatCommand
                            {
                                User = nick,
                                Arguments = s[1],
                                Command = Command.join,
                                Message = String.Empty
                            }
                        );
                        break;
                    case "/leave":
                        break;
                    case "/quit":
                        break;
                    case "/help":
                        break;
                    case "/stats":
                        break;
                    default:
                        bag.Add(
                            new ChatCommand
                            {
                                User = nick,
                                Arguments = "",
                                Message = originalS,
                                Command = Command.send_message
                            }
                        );
                        break;
                }
            }
        }
    }
}
