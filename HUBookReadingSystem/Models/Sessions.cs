namespace HUBookReadingSystem.Models
{
    public class Sessions
    {
        public Guid Id { get; set; }
        public int ReaderId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
    }
}
