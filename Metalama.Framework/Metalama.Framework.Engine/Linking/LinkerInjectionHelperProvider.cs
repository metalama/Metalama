// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using OperatorKind = Metalama.Framework.Code.OperatorKind;

namespace Metalama.Framework.Engine.Linking;

#pragma warning disable CA1822 // Mark members as static

// ReSharper disable MemberCanBeMadeStatic.Global
internal sealed class LinkerInjectionHelperProvider
{
    public const string SyntaxTreeName = "__LinkerInjectionHelpers__.cs";

    public const string HelperTypeName = "__LinkerInjectionHelpers__";
    public const string ConstructorMemberName = "__Constructor";
    public const string FinalizeMemberName = "__Finalize";
    public const string StaticConstructorMemberName = "__StaticConstructor";
    public const string PropertyMemberName = "__Property";
    public const string AsyncVoidMethodMemberName = "__AsyncVoidMethod";
    private const string _eventFieldInitializationExpressionMemberName = "__EventFieldInitializationExpression__";
    private const string _emptyCodeTypeName = "__Empty";
    private const string _sourceCodeTypeName = "__Source";
    private const string _overriddenByTypeName = "__OverriddenBy";
    private const string _auxiliaryTypeName = "__Auxiliary";
    private const string _ordinalTypeName = "__Ordinal";
    private const string _compositeOrdinalTypeName = "__CompositeOrdinal";

    private static readonly ConcurrentDictionary<LanguageOptions, SyntaxTree> _linkerHelperSyntaxTreeCache = new();

    private readonly bool _useNullability;
    private readonly CompilationModel _finalCompilationModel;

    public LinkerInjectionHelperProvider( CompilationModel finalCompilationModel, bool useNullability )
    {
        // TODO: Usage of nullability should be determined from context (design time).
        this._finalCompilationModel = finalCompilationModel;
        this._useNullability = useNullability;
    }

    public static ExpressionSyntax GetFinalizeMemberExpression()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            IdentifierName( FinalizeMemberName ) );

    public static ExpressionSyntax GetConstructorMemberExpression()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            IdentifierName( ConstructorMemberName ) );

    public static ExpressionSyntax GetStaticConstructorMemberExpression()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            IdentifierName( StaticConstructorMemberName ) );

    public static ExpressionSyntax GetPropertyMemberExpression()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            IdentifierName( PropertyMemberName ) );

    public static ExpressionSyntax GetAsyncVoidMethodMemberExpression()
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            IdentifierName( AsyncVoidMethodMemberName ) );

    public static ExpressionSyntax GetEventFieldInitializerExpressionMemberExpression( TypeSyntax eventFieldType )
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            GenericName(
                Identifier( _eventFieldInitializationExpressionMemberName ),
                TypeArgumentList( SingletonSeparatedList( eventFieldType ) ) ) );

    public static ExpressionSyntax GetOperatorMemberExpression(
        ContextualSyntaxGenerator syntaxGenerator,
        OperatorKind operatorKind,
        IType returnType,
        IEnumerable<IType> parameterTypes )
        => MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName( HelperTypeName ),
            GenericName(
                Identifier( operatorKind.ToOperatorMethodName() ),
                TypeArgumentList(
                    SeparatedList(
                        parameterTypes.Select( p => syntaxGenerator.TypeSyntax( p ) )
                            .Append( syntaxGenerator.TypeSyntax( returnType ) ) ) ) ) );

    public TypeSyntax GetSourceType() => QualifiedName( IdentifierName( HelperTypeName ), IdentifierName( _sourceCodeTypeName ) );

    public TypeSyntax GetOverriddenByType( SyntaxGenerationContext context, IAspectClass aspectType, int ordinal )
        => this.GetNumberedHelperType( context, _overriddenByTypeName, "override", aspectType, ordinal );

    public TypeSyntax GetAuxiliaryType( SyntaxGenerationContext context, IAspectClass aspectType, int ordinal )
        => this.GetNumberedHelperType( context, _auxiliaryTypeName, "auxiliary", aspectType, ordinal );

    private TypeSyntax GetNumberedHelperType(
        SyntaxGenerationContext context,
        string baseTypeName,
        string description,
        IAspectClass aspectType,
        int ordinal )
    {
        var aspectTypeSyntax = context.SyntaxGenerator.TypeSyntax( this._finalCompilationModel.Factory.GetTypeByReflectionType( aspectType.Type ) );

        switch ( ordinal )
        {
            case 0:
                return
                    QualifiedName(
                        IdentifierName( HelperTypeName ),
                        GenericName(
                            Identifier( baseTypeName ),
                            TypeArgumentList( SingletonSeparatedList( aspectTypeSyntax ) ) ) );

            default:
                return
                    QualifiedName(
                        IdentifierName( HelperTypeName ),
                        GenericName(
                            Identifier( baseTypeName ),
                            TypeArgumentList(
                                SeparatedList( [aspectTypeSyntax, GetOrdinalTypeArgument( aspectType.ShortName, description, ordinal )] ) ) ) );
        }
    }

    private static TypeSyntax GetOrdinalTypeArgument( string aspectName, string kind, int ordinal )
    {
        switch ( ordinal )
        {
            case <= 0:
                throw new AssertionFailedException( $"Cannot create a {ordinal} ordinal for {kind} by aspect {aspectName} (internal linker error)." );

            case < 10:
                return
                    QualifiedName(
                        IdentifierName( HelperTypeName ),
                        IdentifierName( _ordinalTypeName + ordinal ) );

            case < 100:
                return
                    QualifiedName(
                        IdentifierName( HelperTypeName ),
                        GenericName(
                            Identifier( _compositeOrdinalTypeName ),
                            TypeArgumentList(
                                SeparatedList<TypeSyntax>(
                                    new[]
                                    {
                                        QualifiedName(
                                            IdentifierName( HelperTypeName ),
                                            IdentifierName( _ordinalTypeName + (ordinal / 10) ) ),
                                        QualifiedName(
                                            IdentifierName( HelperTypeName ),
                                            IdentifierName( _ordinalTypeName + (ordinal % 10) ) )
                                    } ) ) ) );

            default:
                // NOTE: Lets have a beer when someone really hits this limit (without having a bug in the aspect).
                throw new AssertionFailedException(
                    $"More than 100 {kind} declarations created for a single member by aspect {aspectName} (aspect authoring error)." );
        }
    }

    public SyntaxTree GetLinkerHelperSyntaxTree( LanguageOptions options )
        => _linkerHelperSyntaxTreeCache.GetOrAdd( options, this.GetLinkerHelperSyntaxTreeCore );

    private SyntaxTree GetLinkerHelperSyntaxTreeCore( LanguageOptions options )
    {
        var useNullability = this._useNullability && options.Version is LanguageVersion.CSharp9 or LanguageVersion.CSharp10;
        var suffix = useNullability ? "?" : "";

        var binaryOperators =
            Enum.GetValues( typeof(OperatorKind) )
                .Cast<OperatorKind>()
                .Where( op => op.GetCategory() == OperatorCategory.Binary )
                .Select( op => $"public static R{suffix} {op.ToOperatorMethodName()}<A,B,R>(A{suffix} a, B{suffix} b) => default(R{suffix});" );

        var unaryOperators =
            Enum.GetValues( typeof(OperatorKind) )
                .Cast<OperatorKind>()
                .Where( op => op.GetCategory() == OperatorCategory.Unary )
                .Select( op => $"public static R{suffix} {op.ToOperatorMethodName()}<A,R>(A{suffix} a) => default(R{suffix});" );

        var conversionOperators =
            Enum.GetValues( typeof(OperatorKind) )
                .Cast<OperatorKind>()
                .Where( op => op.GetCategory() == OperatorCategory.Conversion )
                .Select( op => $"public static R{suffix} {op.ToOperatorMethodName()}<A,R>(A{suffix} a) => default(R{suffix});" );

        var code = @$"
{(useNullability ? "#nullable enable" : "")}
// NOTE: Currently all references to this type should be removed by the linker and where not possible, an error should be produced (e.g. non-inlineable declarations).
internal class {HelperTypeName}
{{
    // Members used for special aspect references, which do not use normal expression such as invocation.
    public static T {ConstructorMemberName}<T>(T value) => value;
    public static void {FinalizeMemberName}() {{}}
    public static void {StaticConstructorMemberName}() {{}}
    public static ref T{suffix} {PropertyMemberName}<T>(T{suffix} value) => ref Dummy<T{suffix}>.Field;    
    public static void {_eventFieldInitializationExpressionMemberName}<T>(T? value) where T : System.Delegate {{}}
    {string.Join( "\n    ", binaryOperators )}
    {string.Join( "\n    ", unaryOperators )}
    {string.Join( "\n    ", conversionOperators )}
    
    public delegate System.Threading.Tasks.Task WrappedDelegate(params object[] args);

    public static WrappedDelegate {AsyncVoidMethodMemberName}<T>(T t) where T : System.Delegate
    {{
        return Wrapped;
        static System.Threading.Tasks.Task Wrapped(params object[] args) => System.Threading.Tasks.Task.CompletedTask;
    }}

    // Types that are used as additional parameters for members where name cannot be changed.
    public readonly struct {_emptyCodeTypeName} {{}}
    public readonly struct {_sourceCodeTypeName} {{}}

    // Used for overrides, first override by single aspect does not use ordinal.
    public readonly struct {_overriddenByTypeName}<TAspect> {{}}
    public readonly struct {_overriddenByTypeName}<TAspect, TOrdinal> {{}}

    // Used for special declarations which are not result of overrides, e.g. target bodies for inserted statements.
    // These have well-known body structure and can usually bypass body analysis.
    public readonly struct {_auxiliaryTypeName}<TAspect> {{}}
    public readonly struct {_auxiliaryTypeName}<TAspect, TOrdinal> {{}}
    
    // Ordinal types used when there are multiple declarations of the same kind.
    public readonly struct {_ordinalTypeName}0 {{}}
    public readonly struct {_ordinalTypeName}1 {{}}
    public readonly struct {_ordinalTypeName}2 {{}}
    public readonly struct {_ordinalTypeName}3 {{}}
    public readonly struct {_ordinalTypeName}4 {{}}
    public readonly struct {_ordinalTypeName}5 {{}}
    public readonly struct {_ordinalTypeName}6 {{}}
    public readonly struct {_ordinalTypeName}7 {{}}
    public readonly struct {_ordinalTypeName}8 {{}}
    public readonly struct {_ordinalTypeName}9 {{}}

    // Composite ordinal that is used when there are more than 9, e.g. C<1,9>.
    public readonly struct {_compositeOrdinalTypeName}<T1, T2> {{}}

    // Helper for returning reference for property expressions.
    public class Dummy<T>
    {{
        public static T? Field;
    }}
}}
                ";

        return CSharpSyntaxTree.ParseText(
            code,
            path: SyntaxTreeName,
            encoding: Encoding.UTF8,
            options: options.ToParseOptions() );
    }
}