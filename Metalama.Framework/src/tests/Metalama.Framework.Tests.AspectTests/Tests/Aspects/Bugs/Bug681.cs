// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug681;

[assembly: AspectOrder( AspectOrderDirection.CompileTime, typeof(ImplementInterfaceAspect), typeof(CheckInterfaceAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug681;

public interface IMyInterface
{
    void DoSomething();
}

public class ImplementInterfaceAspect : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        builder.ImplementInterface( typeof(IMyInterface) );
    }

    [InterfaceMember]
    public void DoSomething()
    {
        Console.WriteLine( "Introduced" );
    }
}

public class CheckInterfaceAspect : TypeAspect
{
    private static readonly DiagnosticDefinition<(INamedType, bool)> _isConvertibleResult = new(
        "BUG681A",
        Severity.Warning,
        "Type '{0}' IsConvertibleTo IMyInterface: {1}" );

    private static readonly DiagnosticDefinition<(INamedType, bool)> _allImplementedInterfacesResult = new(
        "BUG681B",
        Severity.Warning,
        "Type '{0}' has IMyInterface in AllImplementedInterfaces: {1}" );

    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        var targetType = builder.Target;
        var interfaceType = (INamedType) TypeFactory.GetType( typeof(IMyInterface) );

        // Check IsConvertibleTo for an interface added by a previous aspect.
        var isConvertible = targetType.IsConvertibleTo( interfaceType );

        builder.Diagnostics.Report( _isConvertibleResult.WithArguments( (targetType, isConvertible) ) );

        // Also check via AllImplementedInterfaces.
        var implementsViaCollection = false;

        foreach (var iface in targetType.AllImplementedInterfaces)
        {
            if (builder.Target.Compilation.Comparers.Default.Equals( iface, interfaceType ))
            {
                implementsViaCollection = true;

                break;
            }
        }

        builder.Diagnostics.Report( _allImplementedInterfacesResult.WithArguments( (targetType, implementsViaCollection) ) );
    }
}

// <target>
[ImplementInterfaceAspect]
[CheckInterfaceAspect]
public partial class TargetClass { }
