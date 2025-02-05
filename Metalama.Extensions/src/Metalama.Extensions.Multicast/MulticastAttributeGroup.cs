// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// Encapsulates a group of attributes of an aspect type applied to the same declaration.
/// </summary>
[CompileTime]
internal class MulticastAttributeGroup
{
    private readonly MulticastTargets _allowedTargets;
    private readonly List<MulticastAttributeInfo> _attributes;

    /// <summary>
    /// Gets the aspect class of the group.
    /// </summary>
    public IAspectClass AspectClass { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MulticastAttributeGroup"/> class.
    /// </summary>
    /// <param name="builder">The <see cref="IAspectBuilder"/>.</param>
    /// <param name="allowedTargets">The set of targets to which the aspect can be multicast.</param>
    public MulticastAttributeGroup( IAspectBuilder builder, MulticastTargets allowedTargets )
    {
        this._allowedTargets = allowedTargets;
        var aspectInstance = builder.AspectInstance;
        this._attributes = new List<MulticastAttributeInfo>( aspectInstance.SecondaryInstances.Length + 1 ) { new( aspectInstance, builder ) };
        this.AspectClass = builder.AspectInstance.AspectClass;

        foreach ( var instance in aspectInstance.SecondaryInstances )
        {
            this._attributes.Add( new MulticastAttributeInfo( instance, builder ) );
        }

        this._attributes.Sort();
    }

    /// <summary>
    /// Gets a value indicating whether the group contains a single attribute that has the <see cref="IMulticastAttribute.AttributeExclude"/> property
    /// set to <c>true</c>.
    /// </summary>
    public bool IsExcludeOnly => this._attributes.Count == 1 && this._attributes[0].Attribute.AttributeExclude;

    /// <summary>
    /// Gets a value indicating whether a given declaration matches an attribute in the current group and has not been eventually excluded by <see cref="IMulticastAttribute.AttributeExclude"/>.
    /// </summary>
    /// <param name="declaration">The declaration. Note that is does not need to be the final declaration to which the aspect is applied. It can be the declaring type or member.</param>
    /// <param name="targets">The kinds of target of the final declaration, which may not be the same as <paramref name="declaration"/>.</param>
    /// <returns><c>true</c> if an attribute in the current group matches <paramref name="declaration"/> and is not excluded by <see cref="IMulticastAttribute.AttributeExclude"/>,
    /// otherwise <c>false</c>.</returns>
    public bool IsMatch( IDeclaration declaration, MulticastTargets targets )
    {
        // Never match a declaration that already has a custom attribute for this aspect.
        if ( this.HasExcludeAttribute( declaration ) )
        {
            return false;
        }

        var isMatch = false;

        foreach ( var attribute in this._attributes )
        {
            if ( attribute.IsMatch( declaration, targets ) )
            {
                isMatch = !attribute.Attribute.AttributeExclude;
            }
        }

        return isMatch;
    }

    private bool HasExcludeAttribute( IDeclaration declaration )
    {
        var attributeOnTarget = declaration.Attributes.OfAttributeType( this.AspectClass.Type ).FirstOrDefault();

        if ( attributeOnTarget != null )
        {
            var attributeExcludeArgument = attributeOnTarget.NamedArguments.FirstOrDefault( a => a.Key == nameof(IMulticastAttribute.AttributeExclude) );

            if ( attributeExcludeArgument.Value.IsInitialized && (bool) attributeExcludeArgument.Value.Value! )
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the attribute in the group that matches a given declaration, taking <see cref="IMulticastAttribute.AttributeExclude"/> into account.
    /// Throws an exception if <see cref="IsMatch"/> returned <c>false</c> for this declaration.
    /// </summary>
    public IAspect GetMatchingAspect( IDeclaration declaration )
    {
        var targets = MulticastTargetsHelper.GetMulticastTargets( declaration );

        for ( var i = this._attributes.Count - 1; i >= 0; i-- )
        {
            var attribute = this._attributes[i];

            if ( attribute.IsMatch( declaration, targets, true ) && !attribute.Attribute.AttributeExclude )
            {
                return attribute.Attribute;
            }
        }

        throw new InvalidOperationException( $"There is no matching aspect." );
    }

    /// <summary>
    /// Gets a value indicating whether any attribute in the current group is compatible with a set of declaration kinds.
    /// </summary>
    public bool TargetsAnyDeclarationKind( MulticastTargets targets )
    {
        if ( (targets & this._allowedTargets) == 0 )
        {
            return false;
        }

        foreach ( var attribute in this._attributes )
        {
            if ( attribute.Attribute.AttributeTargetElements == 0 || (attribute.Attribute.AttributeTargetElements & targets) != 0 )
            {
                return true;
            }
        }

        return false;
    }
}