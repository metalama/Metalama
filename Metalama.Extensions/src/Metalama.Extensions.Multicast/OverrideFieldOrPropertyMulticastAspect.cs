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
/// <remarks>
/// <para>
/// This class combines the field/property overriding capabilities of <see cref="OverrideFieldOrPropertyAspect"/> with the multicasting features
/// provided by <see cref="MulticastAspect"/>. It can be applied to fields, properties, types, or assemblies, and will automatically apply to
/// matching fields and properties based on the multicasting filter properties inherited from <see cref="IMulticastAttribute"/>.
/// </para>
/// <para>
/// For details on the template system and best practices, see the documentation of <see cref="OverrideFieldOrPropertyAspect"/>.
/// </para>
/// </remarks>
/// <seealso cref="OverrideFieldOrPropertyAspect"/>
/// <seealso cref="MulticastAspect"/>
/// <seealso cref="IMulticastAttribute"/>
/// <seealso cref="OverrideMethodMulticastAspect"/>
/// <seealso href="@migrating-multicasting"/>
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

                b.OverrideAccessors( getterTemplateSelector, "set_" + nameof(this.OverrideProperty) );
            } );
    }

    /// <summary>
    /// The default template property for overriding field or property accessors. See <see cref="OverrideFieldOrPropertyAspect.OverrideProperty"/> for details.
    /// </summary>
    [Template]
    public abstract dynamic? OverrideProperty
    {
        get;
        set;
    }

    /// <summary>
    /// Template property for overriding properties returning <see cref="IEnumerable{T}"/>. See <see cref="OverrideFieldOrPropertyAspect.OverrideEnumerableProperty"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual IEnumerable<dynamic?> OverrideEnumerableProperty => throw new NotSupportedException();

    /// <summary>
    /// Template property for overriding properties returning <see cref="IEnumerator{T}"/>. See <see cref="OverrideFieldOrPropertyAspect.OverrideEnumeratorProperty"/> for details.
    /// </summary>
    [Template( IsEmpty = true )]
    public virtual IEnumerator<dynamic?> OverrideEnumeratorProperty => throw new NotSupportedException();
}