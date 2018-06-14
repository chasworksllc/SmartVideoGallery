using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartVideoGallery.Models
{
    public class SpeakerFrequency
    {
        [JsonProperty(PropertyName = "beginTime")]
        public string BeginTime { get; set; }

        [JsonProperty(PropertyName = "endTime")]
        public string EndTime { get; set; }
    }
}
