using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartVideoGallery.Models
{
    public class Face
    {
        public Face()
        {
            appearances = new List<Appearance>();
        }
        public int id { get; set; }
        public string videoId { get; set; }

       
       public string referenceId { get; set; }
        public string referenceType { get; set; }
        public string knownPersonId { get; set; }
        public int confidence { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string thumbnailId { get; set; }
     
        public List<Appearance> appearances { get; set; }
        public float seenDuration { get; set; }
        public float seenDurationRatio { get; set; }
    }

}



    //  "id": 1000,
    //  "videoId": "a61635115f",
    //  "referenceId": null,
    //  "referenceType": "Bing",
    //  "knownPersonId": "00000000-0000-0000-0000-000000000000",
    //  "confidence": 0,
    //  "name": "Unknown #1",
    //  "description": null,
    //  "title": null,
    //  "thumbnailId": "4aeb7f00-bcc2-40d0-b33c-0557ab1b4b40",
    //  "appearances": [{
    //    "startTime": "0:00:02.169",
    //    "endTime": "0:00:31.325",
    //    "startSeconds": 2.2,
    //    "endSeconds": 31.3
    //  }],
    //  "seenDuration": 29.1,
    //  "seenDurationRatio": 0.4798
    //}