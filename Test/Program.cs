using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;

using NAudio.Wave;
using System.Threading;
using NAudio.CoreAudioApi;

namespace Test
{
    class Program
    {
        static MemoryStream ms = new MemoryStream();

        static void Main(string[] args)
        {

            #region Get video stream

            string url = "https://r5---sn-3cu-u2xl.googlevideo.com/videoplayback?sparams=dur%2Cei%2Cgcr%2Cid%2Cinitcwndbps%2Cip%2Cipbits%2Citag%2Clmt%2Cmime%2Cmm%2Cmn%2Cms%2Cmv%2Cpcm2cms%2Cpl%2Cratebypass%2Crequiressl%2Csource%2Cupn%2Cexpire&ipbits=0&signature=DDDD8F446DAC786ABE1F931B754150446659924B.4EA5474A7A3476231C2CBF0655D0C78CD5A84CCD&upn=pCZc6gLnhBc&pl=18&itag=22&gcr=tw&mime=video%2Fmp4&pcm2cms=yes&mn=sn-3cu-u2xl&mm=31&ratebypass=yes&requiressl=yes&mv=m&mt=1494496882&ms=au&ei=1jYUWZXuOsqf4AKA2pLIDQ&lmt=1490687751990916&key=yt6&ip=182.235.27.171&expire=1494518583&dur=9347.378&id=o-AH5i2dIxnM7NQvCY0VC1toUAXUnAvkKCri9kkasdBcEn&initcwndbps=2422500&source=youtube";
          //  url = Temp.GetYoutubeUrl(url);


            bool stopFlag = true;

          //  new Thread(delegate (object o)
          //  {
                var response = WebRequest.Create(url).GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    byte[] buffer = new byte[65536]; // 64KB chunks
                    int read;
                    while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var pos = ms.Position;
                        ms.Position = ms.Length;
                        ms.Write(buffer, 0, read);
                        ms.Position = pos;
                   
                    }
                }

                stopFlag = false;
          //  }).Start();

           //  Pre-buffering some data to allow NAudio to start playing
         //   while (ms.Length < 65536 * 2 && stopFlag)
         //       Thread.Sleep(1000);

            ms.Position = 0;


            #endregion

         

            var OutFormat = new WaveFormat(48000, 16); // Create a new Output Format, using the spec that Discord will accept, and with the number of channels that our client supports.

      //      Mp3FileReader.FrameDecompressorBuilder builder = new Mp3FileReader.FrameDecompressorBuilder(OutFormat);


            using (WaveStream blockAlignedStream = new BlockAlignReductionStream(WaveFormatConversionStream.CreatePcmStream(new StreamMediaFoundationReader(ms))))
            {
                using (WaveOut waveOut = new WaveOut(WaveCallbackInfo.FunctionCallback()))
                {
                    waveOut.Init(blockAlignedStream);
                    waveOut.Play();

                  
                    //waveOut.OutputWaveFormat = OutFormat;

                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
            }
        }   
    }

    class Temp
    {
        public static string GetYoutubeUrl(string httpUrl)
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
