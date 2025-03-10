// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.AsyncMethod;

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

            if (method.ReturnType.IsReferenceType.GetValueOrDefault()
                && !method.ReturnType.IsNullable.GetValueOrDefault()
                && !method.GetAsyncInfo().ResultType.IsConvertibleTo( SpecialType.Void ))
            {
                builder.With( method.ReturnParameter )
                    .AddContract(
                        nameof(ValidateMethodResult),
                        args: new { methodName = method.Name } );
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

    [Template]
    private void ValidateMethodResult( dynamic? value, [CompileTime] string methodName )
    {
        if (value is null)
        {
            throw new InvalidOperationException( "Method returned null" );
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