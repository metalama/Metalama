using System;

public class C
{
    [ParentAspect]
    public void M()
    {
        Console.WriteLine( "Some template" );
    }
}