namespace seminar2blazor20230107220807
{
    public class Person
    {
        public int? Id { get; set; }
        public string? Personname { get; set; }
        public string? Passwordhash { get; set; }
        public string? Role { get; set; }
        public List<TimeTrack>? Tracks { get; set; }
    }
}
