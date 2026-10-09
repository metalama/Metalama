// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Extensibility.Synthesis;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using Accessibility = Metalama.Framework.Code.Accessibility;
using RefKind = Metalama.Framework.Code.RefKind;

namespace Metalama.Framework.Engine.Extensibility.CallSites
{
    public sealed partial class ExtensionTransformationFactory
    {
        /// <summary>
        /// The forwarders created for the call sites in a conditional access that are redirected to a method declared with <see cref="DeclareMethod"/>,
        /// indexed by the handle of the method. The caller must hold <see cref="_sync"/>.
        /// </summary>
        private readonly Dictionary<SynthesizedMethodHandle, CallSiteForwarder> _synthesizedForwarders = new();

        /// <summary>
        /// Returns the callee of a call site in a conditional access that is redirected to a static method declared with <see cref="DeclareMethod"/>:
        /// the name of the forwarder of the method. The forwarder is created when the method has none yet.
        /// </summary>
        /// <remarks>
        /// The forwarder is generated in the same class as the forwarders of existing methods, and its body calls the declared method with its fully
        /// qualified name. Because the declared method is not in the compilation yet, the call cannot be bound speculatively. The factory verifies
        /// instead that the name of the forwarder designates nothing at the call site, and that the receiver converts to the first parameter by an
        /// identity, reference or boxing conversion.
        /// </remarks>
        private SimpleNameSyntax GetSynthesizedForwarderCallee(
            InvocationRedirectionRequest request,
            SynthesizedCallSiteRedirectionTarget target,
            IInvocationOperation operation,
            SemanticModel semanticModel,
            SyntaxGenerationContext context )
        {
            var callSite = (InvocationExpressionSyntax) operation.Syntax;
            var handle = target.Handle;
            var targetMethod = handle.Method;

            string? reason = null;

            if ( !targetMethod.IsStatic )
            {
                reason = "it is not static";
            }
            else if ( targetMethod.Parameters.Count == 0 || targetMethod.Parameters[0].RefKind != RefKind.None || targetMethod.Parameters[0].IsParams )
            {
                reason = "its first parameter, which receives the receiver, is not a parameter passed by value";
            }
            else if ( targetMethod.DeclaringType.TypeParameters.Count > 0 || targetMethod.DeclaringType.DeclaringType != null )
            {
                reason = "its declaring type is generic or nested";
            }
            else if ( targetMethod.Accessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedInternal)
                      || targetMethod.DeclaringType.Accessibility is not (Accessibility.Public or Accessibility.Internal) )
            {
                reason = "it is not accessible from a top-level class of the assembly";
            }

            if ( reason != null )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through a forwarder, but the declared method '{targetMethod}' cannot be forwarded because {reason}.",
                    nameof(request) );
            }

            if ( !TryGetForwarderReceiver( semanticModel, callSite, context, out _, out var receiverType ) )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access whose receiver type cannot be named, so the call of the forwarder cannot be verified.",
                    nameof(request) );
            }

            if ( targetMethod.Parameters[0].Type.GetSymbol() is { } receiverParameterType )
            {
                var conversion = semanticModel.Compilation.ClassifyConversion( receiverType, receiverParameterType );

                if ( !(conversion.IsIdentity || (conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing))) )
                {
                    throw new ArgumentException(
                        $"The receiver of type '{receiverType.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' converts to the first parameter of '{targetMethod}' by neither an identity, a reference nor a boxing conversion, so it cannot be the receiver of the forwarder of the method.",
                        nameof(request) );
                }
            }

            var forwarderName = GetSynthesizedForwarderName( targetMethod );

            if ( !semanticModel.LookupSymbols( callSite.SpanStart, receiverType, forwarderName, includeReducedExtensionMethods: true ).IsEmpty )
            {
                throw new ArgumentException(
                    $"The call site '{callSite}' is in a conditional access, so it is redirected through the forwarder '{forwarderName}', but this name already designates another member at the call site.",
                    nameof(request) );
            }

            lock ( this._sync )
            {
                this.ThrowIfCompleted();

                if ( !this._synthesizedForwarders.ContainsKey( handle ) )
                {
                    var declaration = CreateSynthesizedForwarderDeclaration( forwarderName, targetMethod, this._compilation, context );

                    this._synthesizedForwarders.Add(
                        handle,
                        new CallSiteForwarder( forwarderName, $"{forwarderName}`{targetMethod.TypeParameters.Count}", declaration ) );
                }
            }

            return request.TypeArguments.IsDefaultOrEmpty
                ? SyntaxFactoryEx.SafeIdentifierName( forwarderName )
                : GenericName(
                    SyntaxFactoryEx.SafeIdentifier( forwarderName ),
                    TypeArgumentList( SeparatedList( request.TypeArguments.Select( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );
        }

        /// <summary>
        /// Returns the name of the forwarder of a declared method, built from its namespace, its declaring type and its name like the name of the
        /// forwarder of an existing method.
        /// </summary>
        private static string GetSynthesizedForwarderName( IMethod method )
        {
            var parts = new List<string>();

            if ( !method.DeclaringType.ContainingNamespace.IsGlobalNamespace )
            {
                parts.AddRange( method.DeclaringType.ContainingNamespace.FullName.Split( '.' ) );
            }

            parts.Add( method.DeclaringType.Name );
            parts.Add( method.Name );

            return "__" + string.Join( "_", parts.SelectAsArray( EscapeNamePart ) );
        }

        /// <summary>
        /// Creates the declaration of the forwarder of a declared method: a public static extension method with the type parameters and the
        /// parameters of the method, whose body calls the method with its fully qualified name.
        /// </summary>
        private static MethodDeclarationSyntax CreateSynthesizedForwarderDeclaration(
            string name,
            IMethod method,
            CompilationModel compilation,
            SyntaxGenerationContext context )
        {
            var syntaxGenerator = context.SyntaxGenerator;
            var parameterList = syntaxGenerator.ParameterList( method, compilation );
            var firstParameter = parameterList.Parameters[0];

            parameterList = parameterList.ReplaceNode(
                firstParameter,
                firstParameter.WithModifiers( firstParameter.Modifiers.Insert( 0, Token( default, SyntaxKind.ThisKeyword, TriviaList( Space ) ) ) ) );

            var arguments = method.Parameters.SelectAsReadOnlyList(
                p => Argument(
                    null,
                    p.RefKind switch
                    {
                        RefKind.Ref => Token( SyntaxKind.RefKeyword ),
                        RefKind.Out => Token( SyntaxKind.OutKeyword ),
                        RefKind.In or RefKind.RefReadOnly => Token( SyntaxKind.InKeyword ),
                        _ => default
                    },
                    SyntaxFactoryEx.SafeIdentifierName( p.Name ) ) );

            SimpleNameSyntax methodName = method.TypeParameters.Count == 0
                ? SyntaxFactoryEx.SafeIdentifierName( method.Name )
                : GenericName(
                    SyntaxFactoryEx.SafeIdentifier( method.Name ),
                    TypeArgumentList( SeparatedList<TypeSyntax>( method.TypeParameters.SelectAsReadOnlyList( t => SyntaxFactoryEx.SafeIdentifierName( t.Name ) ) ) ) );

            var body = InvocationExpression(
                MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, syntaxGenerator.TypeSyntax( method.DeclaringType ), methodName ),
                ArgumentList( SeparatedList( arguments ) ) );

            var attribute = Attribute(
                ParseName( "global::System.Runtime.CompilerServices.MethodImpl" ),
                AttributeArgumentList(
                    SingletonSeparatedList( AttributeArgument( ParseExpression( "global::System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining" ) ) ) ) );

            return MethodDeclaration(
                List( [AttributeList( SingletonSeparatedList( attribute ) ), CreateEditorBrowsableNeverAttributeList()] ),
                TokenList( Token( SyntaxKind.PublicKeyword ), Token( SyntaxKind.StaticKeyword ) ),
                syntaxGenerator.ReturnType( method ),
                null,
                SyntaxFactoryEx.SafeIdentifier( name ),
                syntaxGenerator.TypeParameterList( method, compilation ),
                parameterList,
                syntaxGenerator.ConstraintClauses( method ),
                null,
                ArrowExpressionClause( body ),
                Token( SyntaxKind.SemicolonToken ) );
        }
    }
}
