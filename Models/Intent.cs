using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NLP.Models
{
    public class Intent
    {
        public int intent_id { get; set; }
        public string? name { get; set; }
        public int count { get; set; }
        public double weigths_sum { get; set; }
        public double weigths_avg { get; set; }
        public double relevance_sum { get; set; }
        public double relevance_avg { get; set; }
        public Intent[]? subcategories { get; set; }
        public double confidence { get; set; }
    }
}
