// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Code;
using Metalama.Framework.Code.DeclarationBuilders;
using Metalama.Framework.Eligibility;
using Metalama.Framework.Options;
using Metalama.Framework.Tests.AspectTests.Tests.Options;

[assembly: AspectOrder( AspectOrderDirection.RunTime, typeof(ShowOptionsAspect), typeof(ModifyOptionsAspect) )]

namespace Metalama.Framework.Tests.AspectTests.Tests.Options;

public class MyOptions : IHierarchicalOptions<IDeclaration>
{
    public string? Value { get; init; }

    public string? OverrideHistory { get; init; }

    public bool? BaseWins { get; init; }

#if !NET5_0_OR_GREATER
    public IHierarchicalOptions GetDefaultOptions( OptionsInitializationContext context ) => this;
#endif

    public object ApplyChanges( object changes, in ApplyChangesContext context )
    {
        if (BaseWins.GetValueOrDefault() && context.Axis == ApplyChangesAxis.ContainingDeclaration)
        {
            return this;
        }
        else
        {
            var other = (MyOptions)changes;

            if (other.Value == null)
            {
                return this;
            }

            return new MyOptions
            {
                Value = other.Value ?? Value,
                BaseWins = other.BaseWins ?? BaseWins,
                OverrideHistory = $"{OverrideHistory ?? Value}->{other.OverrideHistory ?? other.Value}"
            };
        }
    }
}

public class MyOptionsAttribute : Attribute, IHierarchicalOptionsProvider
{
    private string _value;
    private bool _baseWins;

    public MyOptionsAttribute( string value, bool baseWins = false )
    {
        _value = value;
        _baseWins = baseWins;
    }

    public IEnumerable<IHierarchicalOptions> GetOptions( in OptionsProviderContext context )
    {
        return new[] { new MyOptions { Value = _value, BaseWins = _baseWins } };
    }
}

public class ActualOptionsAttribute : Attribute
{
    public ActualOptionsAttribute( string value ) { }
}

public class ShowOptionsAspect : Attribute, IAspect<IDeclaration>
{
    public void BuildAspect( IAspectBuilder<IDeclaration> builder )
    {
        builder.IntroduceAttribute(
            AttributeConstruction.Create( typeof(ActualOptionsAttribute), new[] { builder.Target.Enhancements().GetOptions<MyOptions>().OverrideHistory } ) );
    }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }
}

public class ModifyOptionsAspect : Attribute, IAspect<IDeclaration>
{
    private string _value;

    public ModifyOptionsAspect( string value )
    {
        _value = value;
    }

    public void BuildAspect( IAspectBuilder<IDeclaration> builder )
    {
        // Get the old value so populate the cache and we can test that the cache is invalidated.
        var oldValue = builder.Target.Enhancements().GetOptions<MyOptions>().Value;
        builder.Outbound.SetOptions( x => new MyOptions { Value = $"{oldValue}->{_value}" } );
    }

    public void BuildEligibility( IEligibilityBuilder<IDeclaration> builder ) { }
}