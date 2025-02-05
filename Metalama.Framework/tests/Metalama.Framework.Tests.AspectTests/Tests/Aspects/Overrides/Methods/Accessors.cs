using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Overrides.Methods.Accessors;

public class MyAspect : OverrideMethodAspect
{
    public override dynamic? OverrideMethod()
    {
        Console.WriteLine( "Overridden." );

        return meta.Proceed();
    }
}

// <target>
internal class C
{
    public int Property1
    {
        [MyAspect]
        get;
    }

    public int Property2
    {
        [MyAspect]
        set { }
    }

    public int Property3
    {
        [MyAspect]
        get;
        [MyAspect]
        set;
    }

    public event Action Event1
    {
        [MyAspect]
        add { }

        [MyAspect]
        remove { }
    }

    public event Action Event2
    {
        [MyAspect]
        add { }

        remove { }
    }

    public event Action Event3
    {
        add { }

        [MyAspect]
        remove { }
    }

    public int this[ int x ]
    {
        [MyAspect]
        get
        {
            Console.WriteLine( "Original" );

            return x;
        }
    }

    public int this[ int x, int y ]
    {
        [MyAspect]
        set
        {
            Console.WriteLine( "Original" );
        }
    }

    public int this[ int x, int y, int z ]
    {
        [MyAspect]
        get
        {
            Console.WriteLine( "Original" );

            return x + y;
        }

        [MyAspect]
        set
        {
            Console.WriteLine( "Original" );
        }
    }
}