class Student : Person
{
    public int Score { get; set; }

    public string GetLetterGrade()
    {
        return Score switch
        {
            < 60 => "F",
            < 70 => "D",
            < 80 => "C",
            < 90 => "B",
            _ => "A"
        };
    }

    public override string Describe()
    {
        return $"{Name}: {Score} ({GetLetterGrade()})";
    }
}
