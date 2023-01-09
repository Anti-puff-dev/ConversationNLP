
namespace NLP.Models
{
    public class Message
    {
        public string? origin_id { get; set; }
        public string? origin { get; set; }
        public string? phrase { get; set; }
        public DateTime date_creation { get; set; } = DateTime.Now;
    }
}
