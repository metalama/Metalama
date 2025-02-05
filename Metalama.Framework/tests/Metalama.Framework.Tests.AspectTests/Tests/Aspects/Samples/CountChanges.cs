using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Samples;

public class CountChangesAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var counterProperties = new List<IProperty>();

        foreach (var property in builder.Target.Properties)
        {
            var counterProperty = builder.IntroduceProperty(
                    nameof(CounterProperty),
                    buildProperty: b => b.Name = $"{property.Name}ChangeCount" )
                .Declaration;

            builder.With( property ).Override( nameof(IncrementCounter), tags: new { CounterProperty = counterProperty } );

            counterProperties.Add( counterProperty );
        }

        builder.IntroduceProperty(
            nameof(TotalChanges),
            tags: new { CounterProperties = counterProperties } );
    }

    [Template]
    public dynamic? IncrementCounter
    {
        set
        {
            var property = (IProperty)meta.Tags["CounterProperty"]!;

            var oldValue = meta.Target.Property.Value;

            meta.Proceed();

            if (oldValue != meta.Target.Property.Value)
            {
                property.Value = property.Value + 1;
            }
        }
    }

    [Template]
    public int CounterProperty { get; set; }

    [Template]
    public int TotalChanges
    {
        get
        {
            var properties = (IReadOnlyList<IProperty>)meta.Tags["CounterProperties"]!;
            var sum = 0;

            foreach (var property in properties)
            {
                sum += property.Value;
            }

            return sum;
        }
    }
}

// <target>
[CountChanges]
internal class C
{
    public string? Address { get; set; }

    public int Items { get; set; }
}