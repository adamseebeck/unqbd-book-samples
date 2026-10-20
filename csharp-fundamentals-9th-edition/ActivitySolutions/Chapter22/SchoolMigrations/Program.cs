using Microsoft.EntityFrameworkCore;

using SchoolContext db = new();
Console.WriteLine($"Students: {db.Students.Count()}");

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class SchoolContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=school-migrations.db");
}
