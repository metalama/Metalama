namespace AspectLib;

public class Calculator
{
    [Log]
    public static int Add( int a, int b ) => a + b;

    public static int Subtract( int a, int b ) => a - b;
}
