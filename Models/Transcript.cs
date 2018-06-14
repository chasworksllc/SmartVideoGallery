using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SmartVideoGallery.Models
{
    public class Transcript
    {

        public Transcript()
        {
            this.Lines = new List<TranscriptLine>();
        }

        [JsonProperty(PropertyName = "language")]
        public string Language { get; set; }

        [JsonProperty(PropertyName = "lines")]
        public List<TranscriptLine> Lines {get;set;}
    }
}
