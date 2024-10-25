// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Eligibility;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Patterns.Contracts;

/// <summary>
/// Custom attribute that, when added to a field, property or parameter, throws
/// an <see cref="ArgumentException"/> if the target is assigned an empty value.
/// The custom attributes can be added to locations of type <see cref="string"/> (where empty
/// means zero characters), or <see cref="ICollection"/>, <see cref="ICollection{T}"/>, <see cref="IReadOnlyCollection{T}"/>, arrays or <see cref="ImmutableArray{T}"/>
/// (where empty means zero items).  Null references or default <see cref="ImmutableArray{T}"/> instances are accepted and do not throw an exception.
/// </summary>
/// <seealso href="@contract-types"/>
/// <seealso href="@enforcing-non-nullability"/>
[PublicAPI]
public sealed class NotEmptyAttribute : ContractBaseAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotEmptyAttribute"/> class.
    /// </summary>
    public NotEmptyAttribute() { }

    /// <inheritdoc/>
    public override void BuildEligibility( IEligibilityBuilder<IFieldOrPropertyOrIndexer> builder )
    {
        base.BuildEligibility( builder );

        builder.MustSatisfy(
            f => f.Type is INamedType t && (t.Equals( SpecialType.String ) || TryGetCompatibleTargetInterface( t, out _, out _ )),
            f => $"the type of {f} must string or implement ICollection, ICollection<T> or IReadOnlyCollection<T>" );
    }

    /// <inheritdoc/>
    public override void Validate( dynamic? value )
    {
        var context = new ContractContext( meta.Target );
        var options = context.Options;
        var templates = options.Templates!;

        var targetType = context.Type;
        var requiresNullCheck = targetType.IsNullable != false;

        if ( targetType.Equals( SpecialType.String ) || targetType is IArrayType )
        {
            if ( requiresNullCheck )
            {
                if ( value != null && value!.Length <= 0 )
                {
                    templates.OnNotEmptyContractViolated( value, context );
                }
            }
            else
            {
                if ( value!.Length <= 0 )
                {
                    templates.OnNotEmptyContractViolated( value, context );
                }
            }
        }
        else if ( targetType is INamedType namedType )
        {
            if ( namedType.Definition.IsConvertibleTo( typeof(ImmutableArray<>) ) )
            {
                if ( !value!.IsDefault && value.IsEmpty )
                {
                    templates.OnNotEmptyContractViolated( value, context );
                }
            }
            else if ( TryGetCompatibleTargetInterface( namedType, out var interfaceType, out var requiresCast ) )
            {
                if ( requiresCast )
                {
                    if ( requiresNullCheck )
                    {
                        if ( value != null && meta.Cast( interfaceType, value )!.Count <= 0 )
                        {
                            templates.OnNotEmptyContractViolated( value, context );
                        }
                    }
                    else
                    {
                        if ( meta.Cast( interfaceType, value )!.Count <= 0 )
                        {
                            templates.OnNotEmptyContractViolated( value, context );
                        }
                    }
                }
                else
                {
                    if ( requiresNullCheck )
                    {
                        if ( value != null && value!.Count <= 0 )
                        {
                            templates.OnNotEmptyContractViolated( value, context );
                        }
                    }
                    else
                    {
                        if ( value!.Count <= 0 )
                        {
                            templates.OnNotEmptyContractViolated( value, context );
                        }
                    }
                }
            }
            else
            {
                ThrowValidateCalledOnIneligibleTarget();
            }
        }
        else
        {
            ThrowValidateCalledOnIneligibleTarget();
        }
    }

    // TODO: #33294 Use simpler mechanism to throw a compile-time exception from a template if/when available.
    [CompileTime]
    private static void ThrowValidateCalledOnIneligibleTarget() => throw new InvalidOperationException( "Validate called on an ineligible target." );

    [CompileTime]
    private static bool TryGetCompatibleTargetInterface(
        INamedType targetType,
        [NotNullWhen( true )] out INamedType? interfaceType,
        out bool requiresCast )
    {
        var typeOfICollection = (INamedType) TypeFactory.GetType( typeof(ICollection) );

        if ( targetType.IsConvertibleTo( typeOfICollection ) )
        {
            var countProperty = typeOfICollection.Properties.OfName( nameof(ICollection.Count) ).Single();
            targetType.TryFindImplementationForInterfaceMember( countProperty, out var countPropertyImpl );

            interfaceType = typeOfICollection;
            requiresCast = countPropertyImpl is { IsExplicitInterfaceImplementation: true };

            return true;
        }
        else
        {
            var typeOfIReadOnlyCollection1 = (INamedType) TypeFactory.GetType( typeof(IReadOnlyCollection<>) );
            var typeOfICollection1 = (INamedType) TypeFactory.GetType( typeof(ICollection<>) );

            INamedType? foundInterface = null;

            foreach ( var t in targetType.GetSelfAndAllImplementedInterfaces() )
            {
                if ( t.IsGeneric )
                {
                    var originalDefinition = t.Definition;

                    if ( originalDefinition.Equals( typeOfIReadOnlyCollection1 ) ||
                         originalDefinition.Equals( typeOfICollection1 ) )
                    {
                        foundInterface = t;

                        break;
                    }
                }
            }

            if ( foundInterface != null )
            {
                var countProperty = foundInterface.Properties.OfName( "Count" ).Single();
                targetType.TryFindImplementationForInterfaceMember( countProperty, out var countPropertyImpl );

                interfaceType = foundInterface;
                requiresCast = countPropertyImpl is { IsExplicitInterfaceImplementation: true };

                return true;
            }
        }

        interfaceType = null;
        requiresCast = false;

        return false;
    }
}