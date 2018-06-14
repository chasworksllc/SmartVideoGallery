using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartVideoGallery.Models
{
    public class TranscriptLine
    {
        [JsonProperty(PropertyName = "order")]
        public int Order { get; set; }

        [JsonProperty(PropertyName = "speaker")]
        public string Speaker { get; set; }

        [JsonProperty(PropertyName = "line")]
        public string Line { get; set; }
    }
}
