// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.Introductions.Introduced;
using Metalama.Framework.Engine.CodeModel.Source;
using Metalama.Framework.Engine.Collections;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Accessibility = Metalama.Framework.Code.Accessibility;
using DeclarationKind = Metalama.Framework.Code.DeclarationKind;
using EnumerableExtensions = Metalama.Framework.Engine.Collections.EnumerableExtensions;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using OperatorKind = Metalama.Framework.Code.OperatorKind;
using RefKind = Metalama.Framework.Code.RefKind;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;

namespace Metalama.Framework.Engine.CodeModel.Helpers;

public static class DeclarationExtensions
{
    public static DeclarationKind GetDeclarationKind( this ISymbol symbol, CompilationContext compilationContext )
        => symbol switch
        {
            INamespaceSymbol => DeclarationKind.Namespace,
            INamedTypeSymbol => DeclarationKind.NamedType,
            IMethodSymbol method =>
                method.MethodKind switch
                {
                    MethodKind.Constructor or MethodKind.StaticConstructor => DeclarationKind.Constructor,
                    MethodKind.Destructor => DeclarationKind.Finalizer,
                    _ => DeclarationKind.Method
                },
            IPropertySymbol { Parameters.Length: 0 } => DeclarationKind.Property,
            IPropertySymbol { Parameters.Length: > 0 } => DeclarationKind.Indexer,
            IFieldSymbol => DeclarationKind.Field,
            ITypeParameterSymbol => DeclarationKind.TypeParameter,
            IAssemblySymbol assemblySymbol when assemblySymbol.Equals( compilationContext.Compilation.Assembly ) => DeclarationKind.Compilation,
            IAssemblySymbol => DeclarationKind.AssemblyReference,
            IParameterSymbol => DeclarationKind.Parameter,
            IEventSymbol => DeclarationKind.Event,
            ITypeSymbol => DeclarationKind.Type,
            IModuleSymbol => DeclarationKind.Compilation,
            _ => throw new ArgumentException( $"Unexpected symbol: {symbol.GetType().Name}.", nameof(symbol) )
        };

    /// <summary>
    /// Select all declarations recursively contained in a given declaration (i.e. all descendants of the tree).
    /// </summary>
    internal static IEnumerable<IDeclaration> GetContainedDeclarations( this IDeclaration declaration )
        => declaration.SelectManyRecursive(
            child => child switch
            {
                ICompilation compilation => [compilation.GlobalNamespace],
                INamespace ns => EnumerableExtensions.Concat<IDeclaration>( ns.Namespaces, ns.Types ),
                INamedType namedType => EnumerableExtensions.Concat<IDeclaration>(
                        namedType.Types,
                        namedType.Methods,
                        namedType.Constructors,
                        namedType.Fields,
                        namedType.Properties,
                        namedType.Indexers,
                        namedType.Events,
                        namedType.TypeParameters )
                    .ConcatNotNull( namedType.StaticConstructor )
                    .ConcatNotNull( namedType.Finalizer ),
                IMethod method => Enumerable
                    .Concat<IDeclaration>( method.Parameters, method.TypeParameters )
                    .ConcatNotNull( method.ReturnParameter ),
                IIndexer indexer => indexer.Parameters.Concat<IDeclaration>( indexer.Accessors ),
                IConstructor constructor => constructor.Parameters,
                IHasAccessors member => member.Accessors,
                _ => []
            } );

    internal static ISymbol? GetSymbol( this IDeclaration declaration, CompilationContext compilationContext )
        => declaration.GetSymbol() is { } symbol
            ? compilationContext.SymbolTranslator.Translate( symbol, declaration.GetCompilationModel().RoslynCompilation )
            : null;

    internal static Location? GetDiagnosticLocation( this IDeclaration declaration )
        => declaration switch
        {
            ISdkDeclaration hasLocation => hasLocation.DiagnosticLocation,
            _ => null
        };

    private static void CheckArguments( this IDeclaration declaration, IReadOnlyList<IParameter> parameters, TypedExpressionSyntaxImpl[]? arguments )
    {
        // TODO: somehow provide locations for the diagnostics?
        var argumentsLength = arguments?.Length ?? 0;

        if ( parameters.LastOrDefault()?.IsParams == true )
        {
            // all non-params arguments have to be set + any number of params arguments
            var requiredArguments = parameters.Count - 1;

            if ( argumentsLength < requiredArguments )
            {
                throw GeneralDiagnosticDescriptors.MemberRequiresAtLeastNArguments.CreateException( (declaration, requiredArguments, argumentsLength) );
            }
        }
        else
        {
            if ( argumentsLength != parameters.Count )
            {
                throw GeneralDiagnosticDescriptors.MemberRequiresNArguments.CreateException( (declaration, parameters.Count, argumentsLength) );
            }
        }
    }

    internal static ArgumentSyntax[] GetArguments(
        this IDeclaration declaration,
        IReadOnlyList<IParameter> parameters,
        TypedExpressionSyntaxImpl[]? args,
        SyntaxGenerationContext syntaxGenerationContext )
    {
        CheckArguments( declaration, parameters, args );

        if ( args == null || args.Length == 0 )
        {
            return [];
        }

        var arguments = new List<ArgumentSyntax>( args.Length );

        for ( var i = 0; i < args.Length; i++ )
        {
            var arg = args[i];

            ArgumentSyntax argument;

            if ( i >= parameters.Count || parameters[i].IsParams )
            {
                // params methods can be called as params or directly with an array
                // so it's probably best to not do any type-checking for them

                argument = SyntaxFactory.Argument( arg.Syntax );
            }
            else
            {
                var parameter = parameters[i];

                if ( parameter.RefKind is RefKind.Out or RefKind.Ref or RefKind.RefReadOnly )
                {
                    SyntaxKind refKindKeyword;

                    if ( parameter.RefKind is RefKind.RefReadOnly )
                    {
                        // `ref readonly` parameters can be called with the `ref` or `in` modifier if the argument is a mutable variable/`ref` expression,
                        // but only with `in` if it's a read-only variable/`readonly ref`.
                        // If the argument is not a `ref` or variable, no modifier is possible and the code produces a warning.

                        refKindKeyword = arg.IsReferenceable == true ? SyntaxKind.InKeyword : SyntaxKind.None;
                    }
                    else
                    {
                        // With out and ref parameters, we unconditionally add the out or ref modifier, and "hope" the code will later compile.
                        // We also intentionally omit to cast the value since it would be illegal.

                        if ( arg.IsReferenceable == false )
                        {
                            // If we know that the expression is not referenceable, throw. If have a doubt, assume it is.
                            
                            throw new DiagnosticException(
                                GeneralDiagnosticDescriptors.CannotPassExpressionToByRefParameter.CreateRoslynDiagnostic(
                                    null,
                                    (arg.Syntax.ToString(), parameter.Name, parameter.DeclaringMember) ) );
                        }

                        refKindKeyword = parameter.RefKind is RefKind.Ref ? SyntaxKind.RefKeyword : SyntaxKind.OutKeyword;
                    }

                    argument = SyntaxFactory.Argument( null, SyntaxFactory.Token( refKindKeyword ), arg.Syntax );
                }
                else
                {
                    argument = SyntaxFactory.Argument( arg.Convert( parameter.Type, syntaxGenerationContext ).Syntax.RemoveParenthesis() );
                }
            }

            arguments.Add( argument );
        }

        return arguments.ToArray();
    }

    internal static ExpressionSyntax GetReceiverSyntax<T>(
        this T declaration,
        TypedExpressionSyntaxImpl instance,
        SyntaxSerializationContext context )
        where T : IMember
    {
        if ( declaration.IsStatic )
        {
            return context.SyntaxGenerator.Type( declaration.DeclaringType ).WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
        }

        var definition = declaration.Definition;

        if ( definition.IsExplicitInterfaceImplementation )
        {
            return
                SyntaxFactory.ParenthesizedExpression(
                        SyntaxFactory.CastExpression(
                            context.SyntaxGenerator.Type( definition.GetExplicitInterfaceImplementation().DeclaringType ),
                            instance.Syntax ) )
                    .WithSimplifierAnnotationIfNecessary( context.SyntaxGenerationContext );
        }

        return instance.Convert( declaration.DeclaringType, context.SyntaxGenerationContext ).Syntax;
    }

    /// <summary>
    /// Converts Roslyn <see cref="Microsoft.CodeAnalysis.RefKind"/> to Metalama <see cref="RefKind"/> for members and return parameters.
    /// Note that the conversion for parameters is different.
    /// </summary>
    internal static RefKind ToOurRefKind( this Microsoft.CodeAnalysis.RefKind roslynRefKind )
        => roslynRefKind switch
        {
            Microsoft.CodeAnalysis.RefKind.None => RefKind.None,
            Microsoft.CodeAnalysis.RefKind.Ref => RefKind.Ref,
            Microsoft.CodeAnalysis.RefKind.RefReadOnly => RefKind.RefReadOnly,
            _ => throw new InvalidOperationException( $"Roslyn RefKind {roslynRefKind} not recognized here." )
        };

    internal static Accessibility ToOurAccessibility( this Microsoft.CodeAnalysis.Accessibility accessibility )
        => accessibility switch
        {
            Microsoft.CodeAnalysis.Accessibility.NotApplicable => Accessibility.Private,
            Microsoft.CodeAnalysis.Accessibility.Private => Accessibility.Private,
            Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => Accessibility.PrivateProtected,
            Microsoft.CodeAnalysis.Accessibility.Protected => Accessibility.Protected,
            Microsoft.CodeAnalysis.Accessibility.Internal => Accessibility.Internal,
            Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => Accessibility.ProtectedInternal,
            Microsoft.CodeAnalysis.Accessibility.Public => Accessibility.Public,
            _ => throw new ArgumentOutOfRangeException()
        };

    internal static Code.MethodKind ToOurMethodKind( this MethodKind methodKind )
        => methodKind switch
        {
            MethodKind.Ordinary => Code.MethodKind.Default,
            MethodKind.Destructor => Code.MethodKind.Finalizer,
            MethodKind.PropertyGet => Code.MethodKind.PropertyGet,
            MethodKind.PropertySet => Code.MethodKind.PropertySet,
            MethodKind.EventAdd => Code.MethodKind.EventAdd,
            MethodKind.EventRemove => Code.MethodKind.EventRemove,
            MethodKind.EventRaise => Code.MethodKind.EventRaise,
            MethodKind.ExplicitInterfaceImplementation => Code.MethodKind.ExplicitInterfaceImplementation,
            MethodKind.Conversion => Code.MethodKind.Operator,
            MethodKind.UserDefinedOperator => Code.MethodKind.Operator,
            MethodKind.LocalFunction => Code.MethodKind.LocalFunction,
            MethodKind.AnonymousFunction => Code.MethodKind.Lambda,
            MethodKind.DelegateInvoke => Code.MethodKind.DelegateInvoke,
            _ => throw new AssertionFailedException( $"The method kind '{methodKind}' is not expected." )
        };

    internal static string ToDisplayString( this DeclarationKind kind )
        => kind switch
        {
            DeclarationKind.TypeParameter => "generic parameter",
            DeclarationKind.NamedType => "type",
            DeclarationKind.AssemblyReference => "assembly reference",
            _ => kind.ToString().ToLowerInvariant()
        };

    internal static SyntaxKind ToOperatorKeyword( this OperatorKind operatorKind )
        => operatorKind switch
        {
            OperatorKind.ImplicitConversion => SyntaxKind.ImplicitKeyword,
            OperatorKind.ExplicitConversion => SyntaxKind.ExplicitKeyword,
            OperatorKind.Addition => SyntaxKind.PlusToken,
            OperatorKind.BitwiseAnd => SyntaxKind.AmpersandToken,
            OperatorKind.BitwiseOr => SyntaxKind.BarToken,
            OperatorKind.Decrement => SyntaxKind.MinusMinusToken,
            OperatorKind.Division => SyntaxKind.SlashToken,
            OperatorKind.Equality => SyntaxKind.EqualsEqualsToken,
            OperatorKind.ExclusiveOr => SyntaxKind.CaretToken,
            OperatorKind.False => SyntaxKind.FalseKeyword,
            OperatorKind.GreaterThan => SyntaxKind.GreaterThanToken,
            OperatorKind.GreaterThanOrEqual => SyntaxKind.GreaterThanEqualsToken,
            OperatorKind.Increment => SyntaxKind.PlusPlusToken,
            OperatorKind.Inequality => SyntaxKind.ExclamationEqualsToken,
            OperatorKind.LeftShift => SyntaxKind.LessThanLessThanToken,
            OperatorKind.LessThan => SyntaxKind.LessThanToken,
            OperatorKind.LessThanOrEqual => SyntaxKind.LessThanEqualsToken,
            OperatorKind.LogicalNot => SyntaxKind.ExclamationToken,
            OperatorKind.Modulus => SyntaxKind.PercentToken,
            OperatorKind.Multiply => SyntaxKind.AsteriskToken,
            OperatorKind.OnesComplement => SyntaxKind.TildeToken,
            OperatorKind.RightShift => SyntaxKind.GreaterThanGreaterThanToken,
            OperatorKind.Subtraction => SyntaxKind.MinusToken,
            OperatorKind.True => SyntaxKind.TrueKeyword,
            OperatorKind.UnaryNegation => SyntaxKind.MinusToken,
            OperatorKind.UnaryPlus => SyntaxKind.PlusToken,
            _ => throw new AssertionFailedException( $"Unexpected OperatorKind: {operatorKind}." )
        };

    internal static string ToOperatorMethodName( this OperatorKind operatorKind )
        => operatorKind switch
        {
            OperatorKind.ImplicitConversion => WellKnownMemberNames.ImplicitConversionName,
            OperatorKind.ExplicitConversion => WellKnownMemberNames.ExplicitConversionName,
            OperatorKind.Addition => WellKnownMemberNames.AdditionOperatorName,
            OperatorKind.BitwiseAnd => WellKnownMemberNames.BitwiseAndOperatorName,
            OperatorKind.BitwiseOr => WellKnownMemberNames.BitwiseOrOperatorName,
            OperatorKind.Decrement => WellKnownMemberNames.DecrementOperatorName,
            OperatorKind.Division => WellKnownMemberNames.DivisionOperatorName,
            OperatorKind.Equality => WellKnownMemberNames.EqualityOperatorName,
            OperatorKind.ExclusiveOr => WellKnownMemberNames.ExclusiveOrOperatorName,
            OperatorKind.False => WellKnownMemberNames.FalseOperatorName,
            OperatorKind.GreaterThan => WellKnownMemberNames.GreaterThanOperatorName,
            OperatorKind.GreaterThanOrEqual => WellKnownMemberNames.GreaterThanOrEqualOperatorName,
            OperatorKind.Increment => WellKnownMemberNames.IncrementOperatorName,
            OperatorKind.Inequality => WellKnownMemberNames.InequalityOperatorName,
            OperatorKind.LeftShift => WellKnownMemberNames.LeftShiftOperatorName,
            OperatorKind.LessThan => WellKnownMemberNames.LessThanOperatorName,
            OperatorKind.LessThanOrEqual => WellKnownMemberNames.LessThanOrEqualOperatorName,
            OperatorKind.LogicalNot => WellKnownMemberNames.LogicalNotOperatorName,
            OperatorKind.Modulus => WellKnownMemberNames.ModulusOperatorName,
            OperatorKind.Multiply => WellKnownMemberNames.MultiplyOperatorName,
            OperatorKind.OnesComplement => WellKnownMemberNames.OnesComplementOperatorName,
            OperatorKind.RightShift => WellKnownMemberNames.RightShiftOperatorName,
            OperatorKind.Subtraction => WellKnownMemberNames.SubtractionOperatorName,
            OperatorKind.True => WellKnownMemberNames.TrueOperatorName,
            OperatorKind.UnaryNegation => WellKnownMemberNames.UnaryNegationOperatorName,
            OperatorKind.UnaryPlus => WellKnownMemberNames.UnaryPlusOperatorName,
            _ => throw new AssertionFailedException( $"Unexpected OperatorKind: {operatorKind}." )
        };

    internal static bool? IsAutoProperty( this IPropertySymbol symbol )
        => symbol switch
        {
            { IsAbstract: true } => false,
#if ROSLYN_4_12_0_OR_GREATER
            { IsPartialDefinition: true } => false, // Partial property can't be implemented as an auto-property.
#endif
            { DeclaringSyntaxReferences: { Length: > 0 } syntaxReferences } =>
                syntaxReferences.All(
                    sr =>
                        sr.GetSyntax() switch
                        {
                            BasePropertyDeclarationSyntax { AccessorList.Accessors: { Count: > 0 } accessors } when
                                accessors.All( a => a.Body == null && a.ExpressionBody == null ) => true,
                            ParameterSyntax => true,
                            _ => false
                        } ),
            { GetMethod: { } getMethod } => getMethod.IsCompilerGenerated(),
            { SetMethod: { } setMethod } => setMethod.IsCompilerGenerated(),
            _ => null
        };

    internal static bool IsAutoAccessor( this IMethodSymbol symbol )
        => symbol switch
        {
            { IsAbstract: true } => false,
            { DeclaringSyntaxReferences: { Length: > 0 } syntaxReferences } =>
                syntaxReferences.All( sr => sr.GetSyntax() is AccessorDeclarationSyntax { Body: null, ExpressionBody: null } ),
            _ => symbol.IsCompilerGenerated()
        };

    internal static bool? IsEventField( this IEventSymbol symbol )
        => symbol switch
        {
            { IsAbstract: true } => false,
            { DeclaringSyntaxReferences.Length: > 0 } =>
                symbol.DeclaringSyntaxReferences.All( sr => sr.GetSyntax() is VariableDeclaratorSyntax ),
            { AddMethod: { } addMethod, RemoveMethod: { } removeMethod } => addMethod.IsCompilerGenerated() && removeMethod.IsCompilerGenerated(),
            _ => null
        };

    internal static bool? HasInitializer( this IPropertySymbol symbol )
        => symbol switch
        {
            { DeclaringSyntaxReferences.Length: > 0 } =>
                symbol.DeclaringSyntaxReferences.Any( p => p.GetSyntax().AssertCast<PropertyDeclarationSyntax>().Initializer != null ),
            _ => null
        };

    internal static bool? HasInitializer( this IEventSymbol symbol )
        => symbol switch
        {
            { DeclaringSyntaxReferences.Length: > 0 } =>
                symbol.DeclaringSyntaxReferences.Any( v => v.GetSyntax().AssertCast<VariableDeclaratorSyntax>().Initializer != null ),
            _ => null
        };

    internal static IMember GetExplicitInterfaceImplementation( this IMember member )
    {
        switch ( member )
        {
            case IMethod method:
                return method.ExplicitInterfaceImplementations.Single();

            case IProperty property:
                return property.ExplicitInterfaceImplementations.Single();

            case IEvent @event:
                return @event.ExplicitInterfaceImplementations.Single();

            default:
                throw new AssertionFailedException( $"Unexpected member type: {member.GetType()}." );
        }
    }

    public static ImmutableArray<SyntaxReference> GetDeclaringSyntaxReferences( this IDeclaration declaration )
        => ((IDeclarationImpl) declaration).DeclaringSyntaxReferences;

    /// <summary>
    /// Finds a method of given signature that is visible in the specified type, taking into account methods being hidden by other methods.
    /// </summary>
    /// <param name="namedType">Type.</param>
    /// <param name="signatureTemplate">Method that acts as a template for the signature.</param>
    /// <returns>A method of the given signature that is visible from the given type or <c>null</c> if no such method exists.</returns>
    internal static IMethod? FindClosestVisibleMethod( this INamedType namedType, IMethod signatureTemplate )
        => namedType.AllMethods.OfExactSignature( signatureTemplate, false );

    /// <summary>
    /// Finds a method of given signature that is visible in the specified type, taking into account methods being hidden by other methods.
    /// </summary>
    /// <param name="namedType">Type.</param>
    /// <param name="signatureTemplate">Method that acts as a template for the signature.</param>
    /// <returns>A method of the given signature that is visible from the given type or <c>null</c> if no such method exists.</returns>
    internal static IIndexer? FindClosestVisibleIndexer( this INamedType namedType, IIndexer signatureTemplate )
        => namedType.AllIndexers.OfExactSignature( signatureTemplate );

    /// <summary>
    /// Finds a parameterless member in the given type and parent type, taking into account member hiding.
    /// </summary>
    /// <param name="namedType">Type.</param>
    /// <param name="name">Member name.</param>
    /// <returns>A property of the given signature that is visible from the given type or <c>null</c> if no such property exists.</returns>
    internal static IMember? FindClosestUniquelyNamedMember(
        this INamedType namedType,
        string name )
        => namedType.AllProperties.OfName( name ).FirstOrDefault() ??
           (IMember?) namedType.AllFields.OfName( name ).FirstOrDefault() ??
           namedType.AllEvents.OfName( name ).FirstOrDefault();

    internal static bool? IsEventField( this IEvent @event )
    {
        switch ( @event )
        {
            case SourceEvent codeEvent:
                var eventSymbol = codeEvent.GetSymbol().AssertSymbolNotNull();

                return eventSymbol.IsEventField();

            case IntroducedEvent builtEvent:
                return builtEvent.EventBuilderData.IsEventField;

            case EventBuilder eventBuilder:
                return eventBuilder.IsEventField;

            default:
                throw new AssertionFailedException( $"{@event} is not supported" );
        }
    }

    internal static bool TryGetHiddenDeclaration( this IMemberOrNamedType declaration, [NotNullWhen( true )] out IMemberOrNamedType? hiddenDeclaration )
    {
        if ( declaration is IMember { IsOverride: true } )
        {
            // Override symbol never hides anything.
            hiddenDeclaration = null;

            return false;
        }

        var currentType = declaration.DeclaringType?.BaseType;

        while ( currentType != null )
        {
            switch ( declaration )
            {
                case IFieldOrProperty or IEvent or INamedType:
                    // Field/properties/events are matched by name. When a base method is hidden, we ignore it (as it may be still accessible).
                    var candidateMember =
                        currentType.Fields.OfName( declaration.Name ).FirstOrDefault()
                        ?? currentType.Properties.OfName( declaration.Name ).FirstOrDefault()
                        ?? currentType.Events.OfName( declaration.Name ).FirstOrDefault()
                        ?? currentType.Types.OfName( declaration.Name ).FirstOrDefault()
                        ?? (IMemberOrNamedType?) currentType.Methods.OfName( declaration.Name ).FirstOrDefault();

                    if ( candidateMember != null )
                    {
                        hiddenDeclaration = candidateMember;

                        return true;
                    }

                    break;

                case IIndexer indexer:
                    // Indexers are matched by signature.
                    var candidateIndexer = currentType.Indexers.OfExactSignature( indexer );

                    if ( candidateIndexer != null )
                    {
                        hiddenDeclaration = candidateIndexer;

                        return true;
                    }

                    // No need to look for other declaration types as indexers cannot hide them.

                    break;

                case IMethod method:
                    // Methods are matched by signature.
                    var candidateMethod = currentType.Methods.OfExactSignature( method );

                    if ( candidateMethod != null )
                    {
                        hiddenDeclaration = candidateMethod;

                        return true;
                    }

                    var candidateNonMethod =
                        currentType.Fields.OfName( declaration.Name ).FirstOrDefault()
                        ?? currentType.Properties.OfName( declaration.Name ).FirstOrDefault()
                        ?? currentType.Events.OfName( declaration.Name ).FirstOrDefault()
                        ?? (IMemberOrNamedType?) currentType.Types.OfName( declaration.Name ).FirstOrDefault();

                    if ( candidateNonMethod != null )
                    {
                        hiddenDeclaration = candidateNonMethod;

                        return true;
                    }

                    break;

                default:
                    hiddenDeclaration = null;

                    return false;
            }

            currentType = currentType.BaseType;
        }

        hiddenDeclaration = null;

        return false;
    }

    internal static bool IsImplicitInstanceConstructor( this IConstructor constructor )
        => constructor is { IsStatic: false, IsImplicitlyDeclared: true, IsPrimary: false, Parameters: [] };

    internal static int GetDepthImpl( this IDeclaration declaration ) => declaration.GetCompilationModel().GetDepth( declaration );

    internal static T Translate<T>(
        this T declaration,
        ICompilation newCompilation )
        where T : class, IDeclaration
        => declaration.Compilation == newCompilation
            ? declaration
            : ((CompilationModel) newCompilation).Factory.Translate( declaration ).AssertNotNull();

    /// <summary>
    /// Version of <see cref="IDeclaration.ContainingDeclaration"/> that behaves like <see cref="ISymbol.ContainingSymbol"/>,
    /// i.e. returns containing namespace for top-level types.
    /// </summary>
    internal static IDeclaration? GetContainingDeclarationOrNamespace( this IDeclaration declaration )
        => declaration.ContainingDeclaration is IAssembly && declaration is INamedType namedType
            ? namedType.ContainingNamespace
            : declaration.ContainingDeclaration;

    internal static bool IsNullableReferenceType( this IType type ) => type is { IsNullable: true, IsReferenceType: not false };

    internal static bool IsNullableValueType( this IType type ) => type is { IsNullable: true, IsReferenceType: false };

    internal static INamedType? GetBaseType( this IType type )
        => type switch
        {
            INamedType namedType => namedType.BaseType,
            IArrayType => (INamedType) type.GetCompilationModel().Factory.GetTypeByReflectionType( typeof(Array) ),
            _ => null
        };

    internal static IEnumerable<INamedType> GetImplementedInterfaces( this IType type )
        => type switch
        {
            INamedType namedType => namedType.ImplementedInterfaces,
            IArrayType { Rank: 1 } arrayType => SymbolHelpers.ArrayGenericInterfaces.Select(
                definitionSpecialType => type.GetCompilationModel()
                    .Factory
                    .GetNamedType( type.GetCompilationModel().RoslynCompilation.GetSpecialType( definitionSpecialType ) )
                    .WithTypeArguments( arrayType.ElementType ) ),
            _ => []
        };
}