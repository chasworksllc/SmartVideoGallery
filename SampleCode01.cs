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
    public class SampleCode01
    {
        // WRITING THIS EXAMPLE IN ONE LONG FUNCTION TO SHOW 
        // ALL POSSIBILITIES IN ONE LONG CHAIN

        public static async Task UploadIndexVideo(string videoURL, 
                            string videoName)
        {
            string apiURL = "https://api.videoindexer.ai";
            string accountId = "f384ace5-878e-4c68-812c-3fed46ca58c9";
            string apiKey = "935d8e4454cd46538db3dce39efbcd74";
            string location = "trial";
            string cosmosDBURI = "https://chasvideotestdb.documents.azure.com:443/";
            string cosmosKey = "dic6xfvo6LFankKywhxtfu8rq7wzpMjvpgujANFLMg65ZXkqkIDqdCxeXQsiRKJ3ekl7JKMhXuSZA3Ube8ZwCw==";

            string videoId = string.Empty;



            Video VideoObject = new Video();
            VideoObject.Name = videoName;

            // OBTAIN ACCESS TOKEN
            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // CREATE HTTP CLIENT
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);

            // obtain account access token
            var accountAccessTokenRequestResult = client.GetAsync($"{apiURL}/auth/{location}/Accounts/{accountId}/AccessToken?allowEdit=true").Result;
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");

            // UPLOAD VIDEO
            //var content = new MultipartFormDataContent();
            //Console.WriteLine("Uploading...");

            //var uploadRequestResult = client.PostAsync($"{apiURL}/{location}/Accounts/{accountId}/Videos?accessToken={accountAccessToken}&name={videoName}& description=some_description&privacy=private&partition=some_partition&videoUrl={videoURL}", content).Result;
            //var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

            //Console.WriteLine("Uploaded");

            //var videoIdDesearialized = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];
            //videoId = videoIdDesearialized.Value;

            //// OBTAIN VIDEO ACCESS TOKEN           
            //client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", apiKey);
            //var videoTokenRequestResult = client.GetAsync($"{apiURL}/auth/{location}/Accounts/{accountId}/Videos/{videoId}/AccessToken?allowEdit=true").Result;
            //var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            //client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");


            //DateTime kickOffTime = System.DateTime.Now;

            //// WAIT FOR THE VIDEO INDEX TO FINISH
            //while (true)
            //{
            //    Thread.Sleep(10000);

            //    var videoGetIndexRequestResult = client.GetAsync($"{apiURL}/{location}/Accounts/{accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English").Result;
            //    var videoGetIndexResult = videoGetIndexRequestResult.Content.ReadAsStringAsync().Result;

            //    var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

            //    var diffInSeconds = (System.DateTime.Now - kickOffTime).TotalSeconds;


            //    Console.WriteLine("Indexing..." + diffInSeconds.ToString().Substring(0, diffInSeconds.ToString().IndexOf(".")) + " seconds elapsed.");

            //    // JOB IS FINISHED
            //    if (processingState != "Uploaded" && processingState != "Processing")
            //    {
            //        Console.WriteLine("Indexing complete.");
            //        break;
            //    }
            //}


            // GET BACK THE INDEXED DATA AND BEGIN PARSING IT
            // AND ADDING IT TO A VIDEO CLASS
            videoId = "41542a9bbc";


            var uri = apiURL + "/" + location + "trial/Accounts/" + accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=English";

           

            var task = client.GetAsync(uri)
              .ContinueWith((taskwithresponse) =>
              {     

                  var response = taskwithresponse.Result;
                  var jsonString = response.Content.ReadAsStringAsync();
                  jsonString.Wait();
                  var deserializedJson = JObject.Parse(jsonString.Result);

                  // THERE WILL ONLY BE ONE VIDEO RETURNED SINCE WE ARE 
                  // REFERENCING BY ID, SO WE WILL USE THE FIRST ONE
                  VideoObject.Id = deserializedJson["videos"][0]["id"].ToString();
                 


                  // ********************************
                  // CREATE THE TRANSCRIPT IN ENGLISH
                  // ********************************
                  var transcripts = deserializedJson["videos"][0]["insights"]["transcript"];
                  Transcript TranscriptEnglish = new Transcript();
                  TranscriptEnglish.Language = "English";

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
                      TranscriptEnglish.Lines.Add(NewLine);

                     
                  }

                  VideoObject.Transcripts.Add(TranscriptEnglish);

                  // ************************************
                  // WRITE THE FREQUENCY FOR EACH SPEAKER
                  // ************************************

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

                      VideoObject.Speakers.Add(NewSpeaker);
                  }

                  // *************************************************
                  // WRITE THE FACES IN THE VIDEO AND MARK KNOWN FACES
                  // *************************************************

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


                      VideoObject.Faces.Add(NewFace);
                  }

                  // ***************
                  // WRITE KEY WORDS
                  // ****************

                  var keyWords = deserializedJson["summarizedInsights"]["keywords"];

                  foreach (var keyword in keyWords)
                  {
                      VideoObject.KeyWords.Add(keyword["name"].ToString());
                    
                  }


                  // ********************
                  // WRITE OBJECTS/BRANDS
                  // ********************
                  var brands = deserializedJson["summarizedInsights"]["brands"];

                  foreach (var brand in brands)
                  {

                      VideoObject.Brands.Add(brand["name"].ToString());
                  }

                

              });
            task.Wait();


           uri = apiURL + "/" + location + "trial/Accounts/" + accountId + "/Videos/" + videoId + "/Index?accessToken=" + accountAccessToken + "&language=Spanish";

          

            var taskSpanish = client.GetAsync(uri)
              .ContinueWith((taskwithresponseEsp) =>
              {

                  var responseEsp = taskwithresponseEsp.Result;
                  var jsonStringEsp = responseEsp.Content.ReadAsStringAsync();
                  jsonStringEsp.Wait();
                  var deserializedJsonEsp = JObject.Parse(jsonStringEsp.Result);

                  // ********************************
                  // CREATE THE TRANSCRIPT IN SPANISH
                  // ********************************
                  var transcriptsEsp = deserializedJsonEsp["videos"][0]["insights"]["transcript"];
                  Transcript TranscriptSpanish= new Transcript();
                  TranscriptSpanish.Language = "Spanish";

                  int TranscriptOrderCount = 0;
                  foreach (var transcriptEsp in transcriptsEsp)
                  {
                      TranscriptLine NewLine = new TranscriptLine();

                      string transcriptSpeakerName = string.Empty;
                      try
                      {
                          int transcriptionSpeakerId = Convert.ToInt32(transcriptEsp["speakerId"].ToString());
                          transcriptSpeakerName = deserializedJsonEsp["videos"][0]["insights"]["speakers"][transcriptionSpeakerId]["name"].ToString();
                      }
                      catch (Exception NoSpeakerFound)
                      {
                          NoSpeakerFound.Data.Clear();
                          transcriptSpeakerName = "Speaker not found";
                      }

                      NewLine.Speaker = transcriptSpeakerName;
                      NewLine.Order = TranscriptOrderCount;
                      NewLine.Line = transcriptEsp["text"].ToString();
                      TranscriptOrderCount++;
                      TranscriptSpanish.Lines.Add(NewLine);


                  }

                  VideoObject.Transcripts.Add(TranscriptSpanish);



              });
            taskSpanish.Wait();



            // CONNECT AND ADD ENTRY TO COSMOS DB

            using (DocumentClient documentClient = new DocumentClient(new Uri(cosmosDBURI), cosmosKey))
            {

               await documentClient.CreateDatabaseIfNotExistsAsync(new Database { Id = "VideoDB" });

               await documentClient.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri("VideoDB"), new DocumentCollection { Id = "VideoCollection" });


                try
                {
                    await documentClient.ReadDocumentAsync(UriFactory.CreateDocumentUri("VideoDB", "VideoCollection", VideoObject.Id));

                }
                catch (DocumentClientException de)
                {
                    if (de.StatusCode == HttpStatusCode.NotFound)
                    {
                        await documentClient.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri("VideoDB", "VideoCollection"), VideoObject);

                    }
            
                }
            }


            

        }

        public static void ExecuteSimpleQuery(string videoName)
        {
            string cosmosDBURI = "https://chasvideotestdb.documents.azure.com:443/";
            string cosmosKey = "dic6xfvo6LFankKywhxtfu8rq7wzpMjvpgujANFLMg65ZXkqkIDqdCxeXQsiRKJ3ekl7JKMhXuSZA3Ube8ZwCw==";
            string databaseName = "VideoDB";
            string collectionName = "VideoCollection";

            using (DocumentClient documentClient = new DocumentClient(new Uri(cosmosDBURI), cosmosKey))
            {
                // Set some common query options
                FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

                // QUERY FOR VIDEOS BY NAME
                IQueryable<Video> videoQuery = documentClient.CreateDocumentQuery<Video>(
                        UriFactory.CreateDocumentCollectionUri(databaseName, collectionName), queryOptions)
                        .Where(v => v.Name == videoName);

                // The query is executed synchronously here, but can also be executed asynchronously via the IDocumentQuery<T> interface
                Console.WriteLine("Running LINQ query...");
                foreach (Video video in videoQuery)
                {
                    Console.WriteLine( video.Id + " - " + video.Name);
                }

              
            }
        }

    }
}
