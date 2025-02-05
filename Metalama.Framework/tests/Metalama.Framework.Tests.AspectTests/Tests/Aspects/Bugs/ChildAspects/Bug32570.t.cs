public class TargetType
{
    public int Foo(int x)
    {
        global::System.Console.WriteLine("Validate x");
        global::System.Int32 returnValue;
        global::System.Console.WriteLine("Overridden");
        Console.WriteLine("Original");
        returnValue = x;
        global::System.Console.WriteLine("Validate <return>");
        return returnValue;
    }
    public void Foo_Void(int x)
    {
        Console.WriteLine("Original");
    }
    public global::System.Int32 Bar(global::System.Int32 x)
    {
        global::System.Console.WriteLine("Validate x");
        global::System.Int32 returnValue;
        global::System.Console.WriteLine("Overridden");
        global::System.Console.WriteLine("Introduced");
        returnValue = x;
        global::System.Console.WriteLine("Validate <return>");
        return returnValue;
    }
    public void Bar_Void(global::System.Int32 x)
    {
        global::System.Console.WriteLine("Introduced");
    }
}