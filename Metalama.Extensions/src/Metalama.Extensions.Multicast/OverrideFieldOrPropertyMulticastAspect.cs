// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Eligibility;
using System;
using System.Collections.Generic;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// An aspect equivalent to <see cref="OverrideFieldOrPropertyAspect"/> that also implements multicasting for backward compatibility with PostSharp.
/// </summary>
[AttributeUsage(
    AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Field,
    AllowMultiple = true )]
public abstract class OverrideFieldOrPropertyMulticastAspect : MulticastAspect, IAspect<IFieldOrProperty>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OverrideFieldOrPropertyMulticastAspect"/> class.
    /// </summary>
    protected OverrideFieldOrPropertyMulticastAspect() : base( MulticastTargets.Field | MulticastTargets.Property ) { }

    /// <inheritdoc />
    public virtual void BuildEligibility( IEligibilityBuilder<IFieldOrProperty> builder )
    {
        this.BuildEligibility( builder.DeclaringType() );

        builder.AddRule( EligibilityRuleFactory.GetAdviceEligibilityRule( AdviceKind.OverrideFieldOrPropertyOrIndexer ) );
    }

    /// <inheritdoc />
    public virtual void BuildAspect( IAspectBuilder<IFieldOrProperty> builder )
    {
        this.Implementation.BuildAspect(
            builder,
            b =>
            {
                var getterTemplateSelector = new GetterTemplateSelector(
                    "get_" + nameof(this.OverrideProperty),
                    "get_" + nameof(this.OverrideEnumerableProperty),
                    "get_" + nameof(this.OverrideEnumeratorProperty) );

                b.Advice.OverrideAccessors( b.Target, getterTemplateSelector, "set_" + nameof(this.OverrideProperty) );
            } );
    }

    [Template]
    public abstract dynamic? OverrideProperty
    {
        get;
        set;
    }

    [Template( IsEmpty = true )]
    public virtual IEnumerable<dynamic?> OverrideEnumerableProperty => throw new NotSupportedException();

    [Template( IsEmpty = true )]
    public virtual IEnumerator<dynamic?> OverrideEnumeratorProperty => throw new NotSupportedException();
}