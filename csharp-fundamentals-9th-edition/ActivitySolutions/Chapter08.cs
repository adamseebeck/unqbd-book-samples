Student[] students =
{
    new Student { Name = "Maria", GradeLevel = 10, GPA = 3.5 },
    new Student { Name = "David", GradeLevel = 11, GPA = 3.2 }
};
foreach (Student student in students)
{
    student.DisplayInfo();
    if (student.IsOnHonorRoll()) Console.WriteLine("Honor roll");
}

class Student
{
    public string Name { get; set; } = "";
    public int GradeLevel { get; set; }
    public double GPA { get; set; }
    public void DisplayInfo()
    {
        Console.WriteLine($"{Name}: grade {GradeLevel}, GPA {GPA}");
    }
    public bool IsOnHonorRoll()
    {
        return GPA >= 3.5;
    }
}
