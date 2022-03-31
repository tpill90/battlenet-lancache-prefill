namespace Shared.Models
{
    public class ComparedRequest : Request
    {
        public bool Matched { get; set; }

        public Request MatchedRequest { get; set; }
    }
}