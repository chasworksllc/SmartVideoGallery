using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SmartVideoGallery
{
    class Program
    {

        /// <summary>
        /// FEEL FREE TO USE THIS TEST ACCOUNT
        /// UserId:  saulgoodmanvideo@gmail.com
        /// Password: b3tterc@llsaul! 
        /// Video Indexer Summary Page:  https://www.videoindexer.ai/accounts/f384ace5-878e-4c68-812c-3fed46ca58c9/videos/a61635115f/
        /// </summary>

        private static string _apiUrl = "https://api.videoindexer.ai";
        private static string _accountId = "f384ace5-878e-4c68-812c-3fed46ca58c9";
        private static string _apiKey = "935d8e4454cd46538db3dce39efbcd74";
        private static string _location = "trial";
        static void Main(string[] args)
        {


            
            //string videoId  =  UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/BetterCallSaul01.mp4", "BetterCallSaul-Video01");
            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/BetterCallSaul02.mp4", "BetterCallSaul-Video02");
            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/BetterCallSaul03.mp4", "BetterCallSaul-Video03");
            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/BetterCallSaul04.mp4", "BetterCallSaul-Video04");

            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/ShaneDawson.mp4", "ShaneDawson");
            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/video01.mp4", "HathawaySpeech");
            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/video02.mp4", "BowieCharlieRose");
            //string videoId = UploadAndIndexVideo("https://chasworksllcvideotest.blob.core.windows.net/videos/video03.mp4", "GeorgeBenson");

            string videoId = "a61635115f";
            string accessToken = GetAccountAccessToken();

           
            Console.WriteLine("Writing transcript...");
            GetTranscript(videoId, accessToken, "English");
            Console.WriteLine("");

            Console.WriteLine("Writing transcript spanish...");
            GetTranscript(videoId, accessToken, "Spanish");
            Console.WriteLine("");


            Console.WriteLine("Get frequency of speakers");
            GetFrequencyOfSpeakers(videoId, accessToken);
            Console.WriteLine("");


            Console.WriteLine("Writing faces of known people...");
            GetFaces(videoId, accessToken);
            Console.WriteLine("");


            Console.WriteLine("Writing keywords...");
            GetKeywords(videoId, accessToken);
            Console.WriteLine("");


            Console.WriteLine("Writing objects/brands...");
            GetBrands(videoId, accessToken);
            Console.WriteLine("");

            Console.ReadKey();
        }

        public static string UploadVideoOriginal(string videoUrl,
            string videoName,
            string location)
        {
            var apiUrl = "https://api.videoindexer.ai";
            var accountId = _accountId;
            //var location = "trial";
            var apiKey = _apiKey;

            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            // obtain account access token
            var accountAccessTokenRequestResult = client.GetAsync($"{apiUrl}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true").Result;
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // upload a video
            var content = new MultipartFormDataContent();
            Console.WriteLine("Uploading...");

            var uploadRequestResult = client.PostAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name={videoName}& description=some_description&privacy=private&partition=some_partition&videoUrl={videoUrl}", content).Result;
            var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

            // get the video id from the upload result
            var videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
            Console.WriteLine("Uploaded");


            // obtain video access token            
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            var videoTokenRequestResult = client.GetAsync($"{apiUrl}/auth/{location}/Accounts/{accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
            var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // wait for the video index to finish
            while (true)
            {
                Thread.Sleep(10000);

                var videoGetIndexRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                Console.WriteLine("Indexing...");

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing")
                {
                    Console.WriteLine("Indexing complete.");
                    break;
                }
            }

            // search for the video
            var searchRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/Search?accessToken={accountAccessToken}&id={videoId}").Result;
            var searchResult = searchRequestResult.Content.ReadAsStringAsync().Result;
            Console.WriteLine("");
            Console.WriteLine("Search:");
            Console.WriteLine(searchResult);

            // get insights widget url
            var insightsWidgetRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/InsightsWidget?accessToken={videoAccessToken}&widgetType=Keywords&allowEdit=true").Result;
            var insightsWidgetLink = insightsWidgetRequestResult.Headers.Location;
            Console.WriteLine("Insights Widget url:");
            Console.WriteLine(insightsWidgetLink);

            // get player widget url
            var playerWidgetRequestResult = client.GetAsync($"{apiUrl}/{location}/Accounts/{accountId}/Videos/{videoId}/PlayerWidget?accessToken={videoAccessToken}").Result;
            var playerWidgetLink = playerWidgetRequestResult.Headers.Location;
            Console.WriteLine("");
            Console.WriteLine("Player Widget url:");
            Console.WriteLine(playerWidgetLink);

            return videoId.Value;
        }


        public static string UploadAndIndexVideo(string videoUrl,
                string videoName)
        {

            string ReturnVideoId = string.Empty;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

            // obtain account access token
            var accountAccessTokenRequestResult = client.GetAsync($"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true").Result;
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // upload a video
            var content = new MultipartFormDataContent();
            Console.WriteLine("Uploading...");

            var uploadRequestResult = client.PostAsync($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos?accessToken={accountAccessToken}&name={videoName}& description=some_description&privacy=private&partition=some_partition&videoUrl={videoUrl}", content).Result;
            var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

            Console.WriteLine("Uploaded");

            var videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
            ReturnVideoId = videoId.Value;

            // obtain video access token            
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
            var videoTokenRequestResult = client.GetAsync($"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/Videos/{ReturnVideoId}/AccessToken?allowEdit=true").Result;
            var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");


            DateTime kickOffTime = System.DateTime.Now;
            // wait for the video index to finish
            while (true)
            {
                Thread.Sleep(10000);

                var videoGetIndexRequestResult = client.GetAsync($"{_apiUrl}/{_location}/Accounts/{_accountId}/Videos/{ReturnVideoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                var diffInSeconds = (System.DateTime.Now - kickOffTime).TotalSeconds;


                Console.WriteLine("Indexing..." + diffInSeconds.ToString().Substring(0, diffInSeconds.ToString().IndexOf(".")) + " seconds elapsed.");

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing")
                {
                    Console.WriteLine("Indexing complete.");
                    break;
                }
            }



            return ReturnVideoId;
        }

        public static string GetFaces(string videoId, string accessToken)
        {


            var client = new HttpClient();
            var uri = _apiUrl + "/" + _location + "trial/Accounts/" + _accountId + "/Videos/" + videoId + "/Index?accessToken=" + accessToken + "&language=English";


            var task = client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  var deserializedJson = JObject.Parse(jsonString.Result);
                  var faces = deserializedJson["summarizedInsights"]["faces"];

                  foreach (var face in faces)
                  {
                      string NameOfFace = face["name"].ToString();
                      bool IsKnown = true;

                      try
                      {
                          if(NameOfFace.Substring(0,9) == "Unknown #")
                          {
                              IsKnown = false;
                          }
                      }
                      catch(Exception UnknownCompareException)
                      {
                          UnknownCompareException.Data.Clear();
                          IsKnown = true;
                      }

                      if (IsKnown)
                      {
                          Console.WriteLine(face["name"].ToString());
                      }

                  }

              });
            task.Wait();


            return "";
        }


        public static string GetKeywords(string videoId, string accessToken)
        {


            var client = new HttpClient();
            var uri = _apiUrl + "/" + _location + "trial/Accounts/" + _accountId + "/Videos/" + videoId + "/Index?accessToken=" + accessToken + "&language=English";


            var task = client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  var deserializedJson = JObject.Parse(jsonString.Result);
                  var keyWords = deserializedJson["summarizedInsights"]["keywords"];

                  foreach (var keyword in keyWords)
                  {
                   
                          Console.WriteLine(keyword["name"].ToString());


                  }

              });
            task.Wait();


            return "";
        }


        public static string GetBrands(string videoId, string accessToken)
        {


            var client = new HttpClient();
            var uri = _apiUrl + "/" + _location + "trial/Accounts/" + _accountId + "/Videos/" + videoId + "/Index?accessToken=" + accessToken + "&language=English";


            var task = client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  var deserializedJson = JObject.Parse(jsonString.Result);
                  var brands = deserializedJson["summarizedInsights"]["brands"];

                  foreach (var brand in brands)
                  {

                      Console.WriteLine(brand["name"].ToString());


                  }

              });
            task.Wait();


            return "";
        }

        public static string GetTranscript(string videoId, string accessToken, string language)
        {


            var client = new HttpClient();
            var uri = _apiUrl + "/" + _location + "trial/Accounts/" + _accountId + "/Videos/" + videoId + "/Index?accessToken=" + accessToken + "&language=" + language;


            var task = client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  var deserializedJson = JObject.Parse(jsonString.Result);
                  
                  // there will only be one video since this is by id
                  // so take the first one

                  var transcripts = deserializedJson["videos"][0]["insights"]["transcript"];

                  foreach (var transcript in transcripts)
                  {

                      string SpeakerName = string.Empty;
                      try
                      {
                          int SpeakerId = Convert.ToInt32(transcript["speakerId"].ToString());
                          SpeakerName = deserializedJson["videos"][0]["insights"]["speakers"][SpeakerId]["name"].ToString();
                      }
                      catch(Exception NoSpeakerFound)
                      {
                          NoSpeakerFound.Data.Clear();
                          SpeakerName = "Speaker not found";
                      }
                      Console.WriteLine(SpeakerName + ":  " + transcript["text"].ToString());
                  }

              });
            task.Wait();


            return "";
        }


        public static string GetFrequencyOfSpeakers(string videoId, string accessToken)
        {


            var client = new HttpClient();
            var uri = _apiUrl + "/" + _location + "trial/Accounts/" + _accountId + "/Videos/" + videoId + "/Index?accessToken=" + accessToken + "&language=English";


            var task = client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  var deserializedJson = JObject.Parse(jsonString.Result);

                  // there will only be one video since this is by id
                  // so take the first one

                  var speakers = deserializedJson["videos"][0]["insights"]["speakers"];

                  foreach (var speaker in speakers)
                  {

                      string SpeakerName = string.Empty;
                      SpeakerName = speaker["name"].ToString();

                      Console.WriteLine("Speaking moments for " + SpeakerName);

                      var instances = speaker["instances"];

                      foreach (var instance in instances)
                      {
                          var StartTime = instance["start"].ToString();
                          var EndTime = instance["end"].ToString();

                          Console.WriteLine(SpeakerName + " speaks from [" + StartTime + "] to [" + EndTime + "]");
                      }

                    
                  }

              });
            task.Wait();


            return "";
        }





        #region "GetTokens"

        public static string GetAccountAccessToken()
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

            // obtain account access token
            var accountAccessTokenRequestResult = client.GetAsync($"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true").Result;
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");


            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            return accountAccessToken.ToString();

        }


        public static string GetVideoAccessToken(string videoId)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);

       
            var videoTokenRequestResult = client.GetAsync($"{_apiUrl}/auth/{_location}/Accounts/{_accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
            var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            return videoAccessToken.ToString();

        }
        #endregion


    }
}
