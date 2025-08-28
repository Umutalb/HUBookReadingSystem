using System.ComponentModel.DataAnnotations;

namespace HUBookReadingSystem.Models
{
    public class Reader
    {
        // Represents a user (reader) in the system

        [Key]
        public int Id { get; set; }     // PK

        [Required, StringLength(50)]
        public string Name { get; set; }    // Reader name

        [Range(0, 1000)]
        public int TargetCount { get; set; }   // Target number of books

        [Required]
        public int CurrentRound { get; set; } = 1;  // In which round (default: 1)

        public byte[] PinHash { get; set; }
        public byte[] PinSalt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Creation date (UTC)
    }
}
