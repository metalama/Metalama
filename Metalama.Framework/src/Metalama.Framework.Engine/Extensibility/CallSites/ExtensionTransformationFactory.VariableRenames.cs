// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Extensibility.CallSites
{
    public sealed partial class ExtensionTransformationFactory
    {
        /// <summary>
        /// The variable renames of all the redirections, keyed by the source node that declares the variable, so that the redirections that rename
        /// the same variable share one instance, or <c>null</c> until the first rename, because a rename is rare. Protected by <see cref="_sync"/>.
        /// </summary>
        private Dictionary<SyntaxNode, CallSiteVariableRename>? _variableRenames;

        /// <summary>
        /// Returns the syntax that passes a parameter of the member that contains a call site, and adds the variables that hide the parameter at the
        /// call site to the variables that the linker renames.
        /// </summary>
        /// <param name="parameter">The parameter that the request passes.</param>
        /// <param name="parameterName">The syntax generated for the parameter, which is its name.</param>
        /// <param name="semanticModel">The semantic model of the call site.</param>
        /// <param name="callSite">The call site.</param>
        /// <param name="context">The syntax generation context of the call site.</param>
        /// <param name="description">The description of the redirection.</param>
        /// <param name="variableRenames">The collection to which the variables to rename are added. It is created when the first variable is
        /// added.</param>
        /// <returns><paramref name="parameterName"/> when the name designates the parameter at the call site, and otherwise the name cast to the type
        /// of the parameter.</returns>
        /// <exception cref="ArgumentException">A variable that hides the parameter cannot be renamed.</exception>
        /// <remarks>
        /// <para>
        /// Since C# 8, a lambda or a local function can declare a parameter or a local variable that has the name of a parameter of the containing
        /// member. Inside it, the name designates this variable. The linker then renames the variable, and its references, so that the name designates
        /// the parameter again. Several nested functions can declare a variable with the same name, so all the variables between the call site and the
        /// member are renamed.
        /// </para>
        /// <para>
        /// The returned expression is cast to the type of the parameter, because the factory binds the rewritten call speculatively in the source
        /// compilation, where the name still designates the hiding variable. The cast gives the argument the type that it has in the final code. The
        /// simplifier removes it when the code is formatted.
        /// </para>
        /// </remarks>
        private static ExpressionSyntax GetParameterValueSyntax(
            IParameter parameter,
            IdentifierNameSyntax parameterName,
            SemanticModel semanticModel,
            InvocationExpressionSyntax callSite,
            SyntaxGenerationContext context,
            string description,
            ref List<CallSiteVariableRename>? variableRenames )
        {
            // Only a lambda or a local function can declare a variable with the name of a parameter of the member, because C# forbids it in the body
            // of the member itself. The common case, a call site outside any nested function, therefore requires no binding.
            if ( !IsInNestedFunction( callSite ) )
            {
                return parameterName;
            }

            if ( parameter.GetSymbol() is not IParameterSymbol parameterSymbol )
            {
                // A parameter introduced by an aspect has no symbol in the source compilation, and no source variable can hide it.
                return parameterName;
            }

            var name = parameterSymbol.Name;
            var position = callSite.SpanStart;
            var hasRenames = false;

            while ( true )
            {
                var symbol = semanticModel.GetSpeculativeSymbolInfo( position, parameterName, SpeculativeBindingOption.BindAsExpression ).Symbol;

                if ( symbol is { Kind: SymbolKind.Parameter } && !IsNestedFunction( symbol.ContainingSymbol ) )
                {
                    // The name designates a parameter of the member.
                    break;
                }

                if ( symbol is not ({ Kind: SymbolKind.Parameter } or { Kind: SymbolKind.Local }) || !IsNestedFunction( symbol.ContainingSymbol ) )
                {
                    throw new ArgumentException(
                        $"The parameter '{name}' cannot be passed at the call site '{callSite}', because the name designates "
                        + (symbol == null ? "no symbol" : $"'{symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}'")
                        + " there, and only a parameter of a lambda or local function or a local variable can be renamed.",
                        "request" );
                }

                var function = GetFunctionSyntax( semanticModel, callSite, (IMethodSymbol) symbol.ContainingSymbol );
                (variableRenames ??= new List<CallSiteVariableRename>()).Add( CreateVariableRename( symbol, function, semanticModel, callSite, parameterSymbol, description ) );
                hasRenames = true;

                // The variables of the next enclosing scope are those that are visible where the function is declared.
                position = function.SpanStart;
            }

            return hasRenames
                ? ParenthesizedExpression( CastExpression( context.SyntaxGenerator.TypeSyntax( parameter.Type ), parameterName ).WithSimplifierAnnotation() )
                    .WithSimplifierAnnotation()
                : parameterName;
        }

        /// <summary>
        /// Determines whether a node is inside a lambda, an anonymous method or a local function.
        /// </summary>
        private static bool IsInNestedFunction( SyntaxNode node )
        {
            for ( var ancestor = node.Parent; ancestor != null; ancestor = ancestor.Parent )
            {
                if ( ancestor.Kind() is SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression or SyntaxKind.AnonymousMethodExpression
                    or SyntaxKind.LocalFunctionStatement )
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether a symbol is a lambda or a local function.
        /// </summary>
        private static bool IsNestedFunction( ISymbol? symbol )
            => symbol is { Kind: SymbolKind.Method } && ((IMethodSymbol) symbol).MethodKind is MethodKind.AnonymousFunction or MethodKind.LocalFunction;

        /// <summary>
        /// Returns the syntax of the lambda or local function that contains a call site and whose symbol is given.
        /// </summary>
        private static SyntaxNode GetFunctionSyntax( SemanticModel semanticModel, SyntaxNode callSite, IMethodSymbol function )
        {
            foreach ( var ancestor in callSite.Ancestors() )
            {
                var symbol = ancestor.Kind() switch
                {
                    SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression or SyntaxKind.AnonymousMethodExpression =>
                        semanticModel.GetSymbolInfo( ancestor ).Symbol,
                    SyntaxKind.LocalFunctionStatement => semanticModel.GetDeclaredSymbol( ancestor ),
                    _ => null
                };

                if ( symbol != null && SymbolEqualityComparer.Default.Equals( symbol, function ) )
                {
                    return ancestor;
                }
            }

            throw new AssertionFailedException( $"The function '{function}' does not contain the call site '{callSite}'." );
        }

        /// <summary>
        /// Creates the rename of a parameter of a lambda or local function, or of a local variable, that hides a parameter of the containing member.
        /// </summary>
        /// <param name="variable">The parameter or local variable to rename.</param>
        /// <param name="function">The syntax of the lambda or local function that declares the variable.</param>
        /// <param name="semanticModel">The semantic model of the call site.</param>
        /// <param name="callSite">The call site.</param>
        /// <param name="hiddenParameter">The parameter of the containing member.</param>
        /// <param name="description">The description of the redirection.</param>
        /// <exception cref="ArgumentException">The variable cannot be renamed.</exception>
        /// <remarks>
        /// The new name is the first name of the form <c>name_1</c>, <c>name_2</c> and so on that the containing member does not contain as an
        /// identifier. No code of the member can therefore refer to it. The name depends only on the source code of the member, so all the variables
        /// of the member that hide the same parameter receive the same name, which keeps the relations between them.
        /// </remarks>
        private static CallSiteVariableRename CreateVariableRename(
            ISymbol variable,
            SyntaxNode function,
            SemanticModel semanticModel,
            SyntaxNode callSite,
            IParameterSymbol hiddenParameter,
            string description )
        {
            var name = variable.Name;

            if ( variable.DeclaringSyntaxReferences is not [var declarationReference] )
            {
                throw new ArgumentException( $"The variable '{name}', which hides the parameter '{name}' at the call site '{callSite}', cannot be renamed.", "request" );
            }

            var declaration = declarationReference.GetSyntax();

            if ( declaration is not (ParameterSyntax or VariableDeclaratorSyntax or SingleVariableDesignationSyntax or ForEachStatementSyntax
                or CatchDeclarationSyntax) )
            {
                throw new ArgumentException(
                    $"The variable '{name}', which hides the parameter '{name}' at the call site '{callSite}', is declared by a {declaration.Kind()}, which cannot be renamed.",
                    "request" );
            }

            var references = ImmutableArray.CreateBuilder<IdentifierNameSyntax>();

            foreach ( var identifier in function.DescendantNodes().OfType<IdentifierNameSyntax>() )
            {
                if ( identifier.Identifier.ValueText != name
                     || !SymbolEqualityComparer.Default.Equals( semanticModel.GetSymbolInfo( identifier ).Symbol, variable ) )
                {
                    continue;
                }

                // The value of nameof is the name, which the rename would change.
                if ( identifier.Ancestors().Any( IsNameOfExpression ) )
                {
                    throw new ArgumentException(
                        $"The variable '{name}', which hides the parameter '{name}' at the call site '{callSite}', cannot be renamed, because it is used in a nameof expression.",
                        "request" );
                }

                references.Add( identifier );
            }

            var memberIdentifiers = new HashSet<string>(
                GetMemberSyntax( callSite ).DescendantTokens().Where( t => t.IsKind( SyntaxKind.IdentifierToken ) ).Select( t => t.ValueText ),
                StringComparer.Ordinal );

            string newName;

            for ( var i = 1;; i++ )
            {
                newName = $"{name}_{i}";

                if ( !memberIdentifiers.Contains( newName ) )
                {
                    break;
                }
            }

            var variableKind = variable.Kind == SymbolKind.Local
                ? "local variable"
                : function.IsKind( SyntaxKind.LocalFunctionStatement )
                    ? "local function parameter"
                    : "lambda parameter";

            return new CallSiteVariableRename( declaration, references.ToImmutable(), name, newName, variableKind, hiddenParameter, description );

            static bool IsNameOfExpression( SyntaxNode node )
                => node.IsKind( SyntaxKind.InvocationExpression )
                   && ((InvocationExpressionSyntax) node).Expression is { RawKind: (int) SyntaxKind.IdentifierName } expression
                   && ((IdentifierNameSyntax) expression).Identifier.ValueText == "nameof";
        }

        /// <summary>
        /// Returns the member declaration that contains a node, or the compilation unit for a top-level statement.
        /// </summary>
        private static SyntaxNode GetMemberSyntax( SyntaxNode node )
        {
            var member = node.Ancestors().OfType<MemberDeclarationSyntax>().First();

            return member is GlobalStatementSyntax ? member.SyntaxTree.GetRoot() : member;
        }

        /// <summary>
        /// Returns the registered instance of each variable rename, so that the redirections that rename the same variable share one instance. The
        /// caller must hold <see cref="_sync"/>.
        /// </summary>
        private ImmutableArray<CallSiteVariableRename> RegisterVariableRenames( List<CallSiteVariableRename> variableRenames )
        {
            this._variableRenames ??= new Dictionary<SyntaxNode, CallSiteVariableRename>( SyntaxNodeReferenceComparer.Instance );

            var result = ImmutableArray.CreateBuilder<CallSiteVariableRename>( variableRenames.Count );

            foreach ( var rename in variableRenames )
            {
                if ( !this._variableRenames.TryGetValue( rename.DeclarationNode, out var registered ) )
                {
                    registered = rename;
                    this._variableRenames.Add( rename.DeclarationNode, rename );
                }

                result.Add( registered );
            }

            return result.MoveToImmutable();
        }
    }
}
