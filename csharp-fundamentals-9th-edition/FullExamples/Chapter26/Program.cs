EnrollmentResult accepted = new Enrolled("Ada", 3);
EnrollmentResult rejected = new Rejected("Course full");
EnrollmentResult empty = default;

Console.WriteLine(DescribeResult(accepted));
Console.WriteLine(DescribeResult(rejected));
Console.WriteLine(DescribeResult(empty));

EnrollmentState registered = new Registered("Ada");
EnrollmentState waitlisted = new Waitlisted(2);
EnrollmentState? absent = null;

Console.WriteLine(DescribeState(registered));
Console.WriteLine(DescribeState(waitlisted));
Console.WriteLine(DescribeState(absent));

static string DescribeResult(
    EnrollmentResult result) => result switch
{
    Enrolled(var student, var seat) =>
        $"Enrolled: {student}, seat {seat}",
    Rejected(var reason) => $"Rejected: {reason}",
    null => "No result"
};

static string DescribeState(
    EnrollmentState? state) => state switch
{
    Registered(var student) => $"Registered: {student}",
    Waitlisted(var position) =>
        $"Waitlisted: position {position}",
    null => "No status"
};

public sealed record Enrolled(string Student, int Seat);
public sealed record Rejected(string Reason);
public union EnrollmentResult(Enrolled, Rejected);

public closed record EnrollmentState;
public sealed record Registered(string Student) : EnrollmentState;
public sealed record Waitlisted(int Position) : EnrollmentState;
