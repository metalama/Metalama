namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.AsyncMethod_NoReturnContract;

using System;
using System.Linq;
using System.Threading.Tasks;
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
            foreach (var parameter in method.Parameters.Where(
                         p => p.RefKind is RefKind.None or RefKind.In
                              && !p.Type.IsNullable.GetValueOrDefault()
                              && p.Type.IsReferenceType.GetValueOrDefault() ))
            {
                builder.With( parameter )
                    .AddContract(
                        nameof(ValidateParameter),
                        args: new { parameterName = parameter.Name } );
            }
        }
    }

    [Template]
    private void ValidateParameter( dynamic? value, [CompileTime] string parameterName )
    {
        if (value is null)
        {
            throw new ArgumentNullException( parameterName );
        }
    }
}

// <target>
[Test]
public class TestClass
{
    public string DoSomething( string text )
    {
        Console.WriteLine( "Hello" );

        return null!;
    }

    public async Task DoSomethingAsync( string text )
    {
        await Task.Yield();

        Console.WriteLine( "Hello" );
    }

    public async Task<string> DoSomethingAsyncT( string text )
    {
        await Task.Yield();

        Console.WriteLine( "Hello" );

        return null!;
    }

    public async void DoSomethingAsyncVoid( string text )
    {
        await Task.Yield();

        Console.WriteLine( "Hello" );
    }
}