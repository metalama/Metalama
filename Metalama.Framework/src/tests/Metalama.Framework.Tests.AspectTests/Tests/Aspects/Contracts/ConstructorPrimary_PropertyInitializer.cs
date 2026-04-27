// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Contracts.ConstructorPrimary_PropertyInitializer;

/// <summary>
/// Tests that when both a constructor contract and a property contract are applied
/// to a class with a primary constructor, the property backing field initializer
/// does not reference the now-removed primary constructor parameter.
/// This reproduces issue #1536.
/// </summary>
internal class ValidateAttribute : TypeAspect
{
    public override void BuildAspect( IAspectBuilder<INamedType> builder )
    {
        // Apply contract to constructor parameter (triggers primary constructor removal).
        foreach ( var constructor in builder.Target.Constructors )
        {
            foreach ( var parameter in constructor.Parameters )
            {
                if ( parameter.Type.IsReferenceType == true && parameter.Type.IsNullable != true )
                {
                    builder.With( parameter ).AddContract( nameof(this.ValidateParameter), ContractDirection.Input );
                }
            }
        }

        // Apply contract to properties (triggers backing field creation).
        foreach ( var property in builder.Target.Properties )
        {
            if ( property.IsImplicitlyDeclared )
            {
                continue;
            }

            if ( property.Type.IsReferenceType == true && property.Type.IsNullable != true && property.Writeability != Writeability.None )
            {
                builder.With( property ).AddContract( nameof(this.ValidateProperty), ContractDirection.Input );
            }
        }
    }

    [Template]
    private void ValidateParameter( dynamic? value )
    {
        if ( value == null )
        {
            throw new ArgumentNullException( meta.Target.Parameter.Name );
        }
    }

    [Template]
    private void ValidateProperty( dynamic? value )
    {
        if ( value == null )
        {
            throw new ArgumentNullException( nameof(value) );
        }
    }
}

// <target>
[Validate]
internal sealed class Target( Action action ) : IDisposable
{
    private Action Action { get; init; } = action;

    void IDisposable.Dispose()
    {
        this.Action();
    }
}