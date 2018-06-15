using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SmartVideoGallery.Models;

namespace SmartVideoGallery
{
    public class VideoIndexingService:IDisposable
    {

        private string _apiURL = string.Empty;
        private string _accountId = string.Empty;
        private string _apiKey = string.Empty;
        private string _location = string.Empty;
        private string _cosmosDBURI = string.Empty;
        private string _cosmosKey = string.Empty;
        private string _videoAccessToken = string.Empty;
        private DateTime _videoAccessCreateTime;
        private DateTime _accountAccessCreateTime;
        private string _accountAccessToken = string.Empty;
        private string _cosmosDBName = string.Empty;
        private string _cosmosDBCollectionName = string.Empty;
        private HttpClient _client;
        private HttpClientHandler _handler;
        

        public VideoIndexingService(string apiURL,
                                    string accountId,
                                    string apiKey,
                                    string location,
                                    string cosmosDbUri,
                                    string cosmosKey,
                                    string cosmosDBName,
                                    string cosmosDBCollectionName)
        {
            this._apiURL = apiURL;
            this._accountId = accountId;
            this._apiKey = apiKey;
            this._location = location;
            this._cosmosDBURI = cosmosDbUri;
            this._cosmosKey = cosmosKey;
            this._cosmosDBName = cosmosDBName;
            this._cosmosDBCollectionName = cosmosDBCollectionName;

            this. _handler = new HttpClientHandler();
            this._handler.AllowAutoRedirect = false;
            this._client = new HttpClient(_handler);

        }


        public string GetAccountAccessToken()
        {
            
            if ((this._accountAccessToken.Length == 0)||(IsTokenExpired(this._accountAccessCreateTime) == true))
            {
                System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

                // create the http client
                //var handler = new HttpClientHandler();
                //handler.AllowAutoRedirect = false;
                //var client = new HttpClient(handler);
                this._client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._apiKey);

                // obtain account access token
                var accountAccessTokenRequestResult = this._client.GetAsync($"{this._apiURL}/auth/{this._location}/Accounts/{this._accountId}/AccessToken?allowEdit=true").Result;
                var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                this._client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                this._accountAccessCreateTime = System.DateTime.Now;
                this._accountAccessToken = accountAccessToken.ToString();
     
            }
         
            return this._accountAccessToken;
        }
 
        public string GetVideoAccessToken(string videoId){

            if ((this._videoAccessToken.Length == 0)|| (IsTokenExpired(this._videoAccessCreateTime) == true))
            {
                // sometimes you must execute this 
                // more than once as the video
                // takes time to be accessile
                bool tryAgainFlashed = false;
                while (true)
                {

                    System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

                    // create the http client
                    //var handler = new HttpClientHandler();
                    //handler.AllowAutoRedirect = false;
                    //var client = new HttpClient(handler);

                    this._client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", this._apiKey);


                    var videoTokenRequestResult = this._client.GetAsync($"{this._apiURL}/auth/{this._location}/Accounts/{this._accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
                    var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

                    this._client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

                    if (videoAccessToken.ToString().IndexOf("BREAKDOWN_NOT_FOUND") == -1)
                    {
                        this._videoAccessCreateTime = System.DateTime.Now;
                        this._videoAccessToken = videoAccessToken.ToString();
                        if(tryAgainFlashed)
                        {
                            Console.WriteLine("Video access token retrieved!");
                        }
                        break;
                    }
                    else
                    {
                        Thread.Sleep(10000);
                        Console.WriteLine("Video access token not ready yet...will try again in 10 seconds.");
                        tryAgainFlashed = true;
                    }
                    
                }
            }

            return this._videoAccessToken;
        }


        public string UploadVideo(string videoName, string videoURL)
        {
            string videoId = string.Empty;

            string accountAccessToken = GetAccountAccessToken();
            accountAccessToken = GetAccountAccessToken();
            
            // CREATE HTTP CLIENT
            //var handler = new HttpClientHandler();
            //var client = new HttpClient(handler);

            //UPLOAD VIDEO
            Console.WriteLine("Uploading...");
            var content = new MultipartFormDataContent();
            var uploadRequestResult = this._client.PostAsync($"{this._apiURL}/{this._location}/Accounts/{this._accountId}/Videos?accessToken={accountAccessToken}&name={videoName}& description=somethis._description&privacy=private&partition=somethis._partition&videoUrl={videoURL}", content).Result;
            var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;
            Console.WriteLine("Upload complete.");

            var videoIdDesearialized = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
            videoId = videoIdDesearialized.Value;

            // OBTAIN VIDEO ACCESS TOKEN           
            string videoAccessToken = GetVideoAccessToken(videoId);

            DateTime kickOffTime = System.DateTime.Now;

            // WAIT FOR THE VIDEO INDEX TO FINISH
            while (true)
            {
                Thread.Sleep(10000);

                var videoGetIndexRequestResult = this._client.GetAsync($"{this._apiURL}/{this._location}/Accounts/{this._accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
                var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

                var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                var diffInSeconds = (System.DateTime.Now - kickOffTime).TotalSeconds;
                if (processingState != null)
                {
                   
                    Console.WriteLine("Indexing..." + diffInSeconds.ToString().Substring(0, diffInSeconds.ToString().IndexOf(".")) + " seconds elapsed.");

                    // JOB IS FINISHED
                    if (processingState != "Uploaded" && processingState != "Processing")
                    {
                        Console.WriteLine("Indexing complete.");
                        break;
                    }
                }
                else
                {
                    throw new Exception("Unable to determine processing state.  Please check video index library for any corrupt files.");
                }
            }


            return videoId;

        }


        public Transcript GetTranscript(string videoId, string language)
        {

            Transcript returnTranscript = new Transcript();
            returnTranscript.Language = language;

            string accountAccessToken = GetAccountAccessToken();
            var uri = this._apiURL + "/" + this._location + "trial/Accounts/" + this._accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=" + language;

            //var handler = new HttpClientHandler();
            //handler.AllowAutoRedirect = false;

            //var client = new HttpClient(handler);

            var task = this._client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {

                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();

                  jsonString.Wait();

                  var deserializedJson = JObject.Parse(jsonString.Result);

                  var transcripts = deserializedJson["videos"][0]["insights"]["transcript"];
                

                  int TranscriptOrderCount = 0;
                  foreach (var transcript in transcripts)
                  {
                      TranscriptLine NewLine = new TranscriptLine();

                      string transcriptSpeakerName = string.Empty;
                      try
                      {
                          int transcriptionSpeakerId = Convert.ToInt32(transcript["speakerId"].ToString());
                          transcriptSpeakerName = deserializedJson["videos"][0]["insights"]["speakers"][transcriptionSpeakerId]["name"].ToString();
                      }
                      catch (Exception NoSpeakerFound)
                      {
                          NoSpeakerFound.Data.Clear();
                          transcriptSpeakerName = "Speaker not found";
                      }

                      NewLine.Speaker = transcriptSpeakerName;
                      NewLine.Order = TranscriptOrderCount;
                      NewLine.Line = transcript["text"].ToString();
                      TranscriptOrderCount++;
                      returnTranscript.Lines.Add(NewLine);


                  }
               });

              task.Wait();

              return returnTranscript;

              }

        public List<Speaker> GetSpeakersAndFrequencies(string videoId, string language)
        {

            List<Speaker> returnSpeakers = new List<Speaker>();
            string accountAccessToken = GetAccountAccessToken();
            var uri = this._apiURL + "/" + this._location + "trial/Accounts/" + this._accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=" + language;

            //var handler = new HttpClientHandler();
            //handler.AllowAutoRedirect = false;

            //var client = new HttpClient(handler);

            var task = this._client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();

                  jsonString.Wait();

                  var deserializedJson = JObject.Parse(jsonString.Result);

                  var speakers = deserializedJson["videos"][0]["insights"]["speakers"];

                  foreach (var speaker in speakers)
                  {

                      Speaker NewSpeaker = new Speaker();

                      string speakerName = string.Empty;
                      speakerName = speaker["name"].ToString();

                      var frequencyInstances = speaker["instances"];

                      foreach (var instance in frequencyInstances)
                      {
                          SpeakerFrequency NewFrequency = new SpeakerFrequency();


                          NewFrequency.BeginTime = instance["start"].ToString();
                          NewFrequency.EndTime = instance["end"].ToString();

                          NewSpeaker.Frequencies.Add(NewFrequency);
                      }

                      returnSpeakers.Add(NewSpeaker);
                  }


              });

            task.Wait();


            return returnSpeakers;

        }


        public List<string> GetKeyWords(string videoId, string language)
        {


            List<string> keyWordsReturned = new List<string>();
            string accountAccessToken = GetAccountAccessToken();
            var uri = this._apiURL + "/" + this._location + "trial/Accounts/" + this._accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=" + language;

            //var handler = new HttpClientHandler();
            //handler.AllowAutoRedirect = false;

            //var client = new HttpClient(handler);

            var task = this._client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();

                  jsonString.Wait();

                  var deserializedJson = JObject.Parse(jsonString.Result);

                  var keyWords = deserializedJson["summarizedInsights"]["keywords"];

                  foreach (var keyword in keyWords)
                  {
                      keyWordsReturned.Add(keyword["name"].ToString());

                  }

              });

            task.Wait();


            return keyWordsReturned;

        }


        public List<string> GetBrands(string videoId, string language)
        {

            List<string> brandsReturned = new List<string>();
            string accountAccessToken = GetAccountAccessToken();
            var uri = this._apiURL + "/" + this._location + "trial/Accounts/" + this._accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=" + language;

            //var handler = new HttpClientHandler();
            //handler.AllowAutoRedirect = false;

            //var client = new HttpClient(handler);

            var task = this._client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();

                  jsonString.Wait();

                  var deserializedJson = JObject.Parse(jsonString.Result);

                  var brands = deserializedJson["summarizedInsights"]["brands"];

                  foreach (var brand in brands)
                  {

                      brandsReturned.Add(brand["name"].ToString());
                  }

              });

             task.Wait();


             return brandsReturned;

        }


      


        public List<Face> GetFaces(string videoId, string language)
        {

            List<Face> facesReturned = new List<Face>();
            string accountAccessToken = GetAccountAccessToken();
            var uri = this._apiURL + "/" + this._location + "trial/Accounts/" + this._accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=" + language;

            //var handler = new HttpClientHandler();
            //handler.AllowAutoRedirect = false;

            //var client = new HttpClient(handler);

            var task = this._client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {
                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();

                  jsonString.Wait();

                  var deserializedJson = JObject.Parse(jsonString.Result);

                  var faces = deserializedJson["summarizedInsights"]["faces"];

                  foreach (var face in faces)
                  {
                      Face NewFace = new Face();
                      string faceName = face["name"].ToString();
                      bool isKnown = true;

                      try
                      {
                          if (faceName.Substring(0, 9) == "Unknown #")
                          {
                              isKnown = false;
                          }
                      }
                      catch (Exception UnknownCompareException)
                      {
                          UnknownCompareException.Data.Clear();
                          isKnown = true;
                      }

                      NewFace.Id = face["id"].ToString();
                      NewFace.Name = faceName;
                      NewFace.IsKnown = isKnown;


                      facesReturned.Add(NewFace);
                  }

              });

            task.Wait();


            return facesReturned;

        }


        public async Task AddVideoToCosmosDB(Video videoObject)
        {

            using (DocumentClient documentClient = new DocumentClient(new Uri(this._cosmosDBURI), this._cosmosKey))
            {

                await documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = this._cosmosDBName });

                await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(this._cosmosDBName), new DocumentCollection { Id = this._cosmosDBCollectionName });


                try
                {
                    await documentClient.ReadDocumentAsync(UriFactory.CreateDocumentUri(this._cosmosDBName, this._cosmosDBCollectionName, videoObject.Id));

                }
                catch (DocumentClientException de)
                {
                    if (de.StatusCode == HttpStatusCode.NotFound)
                    {
                        await documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(this._cosmosDBName, this._cosmosDBCollectionName), videoObject);

                    }

                }
            }
        }


        public List<Video>  ExecuteSimpleQuery(string videoName)
        {

            List<Video> returnVideos = new List<Video>();

            using (DocumentClient documentClient = new DocumentClient(new Uri(this._cosmosDBURI), this._cosmosKey))
            {
                // Set some common query options
                FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

                // QUERY FOR VIDEOS BY NAME
                IQueryable<Video> videoQuery = documentClient.CreateDocumentQuery<Video>(
                        UriFactory.CreateDocumentCollectionUri(this._cosmosDBName, this._cosmosDBCollectionName), queryOptions)
                        .Where(v => v.Name == videoName);

                // The query is executed synchronously here, but can also be executed asynchronously via the IDocumentQuery<T> interface
                Console.WriteLine("Running LINQ query...");
                foreach (Video video in videoQuery)
                {
                    returnVideos.Add(video);
                }


            }

            return returnVideos;
        }


        private static bool IsTokenExpired(DateTime createTime)
        {
            bool returnValue = true;
            try
            {
                TimeSpan timeSpan = new TimeSpan();
                timeSpan = (System.DateTime.Now - createTime);

                if(timeSpan.Minutes < 60)
                {
                    returnValue = false;
                }

            }
            catch (Exception CompareException)
            {
                CompareException.Data.Clear();
                returnValue = true;
            }

            return returnValue;
        }

        public void Dispose()
        {

            this._handler = null;
            this._client = null;
        }
    }
}
