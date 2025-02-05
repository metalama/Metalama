namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Iterator_ReturnParameter;

using System;
using System.Collections;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

public sealed class TestAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach (var method in builder.Target.Methods)
        {
            builder.With( method.ReturnParameter ).AddContract( nameof(ValidateParameter) );
        }
    }

    [Template]
    private void ValidateParameter( dynamic? value )
    {
        Console.WriteLine( $"Advice" );

        if (meta.Target.Parameter.Type.IsConvertibleTo( SpecialType.IEnumerable )
            || meta.Target.Parameter.Type.IsConvertibleTo(
                TypeFactory.GetType( SpecialType.IEnumerable_T ).WithTypeArguments( TypeFactory.GetType( SpecialType.String ) ) ))
        {
            foreach (var item in value!)
            {
                if (item is null)
                {
                    throw new ArgumentNullException( "<return>" );
                }
            }
        }
        else
        {
            while (value!.MoveNext())
            {
                if (value.Current is null)
                {
                    throw new ArgumentNullException( "<return>" );
                }
            }
        }
    }
}

public class Program
{
    private static void TestMain()
    {
        const string text = "testText";
        var test = new TestClass();

        foreach (var item in test.Enumerable( text ))
        {
            Console.WriteLine( $"{item};" );
        }

        var enumerator1 = test.Enumerator( text );

        while (enumerator1.MoveNext())
        {
            Console.WriteLine( $"{enumerator1.Current};" );
        }

        foreach (var item in test.EnumerableT( text ))
        {
            Console.WriteLine( $"{item};" );
        }

        var enumerator2 = test.EnumeratorT( text );

        while (enumerator2.MoveNext())
        {
            Console.WriteLine( $"{enumerator2.Current};" );
        }
    }
}

// <target>
[Test]
public class TestClass
{
    public IEnumerable Enumerable( string text )
    {
        yield return "Hello";
        yield return text;
    }

    public IEnumerator Enumerator( string text )
    {
        yield return "Hello";
        yield return text;
    }

    public IEnumerable<string> EnumerableT( string text )
    {
        yield return "Hello";
        yield return text;
    }

    public IEnumerator<string> EnumeratorT( string text )
    {
        yield return "Hello";
        yield return text;
    }
}