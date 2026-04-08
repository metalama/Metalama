// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Code.SyntaxBuilders;

// ReSharper disable UnusedMember.Global

namespace Metalama.Patterns.Contracts;

[CompileTime]
internal static class CompileTimeHelpers
{
    public static IExpression ToTypeOf( this Type type )
    {
        var expressionBuilder = new ExpressionBuilder();
        expressionBuilder.AppendVerbatim( "typeof(" );
        expressionBuilder.AppendTypeName( type );
        expressionBuilder.AppendVerbatim( ")" );

        return expressionBuilder.ToExpression();
    }

    public static IEnumerable<INamedType> GetSelfAndAllImplementedInterfaces( this INamedType type )
    {
        if ( type == null )
        {
            throw new ArgumentNullException( nameof(type) );
        }

        if ( type.TypeKind == TypeKind.Interface )
        {
            yield return type;
        }

        foreach ( var i in type.AllImplementedInterfaces )
        {
            yield return i;
        }
    }

    private const string _numberBaseFullName = "System.Numerics.INumberBase";

    public static bool IsGenericMathType( IType type )
    {
        var nonNullable = type.ToNonNullable();

        // Fast reject: intrinsic (well-known) types — int, string, object, Task, etc.
        if ( nonNullable.SpecialType != SpecialType.None )
        {
            return false;
        }

        // Check if the type (or its constraints) transitively implements INumberBase<>.
        if ( nonNullable is ITypeParameter typeParameter )
        {
            // Walk type constraints for a type parameter.
            foreach ( var constraint in typeParameter.TypeConstraints )
            {
                if ( constraint is INamedType namedConstraint && ImplementsNumberBase( namedConstraint ) )
                {
                    return true;
                }
            }

            return false;
        }
        else if ( nonNullable is INamedType namedType )
        {
            // Also handle Nullable<T> where T is a type parameter constrained to INumberBase<>.
            if ( namedType.IsGeneric && namedType.Definition.SpecialType == SpecialType.Nullable_T )
            {
                var underlyingType = namedType.TypeArguments[0];

                return IsGenericMathType( underlyingType );
            }

            return ImplementsNumberBase( namedType );
        }

        return false;
    }

    private static bool ImplementsNumberBase( INamedType type )
    {
        // Check the type itself and all implemented interfaces for INumberBase<>.
        if ( IsNumberBaseDefinition( type ) )
        {
            return true;
        }

        foreach ( var iface in type.AllImplementedInterfaces )
        {
            if ( IsNumberBaseDefinition( iface ) )
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNumberBaseDefinition( INamedType type )
    {
        if ( type.TypeKind != TypeKind.Interface )
        {
            return false;
        }

        var definition = type.IsGeneric ? type.Definition : type;

        return definition.FullName == _numberBaseFullName;
    }

    public static void WarnIfNullable<T>( this IAspectBuilder<T> aspectBuilder )
        where T : class, IDeclaration, IHasType
    {
        if ( aspectBuilder.Target.Type.IsNullable == true && aspectBuilder.Target.Type.TypeKind != TypeKind.TypeParameter &&
             (aspectBuilder.Target.GetContractOptions().WarnOnNotNullableOnNullable ?? true) )
        {
            aspectBuilder.Diagnostics.Report(
                ContractDiagnostics.NotNullableOnNullable.WithArguments( (aspectBuilder.Target, aspectBuilder.AspectInstance.AspectClass.ShortName) ) );
        }
    }
}