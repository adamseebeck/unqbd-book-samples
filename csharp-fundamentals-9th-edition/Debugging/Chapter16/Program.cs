int x = 10;
int y = 0;
try
{
    int result = x / y;
    Console.WriteLine(result);
}
catch (DivideByZeroException ex)
{
    Console.WriteLine(ex.Message);
}
