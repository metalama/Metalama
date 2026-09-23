namespace AspectLib;

public class Calculator
{
    [Log]
    public int Add( int a, int b ) => a + b;
}
