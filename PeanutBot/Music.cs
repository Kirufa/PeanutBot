using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Threading;

using Discord;
using Discord.Audio;

using System.ComponentModel;
using Discord.Commands;
using System.Diagnostics;

namespace PeanutBot
{
    public class Music
    {
        private DiscordClient client;
        private SortedDictionary<ulong, Player> players;
        private object playersLock;

        private BackgroundWorker playWorker;

        const string defaultTextChannel = "note";



        public Music(DiscordClient client)
        {
            this.client = client;
            players = new SortedDictionary<ulong, Player>();
            playersLock = new object();

            playWorker = new BackgroundWorker();
            playWorker.WorkerSupportsCancellation = true;
            playWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(PlayWorker_RunWorkerCompleted);
            playWorker.DoWork += new DoWorkEventHandler(PlayWorker_DoWork);
            playWorker.RunWorkerAsync();
        }

        ~Music()
        {
            playWorker.CancelAsync();
        }

        public async Task<string[]> Play(Channel channel)
        {
            Player player;
            bool exist;

            string title = "";

            lock (playersLock)
                exist = players.TryGetValue(channel.Server.Id, out player);

            if (!exist) return new string[] { "", "Bot is not in voice channel." };

            if (player.Playing) return new string[] { "", "It is playing now." };

            if (player.Playlist.Count == 0) return new string[] { "", "The playlist is empty." };

            string url;

            lock (player.ListLock)
            {
                title = player.Playlist[0].Title;
                url = player.Playlist[0].Url;

                player.Playlist.RemoveAt(0);
            }

            IAudioClient VClient = await client.GetService<AudioService>().Join(channel);
           
            player.NowPlayingWorker = SendAudio(url, VClient);
            player.CompletedStop = false;
            player.NowPlayingWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((object sender, RunWorkerCompletedEventArgs e) =>
            {               
                player.CompletedStop = true;
            });

            player.AutoPlay = true;

            return new string[] { title, "" };
        }



        public string Stop(User user)
        {
            Player player;
            bool exist;
          

            lock (playersLock)
                exist = players.TryGetValue(user.Server.Id, out player);

            if (!exist) return "Bot is not in voice channel.";

            if (!player.Playing) return "There are no songs playing now.";


            player.NowPlayingWorker.CancelAsync();
            while (!player.CompletedStop)
                Thread.Sleep(500);
         
            
            player.NowPlayingWorker = null;
            player.AutoPlay = false;

            return "";
        }



        public string Enqueue(Song song, Channel channel)
        {
            Player player;
            bool exist;

            lock (playersLock)
                exist = players.TryGetValue(channel.Server.Id, out player);

            if (!exist) return "Bot is not in voice channel.";

            lock (player.ListLock)
                player.Playlist.Add(song);

            return "";
        }

        public async Task<Player> JoinChannel(User user)
        {
            Player player;
            bool exist;

            lock (playersLock)
                exist = players.TryGetValue(user.Server.Id, out player);

            if (!exist)
            {
                player = new Player();
                await client.GetService<AudioService>().Join(user.VoiceChannel);
                player.Id = user.Server.Id;
                player.ChannelId = user.VoiceChannel.Id;

                lock (playersLock)
                    players.Add(player.Id, player);
            }
            else
            {
                lock (playersLock)
                    players.Remove(player.Id);

                await client.GetService<AudioService>().Join(user.VoiceChannel);
                player.Id = user.Server.Id;
                player.ChannelId = user.VoiceChannel.Id;

                lock (playersLock)
                    players.Add(player.Id, player);
            }
            return player;
        }

        public async Task<bool> LeaveChannel(User user)
        {

            Player player;
            bool exist;

            lock (playersLock)
                exist = players.TryGetValue(user.Server.Id, out player);

            if (exist)
            {
                // player.stop();
                players.Remove(player.Id);
                await client.GetService<AudioService>().Leave(user.Server);
                
            }

            return exist;
        }


        public BackgroundWorker SendAudio(string url, IAudioClient vClient)
        {          
            var process = Process.Start(new ProcessStartInfo
            { // FFmpeg requires us to spawn a process and hook into its stdout, so we will create a Process
                FileName = @"F:\Program Files Portable\ffmpeg\bin\ffmpeg.exe",
                Arguments = $"-i \"" + url + // Here we provide a list of arguments to feed into FFmpeg. -i means the location of the file/URL it will read from
                       "\" -f s16le -ar 48000 -ac 2 pipe:1", // Next, we tell it to output 16-bit 48000Hz PCM, over 2 channels, to stdout.
                UseShellExecute = false,
                RedirectStandardOutput = true // Capture the stdout of the process
            });
            Thread.Sleep(2000); // Sleep for a few seconds to FFmpeg can start processing data.


            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            int blockSize = 3840; // The size of bytes to read per frame; 1920 for mono
            byte[] buffer = new byte[blockSize];
            int byteCount;


            worker.DoWork += new DoWorkEventHandler((object sender, DoWorkEventArgs e) =>
            {
                try
                {
                    while (true) // Loop forever, so data will always be read
                    {
                        byteCount = process.StandardOutput.BaseStream // Access the underlying MemoryStream from the stdout of FFmpeg
                                .Read(buffer, 0, blockSize); // Read stdout into the buffer

                        if (byteCount == 0 || worker.CancellationPending) // FFmpeg did not output anything
                            break; // Break out of the while(true) loop, since there was nothing to read.

                        vClient.Send(buffer, 0, byteCount); // Send our data to Discord
                    }
                    vClient.Wait(); // Wait for the Voice Client to finish sending data, as ffMPEG may have already finished buffering out a song, and it is unsafe to return now.
                   
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }

            });

            worker.RunWorkerAsync();




            return worker;
        }
    
        
       

        private async void PlayWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;

            while (!worker.CancellationPending)
            {
                GC.Collect();
                Thread.Sleep(1000);

                foreach (KeyValuePair<ulong, Player> pair in players)
                {
                    Player player = pair.Value;

                    if (!player.Playing && player.Playlist.Count > 0 && player.AutoPlay)
                    {
                        Channel channel = client.GetServer(player.Id).GetChannel(player.ChannelId);
                                            
                        string[] playResult = await Play(channel);

                        await client.GetServer(player.Id)
                                    .FindChannels(defaultTextChannel, ChannelType.Text)
                                    .FirstOrDefault()
                                    .SendMessage(string.Format(string.Format(":arrow_forward: Now playing {0} .", playResult[0])));
                    }
                    else if (player.Playlist.Count == 0 && player.AutoPlay == true)
                        player.AutoPlay = false;
                }
            }
        }

        private void PlayWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }


    }

    public class Song
    {
        public string Title;
        public string Url;
        public string Length;
    }

    public class Player
    {
       // public IAudioClient VClient;
        public ulong Id;
        public List<Song> Playlist;
        public object ListLock;
        public ulong ChannelId;

        public bool AutoPlay;

        public bool Playing
        {           
            get { return NowPlayingWorker != null && !CompletedStop; }
        }

        public BackgroundWorker NowPlayingWorker;
        public bool CompletedStop;

        public Player()
        {
            Playlist = new List<Song>();
            ListLock = new object();
            NowPlayingWorker = null;
            CompletedStop = false;
            AutoPlay = false;
        }
    
    }
}
