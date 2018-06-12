using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartVideoGallery.Models
{
    public class SummarizedInsight
    {

        public SummarizedInsight()
        {
            faces = new List<Face>();
        }

        public string name { get; set; }
        public string id { get; set; }
        public string privacyMode { get; set; }
      
        public Duration duration { get; set; }

        public List<Face> faces { get; set; }
    }
}
