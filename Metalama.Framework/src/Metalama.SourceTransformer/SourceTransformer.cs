// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Compiler;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.SourceTransformer
{
    [Transformer]
    [UsedImplicitly]
    internal class SourceTransformer : ISourceTransformer
    {
        public void Execute( TransformerContext context )
        {
            var changeDynamicToObject = context.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(
                                            "build_property.ChangeDynamicToObject",
                                            out var changeDynamicToObjectStr )
                                        && bool.Parse( changeDynamicToObjectStr );

            var rewriter = new Rewriter( changeDynamicToObject, context.Compilation );
            var compilation = context.Compilation;

            foreach ( var tree in compilation.SyntaxTrees )
            {
                var newRoot = rewriter.Visit( tree.GetRoot() );

                context.ReplaceSyntaxTree( tree, tree.WithRootAndOptions( newRoot, tree.Options ) );
            }

            foreach ( var diagnostic in rewriter.Diagnostics )
            {
                context.ReportDiagnostic( diagnostic );
            }
        }

        private sealed class Rewriter : CSharpSyntaxRewriter
        {
            private readonly bool _changeDynamicToObject;
            private readonly Compilation _compilation;
            private List<FieldDeclarationSyntax>? _fieldsToAdd;

            public Rewriter( bool changeDynamicToObject, Compilation compilation )
            {
                this._changeDynamicToObject = changeDynamicToObject;
                this._compilation = compilation;
            }

            public List<Diagnostic> Diagnostics { get; } = new();

            public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node )
            {
                // Make sure we have substituted the dynamic keyword.
                node = (PropertyDeclarationSyntax) base.VisitPropertyDeclaration( node )!;

                if ( !node.AttributeLists.SelectMany( al => al.Attributes )
                        .Any(
                            a => string.Equals( a.Name.ToString(), "Memo", StringComparison.Ordinal ) ||
                                 string.Equals( a.Name.ToString(), "MemoAttribute", StringComparison.Ordinal ) ) )
                {
                    return node;
                }

                if ( !IsSupportedPropertyDeclaration( node ) )
                {
                    this.Diagnostics.Add( Diagnostic.Create( _nonExpressionBodyError, node.GetLocation() ) );

                    return node;
                }

                if ( node.Modifiers.Any( SyntaxKind.StaticKeyword ) )
                {
                    this.Diagnostics.Add( Diagnostic.Create( _staticPropertyError, node.GetLocation() ) );

                    return node;
                }

                var fieldName = "@" + char.ToLowerInvariant( node.Identifier.ValueText[0] ) + node.Identifier.ValueText.Substring( 1 );

                var semanticModel = this._compilation.GetSemanticModel( node.SyntaxTree );
                var nodeType = (ITypeSymbol?) semanticModel.GetSymbolInfo( node.Type ).Symbol;
                var isValueType = nodeType is { IsValueType: true };

                TypeSyntax fieldType;
                ExpressionSyntax deferenceExpression;
                Func<ExpressionSyntax, ExpressionSyntax> evaluateExpression;

                if ( isValueType )
                {
                    // If we have a value type, we use a StrongBox. This seems inefficient at first sight, but this is the only
                    // general way to ensure, without locks, that the writing of the struct is atomic. Improvements are possible for
                    // other intrinsic types provided by Interlocked, but it would require more work, and a convention meaning that
                    // the default value means unassigned.

                    var fieldInstanceType = QualifiedName(
                        QualifiedName(
                            QualifiedName(
                                IdentifierName( "System" ),
                                IdentifierName( "Runtime" ) ),
                            IdentifierName( "CompilerServices" ) ),
                        GenericName( Identifier( "StrongBox" ) )
                            .WithTypeArgumentList( TypeArgumentList( SingletonSeparatedList( node.Type ) ) ) );

                    fieldType = NullableType( fieldInstanceType );

                    deferenceExpression = MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName( fieldName ),
                        IdentifierName( "Value" ) );

                    evaluateExpression = expression
                        => ObjectCreationExpression( fieldInstanceType ).WithArgumentList( ArgumentList( SingletonSeparatedList( Argument( expression ) ) ) );
                }
                else
                {
                    fieldType = NullableType( node.Type );
                    deferenceExpression = IdentifierName( fieldName );
                    evaluateExpression = expression => expression;
                }

                this._fieldsToAdd!.Add( FieldDeclaration( VariableDeclaration( fieldType ).AddVariables( VariableDeclarator( fieldName ) ) ) );

                if ( node.ExpressionBody != null )
                {
                    var block = TransformExpression( fieldName, evaluateExpression( node.ExpressionBody.Expression ), deferenceExpression );

                    var newNode = node.WithExpressionBody( null )
                        .WithSemicolonToken( default )
                        .AddAccessorListAccessors( AccessorDeclaration( SyntaxKind.GetAccessorDeclaration, block ) );

                    // PropertyType? field;

                    return newNode;
                }
                else
                {
                    var getAccessor = node.AccessorList!.Accessors.SingleOrDefault( a => a.Kind() == SyntaxKind.GetAccessorDeclaration )!;
                    var setAccessor = node.AccessorList!.Accessors.SingleOrDefault( a => a.Kind() == SyntaxKind.SetAccessorDeclaration );

                    var block = TransformExpression( fieldName, evaluateExpression( getAccessor.ExpressionBody!.Expression ), deferenceExpression );

                    var newGetAccessor =
                        getAccessor.WithExpressionBody( null )
                            .WithSemicolonToken( default )
                            .WithBody( block );

                    var newNode = node.WithAccessorList(
                            AccessorList(
                                setAccessor != null
                                    ? List( new[] { newGetAccessor, setAccessor } )
                                    : SingletonList( newGetAccessor ) ) )
                        .WithSemicolonToken( default );

                    return newNode;
                }
            }

            private static bool IsSupportedPropertyDeclaration( PropertyDeclarationSyntax node )
            {
                if ( node.ExpressionBody != null )
                {
                    return true;
                }

                if ( node.AccessorList == null )
                {
                    return false;
                }

                var getAccessor = node.AccessorList!.Accessors.SingleOrDefault( a => a.Kind() == SyntaxKind.GetAccessorDeclaration );
                var setAccessor = node.AccessorList!.Accessors.SingleOrDefault( a => a.Kind() == SyntaxKind.SetAccessorDeclaration );

                if ( getAccessor?.ExpressionBody == null )
                {
                    return false;
                }

                if ( setAccessor?.ExpressionBody?.Expression is ThrowExpressionSyntax )
                {
                    return true;
                }

                return false;
            }

            private static BlockSyntax TransformExpression( string fieldName, ExpressionSyntax expression, ExpressionSyntax dereferenceExpression )
                =>

                    // PERF: read the field into a local, to avoid unnecessary double read in the fast case
                    // if (field == null)
                    //     Interlocked.CompareExchange(ref field, expression, null);
                    // return field;
                    Block(
                        IfStatement(
                            BinaryExpression( SyntaxKind.EqualsExpression, IdentifierName( fieldName ), LiteralExpression( SyntaxKind.NullLiteralExpression ) ),
                            ExpressionStatement(
                                InvocationExpression( ParseExpression( "Interlocked.CompareExchange" ) )
                                    .AddArgumentListArguments(
                                        Argument( IdentifierName( fieldName ) ).WithRefKindKeyword( Token( SyntaxKind.RefKeyword ) ),
                                        Argument( expression ),
                                        Argument( LiteralExpression( SyntaxKind.NullLiteralExpression ) ) ) ) ),
                        ReturnStatement(
                            Token( SyntaxKind.ReturnKeyword ).WithTrailingTrivia( Space ),
                            dereferenceExpression,
                            Token( SyntaxKind.SemicolonToken ) ) );

            public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node )
            {
                var parentFieldsToAdd = this._fieldsToAdd;
                this._fieldsToAdd = new List<FieldDeclarationSyntax>();

                var result = (ClassDeclarationSyntax) base.VisitClassDeclaration( node )!;

                result = result.AddMembers( this._fieldsToAdd.ToArray<MemberDeclarationSyntax>() );

                this._fieldsToAdd = parentFieldsToAdd;

                return result;
            }

            public override SyntaxNode VisitRecordDeclaration( RecordDeclarationSyntax node )
            {
                var parentFieldsToAdd = this._fieldsToAdd;
                this._fieldsToAdd = new List<FieldDeclarationSyntax>();

                var result = (RecordDeclarationSyntax) base.VisitRecordDeclaration( node )!;

                result = result.AddMembers( this._fieldsToAdd.ToArray<MemberDeclarationSyntax>() );

                this._fieldsToAdd = parentFieldsToAdd;

                return result;
            }

            public override SyntaxNode VisitCompilationUnit( CompilationUnitSyntax node )
            {
                const string usingSystemThreading = "using System.Threading;";

                node = (CompilationUnitSyntax) base.VisitCompilationUnit( node )!;

                if ( node.Usings.All( u => u.ToString() != usingSystemThreading ) )
                {
                    node = node.AddUsings( ParseCompilationUnit( usingSystemThreading ).Usings.Single() );
                }

                return node;
            }

            public override SyntaxNode? VisitIdentifierName( IdentifierNameSyntax node )
            {
                if ( node.Identifier.Text == "dynamic" )
                {
                    if ( this._changeDynamicToObject )
                    {
                        return PredefinedType( Token( SyntaxKind.ObjectKeyword ) ).WithTriviaFrom( node );
                    }
                    else
                    {
                        this.Diagnostics.Add( Diagnostic.Create( _dynamicKeywordError, node.GetLocation() ) );
                    }
                }

                return base.VisitIdentifierName( node );
            }
        }

        private static readonly DiagnosticDescriptor _nonExpressionBodyError =
            new(
                "CMT001",
                "Only expression-bodied properties are supported",
                "Only expression-bodied properties are supported",
                "Metalama.SourceTransformer",
                DiagnosticSeverity.Error,
                true );

        private static readonly DiagnosticDescriptor _staticPropertyError =
            new(
                "CMT002",
                "Static properties are not supported",
                "Static properties are not supported",
                "Metalama.SourceTransformer",
                DiagnosticSeverity.Error,
                true );

        private static readonly DiagnosticDescriptor _dynamicKeywordError =
            new(
                "CMT004",
                "Dynamic keyword forbidden",
                "The 'dynamic' keyword is forbidden in this project",
                "Metalama.SourceTransformer",
                DiagnosticSeverity.Error,
                true );
    }
}