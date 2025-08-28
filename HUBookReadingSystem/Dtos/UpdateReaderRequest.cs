namespace HUBookReadingSystem.Dtos
{
    public class UpdateReaderRequest
    {
        public string? Name { get; set; }
        public int? TargetCount { get; set; }
        public int? CurrentRound { get; set; }
        public string? Pin { get; set; }
    }
}
