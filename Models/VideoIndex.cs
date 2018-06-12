using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartVideoGallery.Models
{
    public class VideoIndex
    {

        public VideoIndex()
        {
          
        }
        public string accountId { get; set; }
        public string id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string userName { get; set; }
        public string created { get; set; }
        public string privacyMode { get; set; }
        public string state { get; set; }
        public bool isOwned { get; set; }
        public bool isEditable { get; set; }
        public bool isBase { get; set; }
        public int durationInSeconds { get; set; }

        public SummarizedInsight summarizedInsigts { get; set; }

       

        public string thumbnailVideoId { get; set; }

        public string thumbnail { get; set; }

    }
}
