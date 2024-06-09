namespace SearchAPI.Models
{
    public class ElasticSettings
    {
        

        public string ApiKey { get; set; }
        public string Username { get; set; }
        public string ApiValue { get; set; }
        public string SqlDBConnection { get; set; }
        public string ElaIndex { get; set; }
        public Uri ElaUri { get; set; }
        public string ReactUrl { get; set; }
    }
}
