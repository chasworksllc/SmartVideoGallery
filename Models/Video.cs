using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace SmartVideoGallery.Models
{
    public class Video
    {
        public Video()
        {
            this.Transcripts = new List<Transcript>();
            this.Speakers = new List<Speaker>();
            this.Faces = new List<Face>();
            this.KeyWords = new List<string>();
            this.Brands = new List<string>();

        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "transcripts")]
        public List<Transcript> Transcripts { get; set; }

        [JsonProperty(PropertyName = "speakers")]
        public List<Speaker> Speakers { get; set; }

        [JsonProperty(PropertyName = "faces")]
        public List<Face> Faces { get; set; }

        [JsonProperty(PropertyName = "keywords")]
        public List<string> KeyWords { get; set; }

        [JsonProperty(PropertyName = "brands")]
        public List<string> Brands { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
