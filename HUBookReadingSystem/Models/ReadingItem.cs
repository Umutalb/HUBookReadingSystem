using System.ComponentModel.DataAnnotations;

namespace HUBookReadingSystem.Models
{
    public class ReadingItem
    {
        // Represents a single book entry in a reader's list

        [Key]
        public int Id { get; set; } // Primary key, unique identifier for the book

        [Required]
        public int ReaderId { get; set; }   // Foreign key linking to Reader (who owns this book)

        [Required]
        public int Round { get; set; }  // Indicates which reading round this book belongs to

        [Required, StringLength(200)]
        public string Title { get; set; }   // Title of the book (max length 200 characters)

        public bool IsDone { get; set; } = false;   // Status: true if the book is finished

        public DateTime? StartedAt { get; set; }    // Date when reading started (optional)
        public DateTime? FinishedAt { get; set; }   // Date when reading finished (optional)

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;  // Date when this record was created (UTC)
    }
}
