using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

using Discord.Commands;
using Discord;
using Discord.Audio;
using System.Diagnostics;

namespace PeanutBot
{
    public class Text
    {
        
        private CommandService tService;
        private bool initialized;
        private Music musicHandler;

        const string defaultTextChannel = "note";

        public Text(DiscordClient client, Music musicHandler)
        {
            tService = client.GetService<CommandService>();
            this.musicHandler = musicHandler;

            initialized = false;
        }


        public void Initialize()
        {
            if (initialized) return;

            AddDogCommand();
            AddCatCommand();
            AddNoodlesCommand();
            AddImageCommand();
            AddUserUpdateEvent();
            AddMusicCommand();

        }

        private void AddCatCommand()
        {
            tService.CreateCommand("~=cat").Do(async (e) => { await e.Channel.SendMessage(Picture.GetCat()); });
        }

        private void AddDogCommand()
        {
            tService.CreateCommand("~=dog").Do(async (e) => { await e.Channel.SendMessage(Picture.GetDog()); });
        }

        private void AddNoodlesCommand()
        {
            tService.CreateCommand("~=noodles")
              .Do(async (e) => { await e.Channel.SendMessage(Picture.GetImage("乾麵")); });
        }

        private void AddImageCommand()
        {
            tService.CreateCommand("~=image")
                .Parameter("searchkey", ParameterType.Required)
                .Do(async (e) => { await e.Channel.SendMessage(Picture.GetImage(e.GetArg("searchkey"))); });
        }

        private void AddUserUpdateEvent()
        {
            
            tService.Client.UserUpdated += async (s, e) =>
            {
                var channel = e.Server.FindChannels(defaultTextChannel, ChannelType.Text).FirstOrDefault();
                User usr = e.After;

                string name = string.IsNullOrEmpty(usr.Nickname) ? usr.Name : usr.Nickname;

                if (e.Before.Status.Value == "idle" && e.After.Status.Value == "online")
                    await channel.SendMessage(string.Format("{0} 回來了=~=", name));
                else if (e.Before.Status.Value == "offline" && e.After.Status.Value == "online")
                    await channel.SendMessage(string.Format("{0} 上線了=~=", name));
                else if ((e.Before.Status.Value == "online" || e.Before.Status.Value == "online") && e.After.Status.Value == "offline")
                    await channel.SendMessage(string.Format("{0} 下線了=~=", name));
                else if (e.Before.Status.Value == "online" && e.After.Status.Value == "idle")
                    await channel.SendMessage(string.Format("{0} 閒置了=~=", name));
            };
        }

        private void AddMusicCommand()
        {
            tService.CreateCommand("~=music")
            .Parameter("parameter", ParameterType.Required)
            .Parameter("data", ParameterType.Optional)
            .Do( async (e) =>
            {
                string param = e.GetArg("parameter");
                string data = e.GetArg("data");
                var channel = e.Server.FindChannels(defaultTextChannel, ChannelType.Text).FirstOrDefault();

                switch (param)
                {
                    

                    case "q":
                        Song song = new Song();
                        song.Url = GetYoutubeUrl(data);
                        song.Title = GetYoutubeTitle(data);

                        string enqueueError = musicHandler.Enqueue(song, e.User.VoiceChannel);

                        if (enqueueError == "")
                            await channel.SendMessage(string.Format(":arrow_heading_down: Add {0} to playing queue.", song.Title));
                        else
                            await channel.SendMessage(":no_entry_sign: " + enqueueError);
                        break;

                    case "j":
                       await musicHandler.JoinChannel(e.User.VoiceChannel);
                        break;

                    case "l":
                        await musicHandler.LeaveChannel(e.User.VoiceChannel);
                        break;

                    case "s":                     
                        channel = e.Server.FindChannels(defaultTextChannel, ChannelType.Text).FirstOrDefault();

                        string stopError = musicHandler.Stop(e.User.VoiceChannel);

                        if (stopError == "")
                            await channel.SendMessage(string.Format(":stop_button: Stopped."));
                        else
                            await channel.SendMessage(":no_entry_sign: "+ stopError);


                        break;

                    case "p":
                    
                        string[] playResult = musicHandler.Play(e.User.VoiceChannel);
                        string title = playResult[0];
                        string playError = playResult[1];
                    
                    
                        channel = e.Server.FindChannels(defaultTextChannel, ChannelType.Text).FirstOrDefault();
                    
                        if (playError == "")
                            await channel.SendMessage(string.Format(":arrow_forward: Now playing {0} .", title));
                        else
                            await channel.SendMessage(":no_entry_sign: " + playError);
                    
                        break;
                    default:
                        break;
                }
                




            
            });
        }


        //0: Url
        //1: Title
        //2: Length
      //  private string[] GetYoutubeInfo(string httpUrl)
      //  {
      //      Process process = new Process();
      //      ProcessStartInfo startInfo = new ProcessStartInfo();
      //      startInfo.FileName = "youtube-dl";
      //
      //
      //
      //
      //  }


        private string GetYoutubeTitle(string httpUrl)
        {



            string title = "";
            
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(httpUrl);
            req.Method = "GET";

            using (WebResponse wr = req.GetResponse())
            using (Stream st = wr.GetResponseStream())
            using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
            {
                const string sourceName = "\"title\":\"";
                int offset = sourceName.Length;

                string str = sr.ReadToEnd();

                int index = str.IndexOf(sourceName) + offset;

                string tmpStr = "";



                while (str[index] != '"' && index < str.Length)
                    if (str[index] != '\\')
                        tmpStr += str[index++];
                    else
                        index++;

                title = tmpStr;

                sr.Close();
                st.Close();
                wr.Close();
            }

            return title;
        }




        private string GetYoutubeUrl(string httpUrl)
        {           
            const string changeWebside = "https://uploadbeta.com/api/video/?cached&video=";
            string url = "";

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(changeWebside + httpUrl);
            req.Method = "GET";

            using (WebResponse wr = req.GetResponse())
            using (Stream st = wr.GetResponseStream())
            using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
            {
                const string sourceName = "url";
                const int offset = 6;

                string str = sr.ReadToEnd();

                int index = str.IndexOf(sourceName) + offset;

                string tmpStr = "";



                while (str[index] != '"' && index < str.Length)
                    if (str[index] != '\\')
                        tmpStr += str[index++];
                    else
                        index++;

                url = tmpStr;

                sr.Close();
                st.Close();
                wr.Close();
            }

            return url;
        }

    }
}
