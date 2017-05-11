using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Discord;
using Discord.Commands;
using Discord.Audio;

using NAudio.Wave;

namespace PeanutBot
{
    public class Bot
    {
        private DiscordClient client;
        private Music musicHandler;
        private Text textHandler;

          
        public Bot()
        {          
            //initialize client
            DiscordConfigBuilder builder = new DiscordConfigBuilder();
            builder.LogLevel = LogSeverity.Info;
            builder.LogHandler = new EventHandler<LogMessageEventArgs>(Bot_LogMessage);
            
            client = new DiscordClient(builder);
           

            //chat setting
            client.UsingCommands(input =>
            {
                input.PrefixChar = '=';
                input.AllowMentionPrefix = true;         
            });

            //audio setting
            client.UsingAudio(x => // Opens an AudioConfigBuilder so we can configure our AudioService
            {
                x.Mode = AudioMode.Outgoing; // Tells the AudioService that we will only be sending audio
            });

            musicHandler = new Music(client);
            textHandler = new Text(client, musicHandler);
            textHandler.Initialize();

            client.ExecuteAndWait(async () =>
            {
                await client.Connect("MzExNDE1NzQ1MDI2NDU3NjAy.C_MMCQ.lKnK6M3qSHIQJ22repiqF82fgAw", TokenType.Bot);
            });

        }

        private void Bot_LogMessage(object sender, LogMessageEventArgs e)
        {
            Console.WriteLine(e.Message);
        }
              
  

    }
}
