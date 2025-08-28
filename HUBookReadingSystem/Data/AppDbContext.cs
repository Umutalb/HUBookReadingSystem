using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using HUBookReadingSystem.Models;

namespace HUBookReadingSystem.Data
{
    public class AppDbContext : DbContext
    {
        // Constructor to pass options (like connection string) to the base DbContext
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Table for readers (Hazal, Umut)
        public DbSet<Reader> Readers { get; set; }

        // Table for reading items (books linked to each reader)
        public DbSet<ReadingItem> ReadingItems { get; set; }

        public DbSet<Sessions> Sessions { get; set; }
    }
}
