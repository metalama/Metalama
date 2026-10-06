// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using RefKind = Microsoft.CodeAnalysis.RefKind;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.Extensibility.CallSites
{
    public sealed partial class ExtensionTransformationFactory
    {
        /// <summary>
        /// The prefix of the name of the class that contains the forwarders of a project. The sanitized name of the assembly follows it.
        /// </summary>
        internal const string ForwarderClassNamePrefix = "__MetalamaCallSites_";

        /// <summary>
        /// The path of the syntax tree that contains the forwarders of a project.
        /// </summary>
        internal const string ForwarderSyntaxTreePath = "MetalamaCallSites.cs";

        /// <summary>
        /// The forwarders created for the call sites in a conditional access, indexed by the definition of their target method. The caller must
        /// hold <see cref="_sync"/>.
        /// </summary>
        private readonly Dictionary<IMethodSymbol, CallSiteForwarder> _forwarders = new( SymbolEqualityComparer.Default );

        /// <summary>
        /// Returns the callee of a call site in a conditional access that is redirected to a static method: the name of the forwarder of the target,
        /// with explicit type arguments when the forwarder is generic. The forwarder is created when the target has none yet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The receiver of a call in a conditional access exists only inside the conditional access, so the call cannot be written in the static form
        /// <c>T.M( a, x )</c>. The call is written as a call of an extension method, <c>a?.F( x )</c>, where <c>F</c> is a forwarder that the linker
        /// generates in one internal static class of the global namespace and that calls the target with its fully qualified name. The receiver stays in
        /// the conditional access, so the compiler evaluates it once, does not evaluate the arguments when it is null, and lifts the result. No
        /// directive is added to the file of the call site, because the extension methods of a class of the global namespace are in scope in every
        /// file.
        /// </para>
        /// <para>
        /// The forwarder has the parameters of the target, and its first parameter is the receiver. Overload resolution among the forwarders of the
        /// overloads of a method is therefore the same as among the overloads, so the binding is verified on the static form of the call, with the
        /// receiver replaced by <c>default(T)</c>. Only the conversion of the receiver differs: an extension method accepts a receiver that converts to
        /// its first parameter by an identity, reference or boxing conversion only.
        /// </para>
        /// </remarks>
        private SimpleNameSyntax GetForwarderCallee(
            InvocationRedirectionRequest request,
            IInvocationOperation operation,
            SemanticModel semanticModel,
            ArgumentListSyntax rewrittenArguments,
            SyntaxGenerationContext context )
        {
            var callSite = (InvocationExpressionSyntax) operation.Syntax;
            var targetMethod = request.Target.Method;

            if ( targetMethod.GetSymbol() is not { } targetSymbol )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through a forwarder, but the target method '{targetMethod}' is introduced by an aspect and cannot be forwarded.",
                    nameof(request) );
            }

            var definition = targetSymbol.OriginalDefinition;

            if ( !semanticModel.Compilation.IsSymbolAccessibleWithin( definition, semanticModel.Compilation.Assembly ) )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through a forwarder, which is a top-level class, but the target method '{targetMethod}' is not accessible from such a class.",
                    nameof(request) );
            }

            ValidateForwardable( definition, callSite, targetMethod );

            if ( !TryGetForwarderReceiver( semanticModel, callSite, context, out var receiver, out var receiverType ) )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access whose receiver type cannot be named, so the call of the forwarder cannot be verified.",
                    nameof(request) );
            }

            // Bind the static form of the call, with the receiver as the first argument.
            var staticCall = InvocationExpression(
                CreateStaticCallee( request.Target, request.TypeArguments, context ),
                ArgumentList( SeparatedList( rewrittenArguments.Arguments.Insert( 0, Argument( receiver ) ) ) ) );

            var symbolInfo = semanticModel.GetSpeculativeSymbolInfo( callSite.SpanStart, staticCall, SpeculativeBindingOption.BindAsExpression );

            if ( symbolInfo.Symbol is not IMethodSymbol boundMethod
                 || !SymbolEqualityComparer.Default.Equals( boundMethod.OriginalDefinition, definition ) )
            {
                throw new ArgumentException(
                    symbolInfo.Symbol != null
                        ? $"The rewritten call binds to '{symbolInfo.Symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' instead of '{targetMethod}'."
                        : $"The rewritten call does not bind to '{targetMethod}' ({symbolInfo.CandidateReason}).",
                    nameof(request) );
            }

            var receiverParameterType = boundMethod.Parameters[0].Type;
            var conversion = semanticModel.Compilation.ClassifyConversion( receiverType, receiverParameterType );

            if ( !(conversion.IsIdentity || (conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing))) )
            {
                throw new ArgumentException(
                    $"The receiver of type '{receiverType.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' converts to the first parameter of '{targetMethod}' by neither an identity, a reference nor a boxing conversion, so it cannot be the receiver of the forwarder of the target.",
                    nameof(request) );
            }

            // The forwarder does not exist yet, so any symbol that the lookup of its name finds on the receiver would make the call ambiguous or bind
            // it to another method. This happens with the forwarders of a project that grants access to its internal members with InternalsVisibleTo.
            var forwarderName = GetForwarderName( definition );

            if ( !semanticModel.LookupSymbols( callSite.SpanStart, receiverType, forwarderName, includeReducedExtensionMethods: true ).IsEmpty )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through the forwarder '{forwarderName}', but this name already designates another member at the call site, for instance the forwarder of a project that grants access to its internal members.",
                    nameof(request) );
            }

            var name = this.GetOrAddForwarder( definition, context, callSite, targetMethod );

            // A generic forwarder is called with explicit type arguments, which are those of the static form of the call, so that the call does not
            // depend on the inference of the type parameters of the declaring type of the target.
            var typeArguments = GetContainingTypeArguments( boundMethod.ContainingType ).Concat( boundMethod.TypeArguments ).ToList();

            if ( typeArguments.Count == 0 )
            {
                return SyntaxFactoryEx.SafeIdentifierName( name );
            }

            if ( !typeArguments.All( CanBeNamed ) )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through a forwarder, but a type argument of '{targetMethod}' cannot be named.",
                    nameof(request) );
            }

            return GenericName(
                SyntaxFactoryEx.SafeIdentifier( name ),
                TypeArgumentList( SeparatedList( typeArguments.SelectAsArray( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
        }

        /// <summary>
        /// Throws an <see cref="ArgumentException"/> when the signature of a target method cannot be copied to a forwarder.
        /// </summary>
        private static void ValidateForwardable( IMethodSymbol definition, InvocationExpressionSyntax callSite, IMethod targetMethod )
        {
            string? reason = null;

            if ( definition.MethodKind != MethodKind.Ordinary )
            {
                reason = "it is not an ordinary method";
            }
            else if ( definition.ReturnsByRef || definition.ReturnsByRefReadonly )
            {
                reason = "it returns by reference";
            }
            else if ( definition.Parameters.Length == 0 || definition.Parameters[0].RefKind != RefKind.None || definition.Parameters[0].IsParams )
            {
                reason = "its first parameter, which receives the receiver, is not a parameter passed by value";
            }
            else if ( definition.Parameters.Any( p => p.ScopedKind != ScopedKind.None ) )
            {
                reason = "a parameter is scoped";
            }
            else if ( definition.Parameters.Any( p => p.IsOptional && !p.HasExplicitDefaultValue ) )
            {
                reason = "an optional parameter has no explicit default value";
            }
            else if ( definition.IsVararg )
            {
                reason = "it has a variable argument list";
            }
            else
            {
                var typeParameterNames = GetContainingTypeParameters( definition.ContainingType ).Concat( definition.TypeParameters ).Select( p => p.Name ).ToList();

                if ( typeParameterNames.Distinct( StringComparer.Ordinal ).Count() != typeParameterNames.Count )
                {
                    reason = "two of its type parameters, including those of its declaring types, have the same name";
                }
            }

            if ( reason != null )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through a forwarder, but the target method '{targetMethod}' cannot be forwarded because {reason}.",
                    "request" );
            }
        }

        /// <summary>
        /// Gets an expression that can replace the receiver of a call site in a conditional access, outside of the conditional access, and the type of
        /// the receiver.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The receiver of the conditional access is replaced by <c>default(T)</c>, where <c>T</c> is its type as it is seen inside the conditional
        /// access: the underlying type of a nullable value type, and a reference type without nullable annotation. This gives the same member lookup
        /// and the same overload resolution. In <c>a?.M()</c>, the receiver of the call is <c>default(T)</c>. In <c>a?.B.M()</c>, it is
        /// <c>default(T).B</c>.
        /// </para>
        /// </remarks>
        /// <returns><c>false</c> when <c>T</c> cannot be determined or cannot be named.</returns>
        private static bool TryGetForwarderReceiver(
            SemanticModel semanticModel,
            InvocationExpressionSyntax callSite,
            SyntaxGenerationContext context,
            out ExpressionSyntax receiver,
            out ITypeSymbol receiverType )
        {
            receiver = null!;
            receiverType = null!;

            var binding = GetConditionalAccessBinding( callSite.Expression );

            if ( binding?.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>() is not { } conditionalAccess
                 || semanticModel.GetTypeInfo( conditionalAccess.Expression ).Type is not { } type )
            {
                return false;
            }

            if ( type.Kind == SymbolKind.NamedType && type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T )
            {
                type = ((INamedTypeSymbol) type).TypeArguments[0];
            }

            type = type.WithNullableAnnotation( NullableAnnotation.NotAnnotated );

            if ( !CanBeNamed( type ) )
            {
                return false;
            }

            var defaultReceiver = DefaultExpression( context.SyntaxGenerator.TypeSyntax( type ) );

            if ( callSite.Expression == binding )
            {
                receiver = defaultReceiver;
                receiverType = type;

                return true;
            }

            if ( callSite.Expression is not MemberAccessExpressionSyntax { Expression: var receiverSyntax }
                 || semanticModel.GetTypeInfo( receiverSyntax ).Type is not { } chainType )
            {
                return false;
            }

            ExpressionSyntax replacement = binding.Kind() == SyntaxKind.MemberBindingExpression
                ? MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, defaultReceiver, ((MemberBindingExpressionSyntax) binding).Name )
                : ElementAccessExpression( defaultReceiver, ((ElementBindingExpressionSyntax) binding).ArgumentList );

            receiver = receiverSyntax == binding ? replacement : receiverSyntax.ReplaceNode( binding, replacement );
            receiverType = chainType;

            return true;
        }

        /// <summary>
        /// Returns the name of the forwarder of a target method, and creates the forwarder when the target has none yet.
        /// </summary>
        /// <exception cref="ArgumentException">Another target method has a forwarder with the same name and the same parameters.</exception>
        private string GetOrAddForwarder( IMethodSymbol definition, SyntaxGenerationContext context, InvocationExpressionSyntax callSite, IMethod targetMethod )
        {
            lock ( this._sync )
            {
                this.ThrowIfCompleted();

                if ( this._forwarders.TryGetValue( definition, out var existing ) )
                {
                    return existing.Name;
                }

                var name = GetForwarderName( definition );
                var signature = GetForwarderSignature( name, definition );

                if ( this._forwarders.Values.Any( f => f.Signature == signature ) )
                {
                    throw new ArgumentException(
                        $"The call site '{callSite}' is in a conditional access, so it is redirected through a forwarder, but the forwarder of the target method '{targetMethod}' would have the same name and the same parameters as the forwarder of another method.",
                        "request" );
                }

                var forwarder = new CallSiteForwarder( name, signature, CreateForwarderDeclaration( name, definition, targetMethod, context ) );
                this._forwarders.Add( definition, forwarder );

                return name;
            }
        }

        /// <summary>
        /// Creates the compilation unit that declares the forwarders, or returns <c>null</c> when no forwarder was created. The caller must hold
        /// <see cref="_sync"/>.
        /// </summary>
        /// <remarks>
        /// The class is in the global namespace, so that its extension methods are in scope in every file without a directive, and it is internal.
        /// Its name contains the name of the assembly, so that the classes of two projects linked by <c>InternalsVisibleTo</c> have distinct names.
        /// The forwarders are sorted by signature, so that the generated code does not depend on the order of the requests.
        /// </remarks>
        private CompilationUnitSyntax? CreateForwarderCompilationUnit()
        {
            if ( this._forwarders.Count == 0 )
            {
                return null;
            }

            var className = ForwarderClassNamePrefix + SanitizeIdentifier( this._compilation.RoslynCompilation.AssemblyName ?? "" );

            // The nullable annotations of the signatures are kept when the project enables nullable reference types.
            var leadingTrivia = this._compilation.RoslynCompilation.Options.NullableContextOptions != NullableContextOptions.Disable
                ? TriviaList( Trivia( NullableDirectiveTrivia( Token( SyntaxKind.EnableKeyword ), true ) ) )
                : default;

            // The class and its methods are hidden from IntelliSense, because the methods are extension methods in the global namespace and would
            // otherwise be proposed on every receiver type.
            var classDeclaration = ClassDeclaration(
                SingletonList( CreateEditorBrowsableNeverAttributeList( leadingTrivia ) ),
                TokenList( Token( SyntaxKind.InternalKeyword ), Token( SyntaxKind.StaticKeyword ) ),
                SyntaxFactoryEx.SafeIdentifier( className ),
                null,
                null,
                default,
                List<MemberDeclarationSyntax>( this._forwarders.Values.OrderBy( f => f.Signature, StringComparer.Ordinal ).Select( f => f.Declaration ) ) );

            // The class is created once per project, after all the requests, and it is not formatted by the linker.
#pragma warning disable LAMA0830 // NormalizeWhitespace is expensive.
            return CompilationUnit( default, default, default, SingletonList<MemberDeclarationSyntax>( classDeclaration ) ).NormalizeWhitespace();
#pragma warning restore LAMA0830
        }

        /// <summary>
        /// Creates the declaration of the forwarder of a target method: a public static extension method with the type parameters and the parameters
        /// of the target, whose body calls the target with its fully qualified name.
        /// </summary>
        private static MethodDeclarationSyntax CreateForwarderDeclaration(
            string name,
            IMethodSymbol definition,
            IMethod targetMethod,
            SyntaxGenerationContext context )
        {
            var syntaxGenerator = context.SyntaxGenerator;
            var codeModelDefinition = targetMethod.Definition;
            var typeParameters = GetContainingTypeParameters( definition.ContainingType ).Concat( definition.TypeParameters ).ToImmutableArray();

            var parameters = definition.Parameters.Select(
                ( p, i ) =>
                {
                    var modifiers = new List<SyntaxToken>();

                    if ( i == 0 )
                    {
                        modifiers.Add( Token( SyntaxKind.ThisKeyword ) );
                    }

                    switch ( p.RefKind )
                    {
                        case RefKind.Ref:
                            modifiers.Add( Token( SyntaxKind.RefKeyword ) );

                            break;

                        case RefKind.Out:
                            modifiers.Add( Token( SyntaxKind.OutKeyword ) );

                            break;

                        case RefKind.In or RefKind.RefReadOnlyParameter:
                            modifiers.Add( Token( SyntaxKind.InKeyword ) );

                            break;
                    }

                    if ( p.IsParams )
                    {
                        modifiers.Add( Token( SyntaxKind.ParamsKeyword ) );
                    }

                    var defaultValue = codeModelDefinition.Parameters[i].DefaultValue;

                    return Parameter(
                        default,
                        TokenList( modifiers ),
                        syntaxGenerator.TypeSyntax( p.Type ),
                        SyntaxFactoryEx.SafeIdentifier( p.Name ),
                        defaultValue == null ? null : EqualsValueClause( syntaxGenerator.TypedConstant( defaultValue.Value ) ) );
                } );

            var arguments = definition.Parameters.Select(
                p => Argument(
                    null,
                    p.RefKind switch
                    {
                        RefKind.Ref => Token( SyntaxKind.RefKeyword ),
                        RefKind.Out => Token( SyntaxKind.OutKeyword ),
                        RefKind.In or RefKind.RefReadOnlyParameter => Token( SyntaxKind.InKeyword ),
                        _ => default
                    },
                    SyntaxFactoryEx.SafeIdentifierName( p.Name ) ) );

            SimpleNameSyntax methodName = definition.TypeParameters.IsEmpty
                ? SyntaxFactoryEx.SafeIdentifierName( definition.Name )
                : GenericName(
                    SyntaxFactoryEx.SafeIdentifier( definition.Name ),
                    TypeArgumentList( SeparatedList<TypeSyntax>( definition.TypeParameters.Select( t => SyntaxFactoryEx.SafeIdentifierName( t.Name ) ) ) ) );

            var body = InvocationExpression(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, syntaxGenerator.TypeSyntax( definition.ContainingType ), methodName ),
                ArgumentList( SeparatedList( arguments ) ) );

            var attribute = Attribute(
                ParseName( "global::System.Runtime.CompilerServices.MethodImpl" ),
                AttributeArgumentList(
                    SingletonSeparatedList( AttributeArgument( ParseExpression( "global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining" ) ) ) ) );

            return MethodDeclaration(
                List( [AttributeList( SingletonSeparatedList( attribute ) ), CreateEditorBrowsableNeverAttributeList()] ),
                TokenList( Token( SyntaxKind.PublicKeyword ), Token( SyntaxKind.StaticKeyword ) ),
                definition.ReturnsVoid ? PredefinedType( Token( SyntaxKind.VoidKeyword ) ) : syntaxGenerator.TypeSyntax( definition.ReturnType ),
                null,
                SyntaxFactoryEx.SafeIdentifier( name ),
                typeParameters.IsEmpty
                    ? null
                    : TypeParameterList( SeparatedList( typeParameters.Select( t => TypeParameter( SyntaxFactoryEx.SafeIdentifier( t.Name ) ) ) ) ),
                ParameterList( SeparatedList( parameters ) ),
                typeParameters.IsEmpty ? default : syntaxGenerator.TypeParameterConstraintClauses( typeParameters ),
                null,
                ArrowExpressionClause( body ),
                Token( SyntaxKind.SemicolonToken ) );
        }

        /// <summary>
        /// Returns the name of the forwarder of a target method: two underscores, then the namespace, the declaring types and the method name, separated
        /// by underscores.
        /// </summary>
        /// <remarks>
        /// An underscore in a source name is doubled, so that <c>A_B.C</c> and <c>A.B_C</c> give different names. A generic type is written with its
        /// arity, as in its metadata name: <c>Hooks_1</c> for <c>Hooks&lt;T&gt;</c>. The forwarders of the overloads of a method have the same name.
        /// </remarks>
        internal static string GetForwarderName( IMethodSymbol definition )
        {
            var parts = new List<string>();

            if ( definition.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace )
            {
                parts.AddRange( containingNamespace.ToDisplayString().Split( '.' ) );
            }

            var types = new List<string>();

            for ( var type = definition.ContainingType; type != null; type = type.ContainingType )
            {
                types.Insert( 0, type.Arity == 0 ? type.Name : $"{type.Name}`{type.Arity}" );
            }

            parts.AddRange( types );
            parts.Add( definition.Name );

            return "__" + string.Join( "_", parts.SelectAsArray( EscapeNamePart ) );
        }

        /// <summary>
        /// Returns a string that identifies the name and the parameters of a forwarder, which two forwarders of one class cannot share.
        /// </summary>
        private static string GetForwarderSignature( string name, IMethodSymbol definition )
            => $"{name}`{definition.Arity + GetContainingTypeParameters( definition.ContainingType ).Count()}("
               + string.Join( ",", definition.Parameters.Select( p => (p.RefKind == RefKind.None ? "" : "&") + p.Type.ToDisplayString( SymbolDisplayFormat.FullyQualifiedFormat ) ) )
               + ")";

        /// <summary>
        /// Escapes a part of the name of a forwarder: an underscore is doubled, and every other character that cannot occur in a C# identifier is
        /// replaced by an underscore.
        /// </summary>
        private static string EscapeNamePart( string part )
        {
            var builder = new StringBuilder( part.Length + 4 );

            foreach ( var c in part )
            {
                if ( c == '_' )
                {
                    builder.Append( "__" );
                }
                else
                {
                    builder.Append( char.IsLetterOrDigit( c ) ? c : '_' );
                }
            }

            return builder.ToString();
        }

        /// <summary>
        /// Replaces every character that cannot occur in a C# identifier by an underscore.
        /// </summary>
        private static string SanitizeIdentifier( string name )
        {
            var builder = new StringBuilder( name.Length );

            foreach ( var c in name )
            {
                builder.Append( char.IsLetterOrDigit( c ) || c == '_' ? c : '_' );
            }

            if ( builder.Length == 0 || char.IsDigit( builder[0] ) )
            {
                builder.Insert( 0, '_' );
            }

            return builder.ToString();
        }

        /// <summary>
        /// Returns the type parameters of a type and of the types that contain it, from the outermost type.
        /// </summary>
        private static IEnumerable<ITypeParameterSymbol> GetContainingTypeParameters( INamedTypeSymbol? type )
            => type == null ? [] : GetContainingTypeParameters( type.ContainingType ).Concat( type.TypeParameters );

        /// <summary>
        /// Returns the type arguments of a type and of the types that contain it, from the outermost type.
        /// </summary>
        private static IEnumerable<ITypeSymbol> GetContainingTypeArguments( INamedTypeSymbol? type )
            => type == null ? [] : GetContainingTypeArguments( type.ContainingType ).Concat( type.TypeArguments );

        /// <summary>
        /// A forwarder generated for the call sites of one target method in conditional accesses.
        /// </summary>
        /// <param name="Name">The name of the forwarder.</param>
        /// <param name="Signature">The name and the parameters, which identify the forwarder in its class.</param>
        /// <param name="Declaration">The declaration of the forwarder.</param>
        /// <summary>
        /// Creates the attribute list <c>[EditorBrowsable( EditorBrowsableState.Never )]</c>, which hides a forwarder from IntelliSense.
        /// </summary>
        /// <param name="leadingTrivia">The trivia before the opening bracket, or <c>default</c>.</param>
        private static AttributeListSyntax CreateEditorBrowsableNeverAttributeList( SyntaxTriviaList leadingTrivia = default )
            => AttributeList(
                Token( leadingTrivia, SyntaxKind.OpenBracketToken, default ),
                null,
                SingletonSeparatedList(
                    Attribute(
                        ParseName( "global::System.ComponentModel.EditorBrowsable" ),
                        AttributeArgumentList(
                            SingletonSeparatedList(
                                AttributeArgument( ParseExpression( "global::System.ComponentModel.EditorBrowsableState.Never" ) ) ) ) ) ),
                Token( SyntaxKind.CloseBracketToken ) );

        private sealed record CallSiteForwarder( string Name, string Signature, MethodDeclarationSyntax Declaration );
    }
}
