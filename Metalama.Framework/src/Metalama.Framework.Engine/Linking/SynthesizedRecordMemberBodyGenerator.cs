// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Identifies the compiler-synthesized record members whose body Metalama reproduces in source.
/// </summary>
internal enum SynthesizedRecordMemberKind
{
    /// <summary>
    /// The member is not a compiler-synthesized record member, or its body cannot be reproduced in source.
    /// </summary>
    None,

    /// <summary>
    /// The getter of the <c>EqualityContract</c> property.
    /// </summary>
    EqualityContractGetter,

    /// <summary>
    /// The strongly typed <c>Equals(R?)</c> overload.
    /// </summary>
    Equals,

    /// <summary>
    /// The <c>GetHashCode()</c> override.
    /// </summary>
    GetHashCode,

    /// <summary>
    /// The <c>ToString()</c> override.
    /// </summary>
    ToString,

    /// <summary>
    /// The <c>PrintMembers(StringBuilder)</c> method.
    /// </summary>
    PrintMembers,

    /// <summary>
    /// The <c>Deconstruct(out ...)</c> method of a positional record.
    /// </summary>
    Deconstruct
}

/// <summary>
/// Generates, for a compiler-synthesized record member, the C# body that the C# compiler would have synthesized.
/// The C# compiler builds those bodies directly as bound nodes and never exposes them, so reproducing the body in
/// source is the only way to let <c>meta.Proceed()</c> reach the original implementation.
/// </summary>
/// <remarks>
/// The body is returned as a list of statements plus an optional result expression, so that the caller can return the
/// result, assign it to the return variable of an inlining context, or discard it.
/// </remarks>
internal static class SynthesizedRecordMemberBodyGenerator
{
    /// <summary>
    /// The absolute value of the multiplier that <c>MethodBodySynthesizer.GenerateHashCombine</c> uses to combine hash codes.
    /// </summary>
    private const int _hashCombineMultiplier = 1521134295;

    /// <summary>
    /// The name of the local variable that the generated <c>ToString</c> body declares. The body is never inlined, as
    /// <see cref="BodyDeclaresLocalVariable"/> explains, so the name is only ever declared in a body of its own.
    /// </summary>
    private const string _stringBuilderLocalName = "builder";

    /// <summary>
    /// Determines whether the body generated for a member declares a local variable.
    /// </summary>
    /// <remarks>
    /// Such a body must not be inlined. The linker does not know which identifiers are in scope at the inlining site,
    /// and C# rejects a local variable that has the same name as one of an enclosing scope, so the body is emitted as
    /// a separate method instead. Only <c>ToString</c> is concerned.
    /// </remarks>
    public static bool BodyDeclaresLocalVariable( ISymbol symbol )
        => symbol.Kind == SymbolKind.Method
           && symbol is IMethodSymbol { IsImplicitlyDeclared: true } method
           && GetMemberKind( method ) == SynthesizedRecordMemberKind.ToString;

    /// <summary>
    /// Determines which compiler-synthesized record member a method is, if any.
    /// </summary>
    public static SynthesizedRecordMemberKind GetMemberKind( IMethodSymbol symbol )
    {
        var containingType = symbol.ContainingType;

        if ( containingType is not { IsRecord: true } )
        {
            return SynthesizedRecordMemberKind.None;
        }

        switch ( symbol )
        {
            case { MethodKind: MethodKind.PropertyGet, AssociatedSymbol: IPropertySymbol { Name: "EqualityContract" } }:
                return SynthesizedRecordMemberKind.EqualityContractGetter;

            case { Name: "Equals", MethodKind: MethodKind.Ordinary, Parameters: [{ Type: var parameterType }] }
                when SymbolEqualityComparer.Default.Equals( parameterType, containingType ):
                return SynthesizedRecordMemberKind.Equals;

            case { Name: "GetHashCode", MethodKind: MethodKind.Ordinary, Parameters.Length: 0 }:
                return SynthesizedRecordMemberKind.GetHashCode;

            case { Name: "ToString", MethodKind: MethodKind.Ordinary, Parameters.Length: 0 }:
                return SynthesizedRecordMemberKind.ToString;

            case { Name: "PrintMembers", MethodKind: MethodKind.Ordinary, Parameters: [{ Type.Name: "StringBuilder" }] }:
                return SynthesizedRecordMemberKind.PrintMembers;

            case { Name: "Deconstruct", MethodKind: MethodKind.Ordinary, ReturnsVoid: true }
                when symbol.Parameters.Length > 0 && symbol.Parameters.All( p => p.RefKind == RefKind.Out ):
                return SynthesizedRecordMemberKind.Deconstruct;

            default:
                return SynthesizedRecordMemberKind.None;
        }
    }

    /// <summary>
    /// Generates the body of a compiler-synthesized record member.
    /// </summary>
    /// <param name="symbol">The synthesized member. <see cref="GetMemberKind"/> must not return <see cref="SynthesizedRecordMemberKind.None"/> for it.</param>
    /// <param name="rewritingDriver">The rewriting driver, which resolves the name under which a backing field is emitted.</param>
    /// <param name="generationContext">The syntax generation context.</param>
    /// <returns>The statements of the body, and the expression whose value the member returns, or <c>null</c> when the member returns void.</returns>
    public static (IReadOnlyList<StatementSyntax> Statements, ExpressionSyntax? Result) GenerateBody(
        IMethodSymbol symbol,
        LinkerRewritingDriver rewritingDriver,
        SyntaxGenerationContext generationContext )
        => GetMemberKind( symbol ) switch
        {
            SynthesizedRecordMemberKind.EqualityContractGetter => ( [], GenerateEqualityContract( symbol, generationContext ) ),
            SynthesizedRecordMemberKind.Equals => ( [], GenerateEquals( symbol, rewritingDriver, generationContext ) ),
            SynthesizedRecordMemberKind.GetHashCode => ( [], GenerateGetHashCode( symbol, rewritingDriver, generationContext ) ),
            SynthesizedRecordMemberKind.ToString => GenerateToString( symbol, generationContext ),
            SynthesizedRecordMemberKind.PrintMembers => GeneratePrintMembers( symbol, generationContext ),
            SynthesizedRecordMemberKind.Deconstruct => ( GenerateDeconstruct( symbol ), null ),
            _ => throw new AssertionFailedException( $"'{symbol}' is not a materializable compiler-synthesized record member." )
        };

    private static ExpressionSyntax GenerateEqualityContract( IMethodSymbol symbol, SyntaxGenerationContext generationContext )
        => TypeOfExpression( generationContext.SyntaxGenerator.TypeSyntax( symbol.ContainingType ) );

    private static ExpressionSyntax GenerateEquals(
        IMethodSymbol symbol,
        LinkerRewritingDriver rewritingDriver,
        SyntaxGenerationContext generationContext )
    {
        var containingType = symbol.ContainingType;
        var other = SyntaxFactoryEx.SafeIdentifierName( symbol.Parameters[0].Name );
        var fields = GetFieldsToEmit( containingType, rewritingDriver );
        var hasRecordBase = HasRecordBase( containingType );

        var fieldComparisons = fields
            .Select(
                ( field, index ) =>
                    CreateFieldComparison(
                        field,

                        // The compiler cannot infer that the parameter is not null after the call to the base Equals,
                        // so the first access to it in the derived form suppresses the nullable warning.
                        index == 0 && hasRecordBase
                            ? PostfixUnaryExpression( SyntaxKind.SuppressNullableWarningExpression, other )
                            : other ) )
            .ToList();

        if ( containingType.IsValueType )
        {
            // A record struct compares its fields only. There is neither a reference comparison nor an equality contract.
            return fieldComparisons.Count == 0
                ? LiteralExpression( SyntaxKind.TrueLiteralExpression )
                : CombineWithAnd( fieldComparisons );
        }

        List<ExpressionSyntax> conjuncts;

        if ( hasRecordBase )
        {
            // The cast selects the strongly typed Equals of the base record instead of Equals(object).
            var baseEquals =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        BaseExpression(),
                        SyntaxFactoryEx.WellKnownIdentifierName( "Equals" ) ),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                CastExpression(
                                    NullableType( generationContext.SyntaxGenerator.TypeSyntax( containingType.BaseType.AssertNotNull() ) ),
                                    other ) ) ) ) );

            conjuncts = [baseEquals, .. fieldComparisons];
        }
        else
        {
            var otherIsNotNull =
                BinaryExpression(
                    SyntaxKind.NotEqualsExpression,
                    CastToObject( other, nullable: true ),
                    LiteralExpression( SyntaxKind.NullLiteralExpression ) );

            var equalityContractComparison =
                BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    EqualityContractAccess( ThisExpression() ),
                    EqualityContractAccess( other ) );

            conjuncts = [otherIsNotNull, equalityContractComparison, .. fieldComparisons];
        }

        var referenceEquality =
            BinaryExpression(
                SyntaxKind.EqualsExpression,
                CastToObject( ThisExpression(), nullable: false ),
                CastToObject( other, nullable: true ) );

        return BinaryExpression(
            SyntaxKind.LogicalOrExpression,
            referenceEquality,
            ParenthesizedExpression( CombineWithAnd( conjuncts ) ) );

        ExpressionSyntax CreateFieldComparison( RecordField field, ExpressionSyntax otherAccess )
            => InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    CreateDefaultEqualityComparer( field.Type, generationContext ),
                    SyntaxFactoryEx.WellKnownIdentifierName( "Equals" ) ),
                ArgumentList(
                    SeparatedList(
                        new[] { Argument( field.CreateAccess( ThisExpression() ) ), Argument( field.CreateAccess( otherAccess ) ) } ) ) );
    }

    private static ExpressionSyntax EqualityContractAccess( ExpressionSyntax target )
        => MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, target, SyntaxFactoryEx.WellKnownIdentifierName( "EqualityContract" ) );

    private static ExpressionSyntax GenerateGetHashCode(
        IMethodSymbol symbol,
        LinkerRewritingDriver rewritingDriver,
        SyntaxGenerationContext generationContext )
    {
        var containingType = symbol.ContainingType;
        var fields = GetFieldsToEmit( containingType, rewritingDriver );

        ExpressionSyntax? accumulator;

        if ( containingType.IsValueType )
        {
            // A record struct has no seed. The hash code of the first field is the seed and is not multiplied.
            accumulator = null;
        }
        else if ( HasRecordBase( containingType ) )
        {
            accumulator =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        BaseExpression(),
                        SyntaxFactoryEx.WellKnownIdentifierName( "GetHashCode" ) ),
                    ArgumentList() );
        }
        else
        {
            var typeType = generationContext.CompilationContext.Compilation.GetTypeByMetadataName( "System.Type" ).AssertNotNull();

            accumulator =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        CreateDefaultEqualityComparer( typeType, generationContext ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "GetHashCode" ) ),
                    ArgumentList( SingletonSeparatedList( Argument( EqualityContractAccess( ThisExpression() ) ) ) ) );
        }

        foreach ( var field in fields )
        {
            var fieldHashCode =
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        CreateDefaultEqualityComparer( field.Type, generationContext ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "GetHashCode" ) ),
                    ArgumentList( SingletonSeparatedList( Argument( field.CreateAccess( ThisExpression() ) ) ) ) );

            accumulator =
                accumulator == null
                    ? fieldHashCode
                    : BinaryExpression(
                        SyntaxKind.AddExpression,
                        ParenthesizedExpression(
                            BinaryExpression(
                                SyntaxKind.MultiplyExpression,
                                ParenthesizedExpression( accumulator ),
                                PrefixUnaryExpression( SyntaxKind.UnaryMinusExpression, SyntaxFactoryEx.LiteralExpression( _hashCombineMultiplier ) ) ) ),
                        fieldHashCode );
        }

        if ( accumulator == null )
        {
            return SyntaxFactoryEx.LiteralExpression( 0 );
        }

        // The compiler combines the hash codes in an unchecked context, so the generated source is explicitly
        // unchecked and behaves identically in a project that sets CheckForOverflowUnderflow.
        return CheckedExpression( SyntaxKind.UncheckedExpression, accumulator );
    }

    private static (IReadOnlyList<StatementSyntax> Statements, ExpressionSyntax? Result) GenerateToString(
        IMethodSymbol symbol,
        SyntaxGenerationContext generationContext )
    {
        var containingType = symbol.ContainingType;

        var stringBuilderTypeSyntax = generationContext.SyntaxGenerator.TypeSyntax(
            generationContext.CompilationContext.Compilation.GetTypeByMetadataName( "System.Text.StringBuilder" ).AssertNotNull() );

        var builder = SyntaxFactoryEx.SafeIdentifierName( _stringBuilderLocalName );

        var statements = new List<StatementSyntax>
        {
            LocalDeclarationStatement(
                VariableDeclaration(
                    stringBuilderTypeSyntax.WithOptionalTrailingTrivia( ElasticSpace, generationContext.Options ),
                    SingletonSeparatedList(
                        VariableDeclarator( SyntaxFactoryEx.SafeIdentifier( _stringBuilderLocalName ) )
                            .WithInitializer(
                                EqualsValueClause(
                                    ObjectCreationExpression(
                                        SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NewKeyword ),
                                        stringBuilderTypeSyntax,
                                        ArgumentList(),
                                        null ) ) ) ) ) ),
            ExpressionStatement( AppendString( builder, containingType.Name ) ),
            ExpressionStatement( AppendString( builder, " { " ) ),
            IfStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        SyntaxFactoryEx.WellKnownIdentifierName( "PrintMembers" ) ),
                    ArgumentList( SingletonSeparatedList( Argument( builder ) ) ) ),
                Block( ExpressionStatement( AppendChar( builder, ' ' ) ) ) ),
            ExpressionStatement( AppendChar( builder, '}' ) )
        };

        var result =
            InvocationExpression(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, builder, SyntaxFactoryEx.WellKnownIdentifierName( "ToString" ) ),
                ArgumentList() );

        return (statements, result);
    }

    private static (IReadOnlyList<StatementSyntax> Statements, ExpressionSyntax? Result) GeneratePrintMembers(
        IMethodSymbol symbol,
        SyntaxGenerationContext generationContext )
    {
        var containingType = symbol.ContainingType;
        var builder = SyntaxFactoryEx.SafeIdentifierName( symbol.Parameters[0].Name );
        var printableMembers = GetPrintableMembers( containingType );
        var hasRecordBase = HasRecordBase( containingType );

        var basePrintMembers =
            InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    BaseExpression(),
                    SyntaxFactoryEx.WellKnownIdentifierName( "PrintMembers" ) ),
                ArgumentList( SingletonSeparatedList( Argument( builder ) ) ) );

        if ( printableMembers.Count == 0 )
        {
            return hasRecordBase
                ? ( [], basePrintMembers )
                : ( [], LiteralExpression( SyntaxKind.FalseLiteralExpression ) );
        }

        var statements = new List<StatementSyntax>();

        if ( hasRecordBase )
        {
            statements.Add( IfStatement( basePrintMembers, Block( ExpressionStatement( AppendString( builder, ", " ) ) ) ) );
        }
        else if ( !containingType.IsValueType
                  && generationContext.CompilationContext.Compilation
                      .GetTypeByMetadataName( "System.Runtime.CompilerServices.RuntimeHelpers" ) is { } runtimeHelpers
                  && runtimeHelpers.GetMembers( "EnsureSufficientExecutionStack" ).Any() )
        {
            statements.Add(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            generationContext.SyntaxGenerator.TypeSyntax( runtimeHelpers ),
                            SyntaxFactoryEx.WellKnownIdentifierName( "EnsureSufficientExecutionStack" ) ),
                        ArgumentList() ) ) );
        }

        for ( var i = 0; i < printableMembers.Count; i++ )
        {
            var (name, memberType) = printableMembers[i];

            statements.Add( ExpressionStatement( AppendString( builder, i == 0 ? $"{name} = " : $", {name} = " ) ) );

            var memberAccess =
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), SyntaxFactoryEx.SafeIdentifierName( name ) );

            // The cast to object is required rather than cosmetic. Without it, a member of type char[] would bind to
            // StringBuilder.Append(char[]) and its content would be printed instead of the name of its type.
            var argument =
                memberType.IsValueType
                    ? InvocationExpression(
                        MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, memberAccess, SyntaxFactoryEx.WellKnownIdentifierName( "ToString" ) ),
                        ArgumentList() )
                    : CastToObject( memberAccess, nullable: false );

            statements.Add(
                ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, builder, SyntaxFactoryEx.WellKnownIdentifierName( "Append" ) ),
                        ArgumentList( SingletonSeparatedList( Argument( argument ) ) ) ) ) );
        }

        return (statements, LiteralExpression( SyntaxKind.TrueLiteralExpression ));
    }

    private static IReadOnlyList<StatementSyntax> GenerateDeconstruct( IMethodSymbol symbol )
    {
        var containingType = symbol.ContainingType;
        var statements = new List<StatementSyntax>();

        foreach ( var parameter in symbol.Parameters )
        {
            var property = containingType.GetMembers( parameter.Name ).OfType<IPropertySymbol>().FirstOrDefault()
                           ?? throw new AssertionFailedException( $"'{containingType}' has no positional property named '{parameter.Name}'." );

            statements.Add(
                ExpressionStatement(
                    AssignmentExpression(
                        SyntaxKind.SimpleAssignmentExpression,
                        SyntaxFactoryEx.SafeIdentifierName( parameter.Name ),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            ThisExpression(),
                            SyntaxFactoryEx.SafeIdentifierName( property.Name ) ) ) ) );
        }

        return statements;
    }

    private static ExpressionSyntax AppendString( ExpressionSyntax builder, string value )
        => InvocationExpression(
            MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, builder, SyntaxFactoryEx.WellKnownIdentifierName( "Append" ) ),
            ArgumentList( SingletonSeparatedList( Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( value ) ) ) ) ) );

    private static ExpressionSyntax AppendChar( ExpressionSyntax builder, char value )
        => InvocationExpression(
            MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, builder, SyntaxFactoryEx.WellKnownIdentifierName( "Append" ) ),
            ArgumentList( SingletonSeparatedList( Argument( SyntaxFactoryEx.LiteralExpression( value ) ) ) ) );

    private static ExpressionSyntax CastToObject( ExpressionSyntax expression, bool nullable )
    {
        TypeSyntax type = PredefinedType( Token( SyntaxKind.ObjectKeyword ) );

        return CastExpression( nullable ? NullableType( type ) : type, expression );
    }

    private static ExpressionSyntax CreateDefaultEqualityComparer( ITypeSymbol type, SyntaxGenerationContext generationContext )
    {
        var equalityComparerType = generationContext.CompilationContext.Compilation
            .GetTypeByMetadataName( "System.Collections.Generic.EqualityComparer`1" )
            .AssertNotNull();

        // EqualityComparer<string> and EqualityComparer<string?> are the same closed generic type at run time, and
        // constructing the annotated form avoids CS8604 on the arguments.
        var constructedType = equalityComparerType.Construct(
            ImmutableArray.Create( type ),
            ImmutableArray.Create( type.NullableAnnotation ) );

        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            generationContext.SyntaxGenerator.TypeSyntax( constructedType ),
            SyntaxFactoryEx.WellKnownIdentifierName( "Default" ) );
    }

    private static ExpressionSyntax CombineWithAnd( IReadOnlyList<ExpressionSyntax> operands )
    {
        var result = operands[0];

        for ( var i = 1; i < operands.Count; i++ )
        {
            result = BinaryExpression( SyntaxKind.LogicalAndExpression, result, operands[i] );
        }

        return result;
    }

    private static bool HasRecordBase( INamedTypeSymbol type )
        => type is { IsValueType: false, BaseType.IsRecord: true };

    /// <summary>
    /// Gets the name and type of the members that the synthesized <c>PrintMembers</c> prints, in the order of
    /// <c>INamedTypeSymbol.GetMembers</c>. That order is lexical, so the positional properties of a record
    /// precede the members declared in its body.
    /// </summary>
    private static IReadOnlyList<(string Name, ITypeSymbol Type)> GetPrintableMembers( INamedTypeSymbol type )
    {
        var members = new List<(string, ITypeSymbol)>();

        foreach ( var member in type.GetMembers() )
        {
            if ( member is { DeclaredAccessibility: not Accessibility.Public } or { IsStatic: true } )
            {
                continue;
            }

            switch ( member.Kind )
            {
                case SymbolKind.Field when member is IFieldSymbol field:
                    members.Add( (field.Name, field.Type) );

                    break;

                case SymbolKind.Property when member is IPropertySymbol { IsIndexer: false, IsOverride: false, GetMethod: not null } property:
                    members.Add( (property.Name, property.Type) );

                    break;
            }
        }

        return members;
    }

    /// <summary>
    /// Gets the instance fields that the synthesized <c>Equals</c> and <c>GetHashCode</c> iterate, in the order of
    /// <c>SourceMemberContainerSymbol.GetFieldsToEmit</c>. The order is observable in <c>GetHashCode</c>, so it is
    /// reproduced rather than re-derived. The backing field of a field-like event is deliberately absent from
    /// <c>INamedTypeSymbol.GetMembers</c>, so the event stands for it in its own position.
    /// </summary>
    private static IReadOnlyList<RecordField> GetFieldsToEmit( INamedTypeSymbol type, LinkerRewritingDriver rewritingDriver )
    {
        var fields = new List<RecordField>();

        foreach ( var member in type.GetMembers() )
        {
            switch ( member.Kind )
            {
                case SymbolKind.Field when member is IFieldSymbol { IsStatic: false, IsConst: false } field
                                           && field.AssociatedSymbol?.Kind != SymbolKind.Event:
                    fields.Add( CreateFieldFor( field, rewritingDriver ) );

                    break;

                case SymbolKind.Event when member is IEventSymbol { IsStatic: false, ExplicitInterfaceImplementations.IsEmpty: true } @event
                                           && @event.IsEventField() == true:
                    fields.Add( CreateFieldForEvent( @event, rewritingDriver ) );

                    break;
            }
        }

        return fields;
    }

    /// <summary>
    /// Gets the auto-properties of a record whose value the generated body of <c>Equals</c> and <c>GetHashCode</c> reads
    /// through the property, where the C# compiler reads the backing field.
    /// </summary>
    /// <param name="type">The record type.</param>
    /// <param name="hasMaterializedBackingField">
    /// Determines whether the linker emits an explicit backing field for a property, in which case the generated body reads
    /// that field and the property is not returned.
    /// </param>
    /// <remarks>
    /// The backing field of an auto-property has no name that can be written in source code, so the generated body reads the
    /// property instead. The two are equivalent, unless the property can be overridden by a derived type, or an aspect has
    /// replaced its implementation. The caller classifies the two cases and reports the corresponding warning.
    /// </remarks>
    public static IReadOnlyList<IPropertySymbol> GetAutoPropertiesReadThroughProperty(
        INamedTypeSymbol type,
        Predicate<IPropertySymbol> hasMaterializedBackingField )
    {
        List<IPropertySymbol>? properties = null;

        foreach ( var member in type.GetMembers() )
        {
            if ( member.Kind == SymbolKind.Field
                 && member is IFieldSymbol { IsStatic: false, IsConst: false, AssociatedSymbol.Kind: SymbolKind.Property } field
                 && field.AssociatedSymbol is IPropertySymbol property
                 && !hasMaterializedBackingField( property ) )
            {
                properties ??= [];
                properties.Add( property );
            }
        }

        return (IReadOnlyList<IPropertySymbol>?) properties ?? [];
    }

    /// <summary>
    /// Creates the description of the backing field of a field-like event. Inside the declaring type, the name of the event
    /// binds to that field, unless the linker replaces the event by a private event that carries the field.
    /// </summary>
    private static RecordField CreateFieldForEvent( IEventSymbol @event, LinkerRewritingDriver rewritingDriver )
    {
        var name = rewritingDriver.HasMaterializedBackingField( @event )
            ? rewritingDriver.GetBackingFieldName( @event )
            : @event.Name;

        return new RecordField( name, @event.Type );
    }

    private static RecordField CreateFieldFor( IFieldSymbol field, LinkerRewritingDriver rewritingDriver )
    {
        if ( field.AssociatedSymbol?.Kind == SymbolKind.Property && field.AssociatedSymbol is IPropertySymbol property )
        {
            // The backing field of an auto-property has no name that can be written in source. The linker emits an
            // explicit backing field when it rewrites the property; otherwise the property name is the only way to read it.
            var name = rewritingDriver.HasMaterializedBackingField( property )
                ? rewritingDriver.GetBackingFieldName( property )
                : property.Name;

            return new RecordField( name, property.Type );
        }

        return new RecordField( field.Name, field.Type );
    }

    /// <summary>
    /// A field of the record, identified by the name under which it can be read from source.
    /// </summary>
    private readonly struct RecordField( string name, ITypeSymbol type )
    {
        public ITypeSymbol Type { get; } = type;

        public ExpressionSyntax CreateAccess( ExpressionSyntax target )
            => MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, target, SyntaxFactoryEx.SafeIdentifierName( name ) );
    }
}
