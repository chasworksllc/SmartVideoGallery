using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartVideoGallery.Models
{
    public class Speaker
    {
        public Speaker()
        {
            this.Frequencies = new List<SpeakerFrequency>();
        }

        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "frequencies")]
        public List<SpeakerFrequency> Frequencies {get;set;}

    }

}
