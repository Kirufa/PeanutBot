using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PeanutBot
{
    public class Picture
    {
        public static string GetDog()
        {
            string result = "";

            const string dogWebside = "https://random.dog";

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(dogWebside);
            req.Method = "GET";
            using (WebResponse wr = req.GetResponse())
            using (Stream st = wr.GetResponseStream())
            using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
            {
                const string sourceName = "src=";
                string str = sr.ReadToEnd();

                int index = str.IndexOf(sourceName) + sourceName.Length + 1;

                string fullAddress = dogWebside + '/';

                if (index >= 0)
                {
                    while (str[index] != '"' && index < str.Length)
                        fullAddress += str[index++];

                    if (index < str.Length)
                        result = fullAddress;
                }


                sr.Close();
                st.Close();
                wr.Close();
            }

            return result;
        }

        public static string GetCat()
        {
            string result = "";

            const string catWebside = "https://random.cat";

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(catWebside);
            req.Method = "GET";
            using (WebResponse wr = req.GetResponse())
            using (Stream st = wr.GetResponseStream())
            using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
            {
                const string sourceName = "img";
                string str = sr.ReadToEnd();

                int index0 = str.IndexOf(sourceName) + sourceName.Length + 1;
                int index1 = str.IndexOf(sourceName, index0) + sourceName.Length + 1;
                int index2 = str.IndexOf(sourceName, index1) + sourceName.Length + 6;



                string fullAddress = catWebside + '/';

                if (index2 >= 0)
                {
                    while (str[index2] != '"' && index2 < str.Length)
                        fullAddress += str[index2++];

                    if (index2 < str.Length)
                        result = fullAddress;
                }


                sr.Close();
                st.Close();
                wr.Close();
            }

            return result;
        }

        public static string GetImage(string title)
        {
            string result;

            Random r = new Random((int)DateTime.Now.Ticks);


            string url = "https://images.search.yahoo.com/search/images?p=" + title;

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
            req.Method = "GET";
            using (WebResponse wr = req.GetResponse())
            using (Stream st = wr.GetResponseStream())
            using (StreamReader sr = new StreamReader(st, Encoding.UTF8))
            {
                const string sourceName = "iurl";
                const int offset = 7;

                string str = sr.ReadToEnd();

                int _index = str.IndexOf(sourceName) + sourceName.Length + 1;
                int index = str.IndexOf(sourceName, _index);

                List<string> results = new List<string>();



                while (index >= 0)
                {
                    index += offset;

                    string tmpStr = "";

                    while (str[index] != '"')
                        if (str[index] != '\\')
                            tmpStr += str[index++];
                        else
                            index++;

                    index = str.IndexOf(sourceName, index);
                    results.Add(tmpStr);
                }



                int count = r.Next() % results.Count;

                result = results[count];

                sr.Close();
                st.Close();
                wr.Close();
            }

            return result;
        }
    }
}
