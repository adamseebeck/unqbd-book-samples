using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

string directory = Path.Combine(
    Path.GetTempPath(), $"csharp-school-{Guid.NewGuid():N}");
Directory.CreateDirectory(directory);
using SchoolContext db =
    new(Path.Combine(directory, "school.db"));
db.Database.EnsureCreated();
db.Students.AddRange(
    new Student { Name = "Maria", Score = 95 },
    new Student { Name = "Ben", Score = 87 },
    new Student { Name = "Lila", Score = 80 });
db.SaveChanges();
Console.WriteLine($"Students: {db.Students.Count()}");
foreach (Student student in db.Students.OrderBy(s => s.Name))
    Console.WriteLine($"{student.Name} - {student.Score}");

Student maria = db.Students.Single(s => s.Name == "Maria");
maria.Score = 98;
db.SaveChanges();
Console.WriteLine($"Updated Maria: {maria.Score}");

Student lila = db.Students.Single(s => s.Name == "Lila");
db.Students.Remove(lila);
db.SaveChanges();
Console.WriteLine("Deleted Lila.");

var topStudents = db.Students.Where(s => s.Score >= 90)
    .OrderByDescending(s => s.Score).ThenBy(s => s.Name).ToList();
foreach (Student student in topStudents)
    Console.WriteLine($"Top: {student.Name} - {student.Score}");
Console.WriteLine($"Remaining: {db.Students.Count()}");

public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Score { get; set; }
}

public class SchoolContext : DbContext
{
    private readonly string _databasePath;
    public SchoolContext(string databasePath)
        => _databasePath = databasePath;
    public DbSet<Student> Students => Set<Student>();

    protected override void OnConfiguring(
        DbContextOptionsBuilder options)
    {
        string connectionString =
            new SqliteConnectionStringBuilder
            {
                DataSource = _databasePath
            }.ToString();
        options.UseSqlite(connectionString);
    }
}
