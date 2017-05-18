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
        private LinkedList<ulong> messageList;


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

            Array.Sort(commandDescription);

            AddDogCommand();
            AddCatCommand();
            AddNoodlesCommand();
            AddImageCommand();
            AddUserUpdateEvent();
            AddMusicCommand();
            AddCleanCommand();
            AddHelpCommand();
            AddSayCommand();


        }

        string[] commandDescription = new string[]
        {
            "=~=help                    :顯示這段訊息",
            "=~=cat                     :召喚貓咪~",
            "=~=dog                     :召喚狗狗~",
            "=~=image [keyword]         :召喚主人想召喚的(不保證正確哦)\n"+
            "                            [keyword]是主人想召喚的東西名稱",
            "=~=noodles                 :召喚花生最愛的乾麵=~=",
            "=~=clean [number]          :刪掉花生說過的話QAQ\n"+
            "                            [number]是要刪除的訊息數量，刪除最近的[number]條內花生說過的話，預設50",
            "=~=music [argument] [url]  :花生會唱歌哦\n"+
            "                            [argument] p:播放清單內第一首歌\n"+
            "                                       q:將[url]加入播放清單\n"+
            "                                       s:停止目前的歌並停止播放\n"+
            "                                       j:將花生加入主人的語音頻道\n"+
            "                                       q:將花生從語音頻道移除\n"

        };




        private void AddSayCommand()
        {
            tService.CreateCommand("~=say")
                .Parameter("string",ParameterType.Required)
                .Do(async (e) =>
            {
                await e.Channel.SendTTSMessage(e.GetArg("string"));
            });
        }


        private void AddHelpCommand()
        {

            string helpString =
                "```css\n[Help]" + Environment.NewLine + Environment.NewLine;

            for (int i = 0; i != commandDescription.Length; ++i)
                helpString += commandDescription[i] + Environment.NewLine;

            helpString += "```";



                tService.CreateCommand("~=help").Do(async (e) => 
            {
                await e.Channel.SendMessage(helpString);
            });
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

        private void AddCleanCommand()
        {

            tService.CreateCommand("~=clean")
                .Parameter("amount", ParameterType.Optional)
                .Do( async (e) =>
                {
                    int amount;
                    string str;

                    if (string.IsNullOrEmpty(str = e.GetArg("amount")) ||
                        !int.TryParse(str, out amount))
                        amount = 50;

                    Message[] messages = await e.Channel.DownloadMessages(amount);
                    List<Message> botMessages = new List<Message>();

                    foreach (Message m in messages)
                        if (m.User.Name == "花生殼")
                            botMessages.Add(m);

                    await e.Channel.DeleteMessages(botMessages.ToArray()); 

                });
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

                        string[] result = GetYoutubeInfo(data);

                        if (result.Length != 3)
                        {
                            Console.WriteLine(result[0]);
                            await channel.SendMessage(string.Format(":no_entry_sign: Error occured during conversion of url."));


                            break;
                        }
                        song.Title = result[0];
                        song.Url = result[1];
                        song.Length = result[2];
                      

                        string enqueueError = musicHandler.Enqueue(song, e.User.VoiceChannel);

                        if (enqueueError == "")
                            await channel.SendMessage(string.Format(":arrow_heading_down: Add {0} ({1}) to playing queue.", song.Title,song.Length));
                        else
                            await channel.SendMessage(":no_entry_sign: " + enqueueError);
                        break;

                    case "j":
                        await musicHandler.JoinChannel(e.User);
                        break;

                    case "l":
                        await musicHandler.LeaveChannel(e.User);
                        break;

                    case "s":                     
                        channel = e.Server.FindChannels(defaultTextChannel, ChannelType.Text).FirstOrDefault();

                        string stopError = musicHandler.Stop(e.User);

                        if (stopError == "")
                            await channel.SendMessage(string.Format(":stop_button: Stopped."));
                        else
                            await channel.SendMessage(":no_entry_sign: "+ stopError);


                        break;

                    case "p":

                        string[] playResult = await musicHandler.Play(e.User.VoiceChannel);
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

        //0: Title
        //1: Url
        //2: Length
        public static string[] GetYoutubeInfo(string httpUrl)
        {
            const string title = "--get-title";
            const string url = "--get-url";
            const string duration = "--get-duration";
            const string encoding = "--encoding cp950";
            const string fileName = "youtube-dl";


            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = fileName;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardInput = true;

            #region Set cmd

            #endregion




            string input = encoding + " " + url + " " + title + " " + duration + " " + httpUrl;

            startInfo.Arguments = input;

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            process.StartInfo = startInfo;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

                    
            string[] result = output.Split(new char[] { '\n', '\r' });


            process.WaitForExit();

            if (error == "")
                return new string[] { result[0], result[result.Length - 3], result[result.Length - 2] };
            else
                return new string[] { error };

        }


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
