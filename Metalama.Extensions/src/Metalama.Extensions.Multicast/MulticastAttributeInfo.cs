// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using System;
using System.Text.RegularExpressions;

namespace Metalama.Extensions.Multicast;

/// <summary>
/// Encapsulates an <see cref="IMulticastAttribute"/> and provides matching methods for it.
/// </summary>
[CompileTime]
internal class MulticastAttributeInfo : IComparable<MulticastAttributeInfo>
{
    public IMulticastAttribute Attribute { get; }

    private Regex? AttributeTargetParametersRegex { get; }

    private Regex? AttributeTargetMembersRegex { get; }

    private Regex? AttributeTargetTypesRegex { get; }

    public MulticastAttributeInfo( IAspectInstance aspectInstance, IAspectBuilder aspectBuilder )
    {
        this.Attribute = (IMulticastAttribute) aspectInstance.Aspect;
        this.AttributeTargetTypesRegex = GetRegex( this.Attribute.AttributeTargetTypes, nameof(IMulticastAttribute.AttributeTargetTypes) );
        this.AttributeTargetMembersRegex = GetRegex( this.Attribute.AttributeTargetMembers, nameof(IMulticastAttribute.AttributeTargetMembers) );
        this.AttributeTargetParametersRegex = GetRegex( this.Attribute.AttributeTargetParameters, nameof(IMulticastAttribute.AttributeTargetParameters) );

        Regex? GetRegex( string? filter, string propertyName )
        {
            if ( filter == null )
            {
                return null;
            }
            else if ( filter.StartsWith( "regex:", StringComparison.OrdinalIgnoreCase ) )
            {
                try
                {
                    return new Regex( filter.Substring( 6 ) );
                }
                catch ( ArgumentException e )
                {
                    var declaration = aspectInstance.TargetDeclaration.GetTarget( aspectBuilder.Target.Compilation );

                    aspectBuilder.Diagnostics.Report(
                        MulticastAspect.InvalidRegexError.WithArguments(
                            (propertyName, aspectInstance.AspectClass.ShortName, declaration.DeclarationKind, declaration, e.Message) ) );

                    return null;
                }
            }
            else
            {
#pragma warning disable CA1307
                return new Regex(
                    "^" + Regex.Escape( filter ).Replace( "\\*", ".*" ) + "$",
                    RegexOptions.IgnoreCase );
#pragma warning restore CA1307
            }
        }
    }

    public int CompareTo( MulticastAttributeInfo? other )
        => other == null ? 1 : this.Attribute.AttributePriority.CompareTo( other.Attribute.AttributePriority );

    private static bool DoesNameMatch( INamedDeclaration named, Regex? regex )
    {
        if ( regex == null )
        {
            return true;
        }
        else
        {
            return regex.IsMatch( named.Name );
        }
    }

    public bool IsMatch( IDeclaration declaration, MulticastTargets targets, bool testContainingDeclarations = false )
    {
        if ( this.Attribute.AttributeTargetElements != 0 && !this.Attribute.AttributeTargetElements.HasAnyFlag( targets ) )
        {
            return false;
        }

        return this.IsMatchCore( declaration, testContainingDeclarations, false );
    }

    private bool IsMatchCore( IDeclaration declaration, bool testContainingDeclarations, bool testDeclarationKind )
    {
        if ( testDeclarationKind && !this.DoesDeclarationKindMatch( declaration ) )
        {
            return false;
        }

        switch ( declaration.DeclarationKind )
        {
            case DeclarationKind.Compilation:
                return true;

            case DeclarationKind.NamedType:
                return this.IsNamedTypeMatch( (INamedType) declaration );

            case DeclarationKind.Parameter:
                return this.IsParameterMatch( (IParameter) declaration, testContainingDeclarations );

            case DeclarationKind.Field:
                return this.IsFieldMatch( (IField) declaration, testContainingDeclarations );

            case DeclarationKind.Method:
                return this.IsMethodMatch( (IMethod) declaration, testContainingDeclarations );

            default:
                return this.IsMemberMatch( (IMember) declaration, testContainingDeclarations );
        }
    }

    private bool IsMemberMatch( IMember member, bool testDeclaringType )
    {
        if ( testDeclaringType && !this.IsNamedTypeMatch( member.DeclaringType ) )
        {
            return false;
        }

        return DoMemberAttributesMatch( member, this.Attribute.AttributeTargetMemberAttributes ) && DoesNameMatch( member, this.AttributeTargetMembersRegex );
    }

    private bool IsParameterMatch( IParameter parameter, bool testContainingDeclarations )
    {
        if ( testContainingDeclarations && !this.IsMatchCore( parameter.DeclaringMember, true, false ) )
        {
            return false;
        }

        return DoesParameterDirectionMatch( parameter, this.Attribute.AttributeTargetParameterAttributes )
               && DoesNameMatch( parameter, this.AttributeTargetParametersRegex );
    }

    private bool IsNamedTypeMatch( INamedType type )
        => DoMemberOrNamedTypeAttributesMatch( type, this.Attribute.AttributeTargetTypeAttributes )
           && DoesFullNameMatch( type, this.AttributeTargetTypesRegex );

    private bool IsFieldMatch( IField field, bool testDeclaringType )
        => this.IsMemberMatch( field, testDeclaringType ) && DoesLiteralMatch( field, this.Attribute.AttributeTargetMemberAttributes );

    private bool IsMethodMatch( IMethod method, bool testDeclaringType )
        => this.IsMemberMatch( method, testDeclaringType ) && DoesManagedMatch( method, this.Attribute.AttributeTargetMemberAttributes );

    private static bool DoMemberOrNamedTypeAttributesMatch( IMemberOrNamedType member, MulticastAttributes attributes )
        => DoesAccessibilityMatch( member, attributes ) &&
           DoesAbstractionMatch( member, attributes ) &&
           DoesScopeMatch( member, attributes ) &&
           DoesCompilerGeneratedMatch( member, attributes );

    private static bool DoMemberAttributesMatch( IMember member, MulticastAttributes attributes )
        => DoMemberOrNamedTypeAttributesMatch( member, attributes ) &&
           DoesVirtualityMatch( member, attributes );

    private static bool DoesFullNameMatch( INamedType namedType, Regex? regex )
    {
        if ( regex == null )
        {
            return true;
        }
        else
        {
            return regex.IsMatch( namedType.GetFullMetadataName() );
        }
    }

    private static bool DoesAccessibilityMatch( IMemberOrNamedType member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyVisibility) != 0 )
        {
            switch ( member.Accessibility )
            {
                case Accessibility.Private:
                    return attributes.HasFlagFast( MulticastAttributes.Private );

                case Accessibility.PrivateProtected:
                    return attributes.HasFlagFast( MulticastAttributes.InternalAndProtected );

                case Accessibility.Protected:
                    return attributes.HasFlagFast( MulticastAttributes.Protected );

                case Accessibility.Internal:
                    return attributes.HasFlagFast( MulticastAttributes.Internal );

                case Accessibility.ProtectedInternal:
                    return attributes.HasFlagFast( MulticastAttributes.InternalOrProtected );

                case Accessibility.Public:
                    return attributes.HasFlagFast( MulticastAttributes.Public );

                default:
                    throw new ArgumentOutOfRangeException( $"Unexpected accessibility: {member.Accessibility}." );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesLiteralMatch( IField member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyLiterality) != 0 )
        {
            if ( member.Writeability == Writeability.None )
            {
                return attributes.HasFlagFast( MulticastAttributes.Literal );
            }
            else
            {
                return attributes.HasFlagFast( MulticastAttributes.NonLiteral );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesManagedMatch( IMethod member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyImplementation) != 0 )
        {
            if ( member.IsExtern )
            {
                return attributes.HasFlagFast( MulticastAttributes.NonManaged );
            }
            else
            {
                return attributes.HasFlagFast( MulticastAttributes.Managed );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesCompilerGeneratedMatch( IDeclaration member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyGeneration) != 0 )
        {
            if ( member.Origin.IsCompilerGenerated )
            {
                return attributes.HasFlagFast( MulticastAttributes.CompilerGenerated );
            }
            else
            {
                return attributes.HasFlagFast( MulticastAttributes.UserGenerated );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesScopeMatch( IMemberOrNamedType member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyScope) != 0 )
        {
            if ( member.IsStatic )
            {
                return attributes.HasFlagFast( MulticastAttributes.Static );
            }
            else
            {
                return attributes.HasFlagFast( MulticastAttributes.Instance );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesAbstractionMatch( IMemberOrNamedType member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyAbstraction) != 0 )
        {
            if ( member.IsAbstract )
            {
                return attributes.HasFlagFast( MulticastAttributes.Abstract );
            }
            else
            {
                return attributes.HasFlagFast( MulticastAttributes.NonAbstract );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesVirtualityMatch( IMember member, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyVirtuality) != 0 )
        {
            if ( member.IsVirtual )
            {
                return attributes.HasFlagFast( MulticastAttributes.Virtual );
            }
            else
            {
                return attributes.HasFlagFast( MulticastAttributes.NonVirtual );
            }
        }
        else
        {
            return true;
        }
    }

    private static bool DoesParameterDirectionMatch( IParameter parameter, MulticastAttributes attributes )
    {
        if ( (attributes & MulticastAttributes.AnyParameter) != 0 )
        {
            return parameter.RefKind switch
            {
                RefKind.Out => attributes.HasFlagFast( MulticastAttributes.OutParameter ),
                RefKind.Ref => attributes.HasFlagFast( MulticastAttributes.RefParameter ),
                _ => attributes.HasFlagFast( MulticastAttributes.InParameter )
            };
        }
        else
        {
            return true;
        }
    }

    private bool DoesDeclarationKindMatch( IDeclaration declaration )
    {
        var targets = this.Attribute.AttributeTargetElements;

        if ( targets == 0 )
        {
            return true;
        }

        return targets.HasFlagFast( MulticastTargetsHelper.GetMulticastTargets( declaration ) );
    }
}