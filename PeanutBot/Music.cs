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

using NAudio.Wave;
using System.ComponentModel;
using Discord.Commands;

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
            #region Get video stream

            MemoryStream ms = new MemoryStream();

            bool running = true;
            object msLock = new object();


            //new Thread(delegate (object o)
            //{

            try
            {
                var response = WebRequest.Create(url).GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    byte[] gettingBuffer = new byte[65536]; // 64KB chunks
                    int read;
                    while ((read = stream.Read(gettingBuffer, 0, gettingBuffer.Length)) > 0)
                    {
                        lock (msLock)
                        {
                            // var pos = ms.Position;
                            //   ms.Position = ms.Length;
                            ms.Write(gettingBuffer, 0, read);
                            //   ms.Position = pos;
                        }
                    }
                }

                running = false;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }                
           // }).Start();

            //  Pre-buffering some data to allow NAudio to start playing
         //   while (ms.Length < 65536 * 10 && running)
         //       Thread.Sleep(1000);



            ms.Position = 0;
            #endregion



            var channelCount = client.GetService<AudioService>().Config.Channels; // Get the number of AudioChannels our AudioService has been configured to use.
            var OutFormat = new WaveFormat(48000, 16, channelCount); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.

            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;

            using (var StreamFileReader = new StreamMediaFoundationReader(ms)) // Create a new Disposable MP3FileReader, to read audio from the filePath parameter
            using (var resampler = new MediaFoundationResampler(StreamFileReader, OutFormat)) // Create a Disposable Resampler, which will convert the read MP3 data to PCM, using our Output Format
            {

                resampler.ResamplerQuality = 60; // Set the quality of the resampler to 60, the highest quality
                int blockSize = OutFormat.AverageBytesPerSecond / 50; // Establish the size of our AudioBuffer
                byte[] buffer = new byte[blockSize];
                int byteCount = 0;
                 
                                               
                worker.DoWork += new DoWorkEventHandler((object sender, DoWorkEventArgs e) =>
                {
                    try
                    {
                        do
                        {
                            lock (msLock)
                                byteCount = resampler.Read(buffer, 0, blockSize);   // Read audio into our buffer, and keep a loop open while data is present

                            if (byteCount < blockSize)
                            {
                                // Incomplete Frame
                                for (int i = byteCount; i < blockSize; i++)
                                    buffer[i] = 0;
                            }

                            vClient.Send(buffer, 0, blockSize); // Send the buffer to Discord


                        }
                        while (!worker.CancellationPending && (byteCount > 0 || running));

                        ms.Close();
                        ms.Dispose();
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                });

                worker.RunWorkerAsync();

              
              
            }
            
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
            get { return NowPlayingWorker != null && NowPlayingWorker.IsBusy; }
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
