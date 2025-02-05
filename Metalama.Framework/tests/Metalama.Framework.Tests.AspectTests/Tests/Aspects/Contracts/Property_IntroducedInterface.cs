using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface;

#pragma warning disable CS8618, CS0169

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(NotNullAttribute), typeof(IntroduceInterfaceAttribute) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.Property_IntroducedInterface;

public class IntroduceInterfaceAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(I) );
    }

    [InterfaceMember( IsExplicit = true )]
    public string? M
    {
        get
        {
            Console.WriteLine( "Introduced." );

            return meta.Proceed();
        }

        set
        {
            Console.WriteLine( "Introduced." );
            meta.Proceed();
        }
    }

    [InterfaceMember( IsExplicit = true )]
    public string? N { get; set; } = default;
}

public class NotNullAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        foreach (var property in builder.Target.Properties)
        {
            if (property.IsImplicitlyDeclared)
            {
                continue;
            }

            builder.With( property ).AddContract( nameof(Validate), ContractDirection.Both );
        }
    }

    [Template]
    public void Validate( dynamic? value )
    {
        if (value == null)
        {
            throw new ArgumentNullException( meta.Target.Property.Name );
        }
    }
}

public interface I
{
    string? M { get; set; }

    string? N { get; set; }
}

// <target>
[IntroduceInterface]
[NotNull]
internal record Target { }