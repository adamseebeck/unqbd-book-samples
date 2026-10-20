using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

string directory = Path.Combine(Path.GetTempPath(), $"csharp-school-activity-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "school.db");
using (SchoolContext db = new(path))
{
    db.Database.EnsureCreated();
    db.Students.AddRange(
        new Student { Name = "Maria", Score = 95 },
        new Student { Name = "Ben", Score = 87 },
        new Student { Name = "Lila", Score = 80 });
    db.SaveChanges();
    foreach (Student student in db.Students.OrderBy(s => s.Name))
        Console.WriteLine($"{student.Name} - {student.Score}");
    Console.WriteLine("Above 85:");
    foreach (Student student in db.Students.Where(s => s.Score > 85)
                 .OrderByDescending(s => s.Score).ThenBy(s => s.Name))
        Console.WriteLine($"{student.Name} - {student.Score}");
}
// Open a second context to prove the data came back from the same file.
using (SchoolContext reopened = new(path))
{
    Console.WriteLine($"Reopened count: {reopened.Students.Count()}");
}

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class SchoolContext : DbContext
{
    private readonly string _databasePath;
    public SchoolContext(string databasePath) => _databasePath = databasePath;
    public DbSet<Student> Students => Set<Student>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath
        }.ToString();
        options.UseSqlite(connectionString);
    }
}
