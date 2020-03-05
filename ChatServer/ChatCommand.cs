using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ChatServer
{
    public enum Command
    {
        connect,
        nick,
        list,
        join,
        leave,
        quit,
        help,
        stats,
        send_message,
        receive_message
    }

    [Serializable]
    public class ChatCommand
    {
        public String User { get; set; }

        public Command Command { get; set; }

        public String Arguments { get; set; }

        public String Message { get; set; }
        
        public String Channel { get; set; }
    }
}
