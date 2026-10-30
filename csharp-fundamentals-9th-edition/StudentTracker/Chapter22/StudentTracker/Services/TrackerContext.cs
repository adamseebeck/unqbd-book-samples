using Microsoft.EntityFrameworkCore;
using StudentTracker.Models;

namespace StudentTracker.Services;

public class TrackerContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder options)
    {
        options.UseSqlite("Data Source=students.db");
    }
}
