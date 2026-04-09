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

    private const string _numberReflectionName = "System.Numerics.INumber`1";

    public static bool IsGenericMathType( IType type )
    {
        var nonNullable = type.ToNonNullable();

        // Fast reject: intrinsic (well-known) types — int, string, object, Task, etc.
        // can never be generic-math type parameters.
        if ( nonNullable.SpecialType != SpecialType.None )
        {
            return false;
        }

        // Look up INumber<> in the target compilation. INumber<T> extends
        // IComparisonOperators<T,T,bool>, guaranteeing that < and > operators
        // are available for the generated range-check code.
        // INumberBase<T> alone is not sufficient because it does not provide
        // comparison operators (e.g. Complex implements INumberBase but not INumber).
        if ( !nonNullable.Compilation.Factory.TryGetTypeByReflectionName( _numberReflectionName, out var numberType ) )
        {
            return false;
        }

        return nonNullable.IsConvertibleTo( numberType, ConversionKind.TypeDefinition );
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