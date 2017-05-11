using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Google.Apis.Customsearch.v1;
using Google.Apis.Services;
using Google.Apis.Customsearch.v1.Data;

namespace GoogleSearchAPI
{
    public class GoogleSearch
    {

        //API Key
        private const string API_KEY = "AIzaSyCVWb7MQyTH6m2l7u6iRW4oeU0qV8psdyo";
        //private const string API_KEY = "AIzaSyAt8AkrmkiLVghrcKA3lFh37R79rSG0NsE";

        //The custom search engine identifier
        private const string cx = "000412669093790997000:si9itgso00c";
        //private const string cx = "003470263288780838160:ty47piyybua";

        private CustomsearchService Service;

        public GoogleSearch()
        {           

            Service = new CustomsearchService(
                new BaseClientService.Initializer
                {
                    ApplicationName = "search",
                    ApiKey = API_KEY,
                });    
        }


        public string Search(string query)
        {
            Console.WriteLine("Executing custom search for query: {0} ...", query);

            CseResource.ListRequest listRequest = Service.Cse.List(query);
            listRequest.Cx = cx;
            listRequest.SearchType = CseResource.ListRequest.SearchTypeEnum.Image;

            Random r = new Random(1024);
            listRequest.Start = r.Next();


            Search search = listRequest.Execute();
          


            return search.Items[0].Image.ContextLink;
        }

    }
}