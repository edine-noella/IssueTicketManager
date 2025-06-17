using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore;
using IssueTicketManager.API.Models;
using ModelsLabel = IssueTicketManager.API.Models.Label;

namespace IssueTicketManager.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) 
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Issue> Issues { get; set; }
        public DbSet<ModelsLabel> Labels { get; set; } //Labels was causing a naming conflict
        public DbSet<Comment> Comments { get; set; }
        public DbSet<IssueLabel> IssueLabels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // this creates a join table with composite primary keys(issueId and LabelId)
            modelBuilder.Entity<IssueLabel>()
                .HasKey(il => new { il.IssueId, il.LabelId });
                
            modelBuilder.Entity<IssueLabel>()
                .HasOne(il => il.Issue)
                .WithMany(i => i.IssueLabels)
                .HasForeignKey(il => il.IssueId);
                
            modelBuilder.Entity<IssueLabel>()
                .HasOne(il => il.Label)
                .WithMany(l => l.IssueLabels)
                .HasForeignKey(il => il.LabelId);
                
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Creator)
                .WithMany(u => u.CreatedIssues)
                .HasForeignKey(i => i.CreatorId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete
        
            
            modelBuilder.Entity<Issue>()
                .HasOne(i => i.Assignee)
                .WithMany(u => u.AssignedIssues)
                .HasForeignKey(i => i.AssigneeId)
                .IsRequired(false) // Assignee is optional
                .OnDelete(DeleteBehavior.Restrict); 
            
            // keeping status as a string
            modelBuilder.Entity<Issue>()
                .Property(i => i.Status)
                .HasConversion<string>();
                
            
        }
    }
}