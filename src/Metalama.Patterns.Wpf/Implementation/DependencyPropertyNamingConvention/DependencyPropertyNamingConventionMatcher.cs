// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Wpf.Implementation.NamingConvention;

namespace Metalama.Patterns.Wpf.Implementation.DependencyPropertyNamingConvention;

[CompileTime]
internal static class DependencyPropertyNamingConventionMatcher
{
    public static DependencyPropertyNamingConventionMatch Match(
        INamingConvention namingConvention,
        IProperty targetProperty,
        string dependencyPropertyName,
        string registrationFieldName,
        INameMatchPredicate matchPropertyChangedPredicate,
        INameMatchPredicate matchValidateNamePredicate,
        bool requirePropertyChangedMatch = false,
        bool requireValidateMatch = false )
    {
        var inspectedMembers = new List<InspectedMember>();

        var assets = targetProperty.Compilation.Cache.GetOrAdd( _ => new DependencyPropertyAssets() );

        var declaringType = targetProperty.DeclaringType;

        var conflictingMember = (IMemberOrNamedType?) declaringType.AllMembers().FirstOrDefault( m => m.Name == registrationFieldName )
                                ?? declaringType.Types.FirstOrDefault( t => t.Name == registrationFieldName );

        MemberMatch<IMemberOrNamedType, DefaultMatchKind> registrationFieldMatch;

        if ( conflictingMember != null )
        {
            registrationFieldMatch = MemberMatch<IMemberOrNamedType, DefaultMatchKind>.Conflict( conflictingMember );
        }
        else
        {
            registrationFieldMatch = MemberMatch<IMemberOrNamedType, DefaultMatchKind>.Success( DefaultMatchKind.Default );
        }

        var findPropertyChangedResult = declaringType.Methods.FindMatchingMembers(
            matchPropertyChangedPredicate,
            m => GetChangeHandlerSignature( m, targetProperty, assets, true ),
            inspectedMembers.Add,
            DependencyPropertyAspectBuilder.PropertyChangedMethodCategory );

        var findValidateResult = declaringType.Methods.FindMatchingMembers(
            matchValidateNamePredicate,
            m => GetValidationHandlerSignature( m, targetProperty, assets ),
            inspectedMembers.Add,
            DependencyPropertyAspectBuilder.ValidateMethodCategory );

        return new DependencyPropertyNamingConventionMatch(
            namingConvention,
            dependencyPropertyName,
            registrationFieldName,
            registrationFieldMatch,
            findPropertyChangedResult,
            findValidateResult,
            inspectedMembers,
            requirePropertyChangedMatch,
            requireValidateMatch );
    }

    private static ChangeHandlerSignatureKind? GetChangeHandlerSignature(
        IMethod method,
        IProperty targetProperty,
        DependencyPropertyAssets assets,
        bool allowOldValue )
    {
        var declaringType = targetProperty.DeclaringType;
        var propertyType = targetProperty.Type;

        var p = method.Parameters;

        if ( method.ReturnType.SpecialType != SpecialType.Void || p.Count > 2 || p.Any( parameter => parameter.RefKind is not (RefKind.None or RefKind.In) ) )
        {
            return null;
        }

        switch ( p.Count )
        {
            case 0:
                return method.IsStatic ? ChangeHandlerSignatureKind.StaticNoParameters : ChangeHandlerSignatureKind.InstanceNoParameters;

            case 1:

                if ( p[0].Type.Equals( assets.DependencyProperty ) )
                {
                    return method.IsStatic ? ChangeHandlerSignatureKind.StaticDependencyProperty : ChangeHandlerSignatureKind.InstanceDependencyProperty;
                }
                else if ( method.IsStatic
                          && (p[0].Type.SpecialType == SpecialType.Object
                              || p[0].Type.Equals( declaringType )
                              || p[0].Type.Equals( assets.DependencyObject )) )
                {
                    return ChangeHandlerSignatureKind.StaticInstance;
                }
                else if ( !method.IsStatic
                          && (p[0].Type.SpecialType == SpecialType.Object
                              || propertyType.IsConvertibleTo( p[0].Type )
                              || (p[0].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1)) )
                {
                    return ChangeHandlerSignatureKind.InstanceValue;
                }

                break;

            case 2:

                if ( method.IsStatic
                     && p[0].Type.Equals( assets.DependencyProperty )
                     && (p[1].Type.SpecialType == SpecialType.Object
                         || p[1].Type.Equals( declaringType )
                         || p[1].Type.Equals( assets.DependencyObject )) )
                {
                    return ChangeHandlerSignatureKind.StaticDependencyPropertyAndInstance;
                }
                else if ( allowOldValue
                          && !method.IsStatic
                          && (p[0].Type.SpecialType == SpecialType.Object
                              || propertyType.IsConvertibleTo( p[0].Type )
                              || (p[0].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1))
                          && (p[1].Type.SpecialType == SpecialType.Object
                              || propertyType.IsConvertibleTo( p[1].Type )
                              || (p[1].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1)) )
                {
                    return ChangeHandlerSignatureKind.InstanceOldValueAndNewValue;
                }

                break;
        }

        return null;
    }

    private static ValidationHandlerSignatureKind? GetValidationHandlerSignature(
        IMethod method,
        IProperty targetProperty,
        DependencyPropertyAssets assets )
    {
        var declaringType = targetProperty.DeclaringType;
        var propertyType = targetProperty.Type;

        var p = method.Parameters;

        if ( method.ReturnType.SpecialType != SpecialType.Void
             || p.Count > 3
             || p.Any( parameter => parameter.RefKind is not (RefKind.None or RefKind.In) ) )
        {
            return null;
        }

        switch ( p.Count )
        {
            case 0:
                return null;

            case 1:

                if ( p[0].Type.SpecialType == SpecialType.Object
                     || propertyType.IsConvertibleTo( p[0].Type )
                     || (p[0].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1) )
                {
                    return method.IsStatic ? ValidationHandlerSignatureKind.StaticValue : ValidationHandlerSignatureKind.InstanceValue;
                }

                break;

            case 2:

                if ( p[0].Type.Equals( assets.DependencyProperty )
                     && (p[1].Type.SpecialType == SpecialType.Object
                         || propertyType.IsConvertibleTo( p[1].Type )
                         || (p[1].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1)) )
                {
                    return method.IsStatic
                        ? ValidationHandlerSignatureKind.StaticDependencyPropertyAndValue
                        : ValidationHandlerSignatureKind.InstanceDependencyPropertyAndValue;
                }
                else if ( method.IsStatic
                          && (p[0].Type.SpecialType == SpecialType.Object
                              || p[0].Type.Equals( declaringType )
                              || p[0].Type.Equals( assets.DependencyObject ))
                          && (p[1].Type.SpecialType == SpecialType.Object
                              || propertyType.IsConvertibleTo( p[1].Type )
                              || (p[1].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1)) )
                {
                    return ValidationHandlerSignatureKind.StaticInstanceAndValue;
                }

                break;

            case 3:

                if ( method.IsStatic
                     && p[0].Type.Equals( assets.DependencyProperty )
                     && (p[1].Type.SpecialType == SpecialType.Object
                         || p[1].Type.Equals( declaringType )
                         || p[1].Type.Equals( assets.DependencyObject ))
                     && (p[2].Type.SpecialType == SpecialType.Object
                         || propertyType.IsConvertibleTo( p[2].Type )
                         || (p[2].Type.TypeKind == TypeKind.TypeParameter && method.TypeParameters.Count == 1)) )
                {
                    return ValidationHandlerSignatureKind.StaticDependencyPropertyAndInstanceAndValue;
                }

                break;
        }

        return null;
    }
}