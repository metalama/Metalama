namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.AsyncMethod_CrossAssembly;

using System;
using System.Linq;
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