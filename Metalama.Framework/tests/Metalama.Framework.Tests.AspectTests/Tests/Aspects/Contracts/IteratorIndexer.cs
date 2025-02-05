#if TEST_OPTIONS
// @Skipped(#32616)
#endif

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.IteratorIndexer;

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

public sealed class TestAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        base.BuildAspect( builder );

        foreach (var indexer in builder.Target.Indexers)
        {
            foreach (var parameter in indexer.GetMethod!.Parameters)
            {
                builder.With( parameter )
                    .AddContract(
                        nameof(ValidateParameter),
                        args: new { parameterName = parameter.Name } );
            }

            foreach (var parameter in indexer.SetMethod!.Parameters)
            {
                builder.With( parameter )
                    .AddContract(
                        nameof(ValidateParameter),
                        args: new { parameterName = parameter.Name } );
            }

            // TODO: #32616
            //builder.With( //    indexer.GetMethod!.ReturnParameter ).AddContract(
            //    nameof(ValidateParameter),
            //    args: new { parameterName = indexer.GetMethod!.ReturnParameter.Name });
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
    public IEnumerable<string> this[ string name ]
    {
        get
        {
            yield return "Hello";
            yield return name;
        }
        set { }
    }

    public IEnumerator<string> this[ string name1, string name2 ]
    {
        get
        {
            yield return "Hello";
            yield return name1;
            yield return name2;
        }
        set { }
    }
}