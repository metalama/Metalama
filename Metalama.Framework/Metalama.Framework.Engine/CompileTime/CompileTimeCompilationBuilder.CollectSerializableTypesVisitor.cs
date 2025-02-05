// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime.Serialization;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.CompileTime
{
    internal sealed partial class CompileTimeCompilationBuilder
    {
        /// <summary>
        /// Determines if a syntax tree has compile-time code.
        /// </summary>
        private sealed class CollectSerializableTypesVisitor : SafeSyntaxWalker
        {
            private readonly ClassifyingCompilationContext _compilationContext;
            private readonly SemanticModel _semanticModel;
            private readonly CancellationToken _cancellationToken;
            private readonly Action<SerializableTypeInfo> _onSerializableTypeDiscovered;

            public CollectSerializableTypesVisitor(
                ClassifyingCompilationContext compilationContext,
                SemanticModel semanticModel,
                Action<SerializableTypeInfo> onSerializableTypeDiscovered,
                CancellationToken cancellationToken )
            {
                this._compilationContext = compilationContext;
                this._semanticModel = semanticModel;
                this._cancellationToken = cancellationToken;
                this._onSerializableTypeDiscovered = onSerializableTypeDiscovered;
            }

            private void ProcessTypeDeclaration( SyntaxNode node )
            {
                this._cancellationToken.ThrowIfCancellationRequested();

                var declaredSymbol = (INamedTypeSymbol) this._semanticModel.GetDeclaredSymbol( node ).AssertNotNull();

                var serializableInterface = this._compilationContext.ReflectionMapper.GetTypeSymbol( typeof(ICompileTimeSerializable) );

                if ( !declaredSymbol.AllInterfaces.Any( i => this._compilationContext.CompilationContext.SymbolComparer.Equals( i, serializableInterface ) ) )
                {
                    return;
                }

                var innerVisitor = new CollectSerializableFieldsVisitor(
                    this._compilationContext,
                    this._semanticModel,
                    node,
                    this._cancellationToken );

                innerVisitor.Visit( node );

                this._onSerializableTypeDiscovered( new SerializableTypeInfo( declaredSymbol, innerVisitor.SerializableFieldsOrProperties ) );
            }

            public override void VisitClassDeclaration( ClassDeclarationSyntax node )
            {
                this.ProcessTypeDeclaration( node );
                base.VisitClassDeclaration( node );
            }

            public override void VisitStructDeclaration( StructDeclarationSyntax node )
            {
                this.ProcessTypeDeclaration( node );
                base.VisitStructDeclaration( node );
            }

            public override void VisitRecordDeclaration( RecordDeclarationSyntax node )
            {
                this.ProcessTypeDeclaration( node );
                base.VisitRecordDeclaration( node );
            }

            public override void VisitMethodDeclaration( MethodDeclarationSyntax node ) { }

            public override void VisitFieldDeclaration( FieldDeclarationSyntax node ) { }

            public override void VisitPropertyDeclaration( PropertyDeclarationSyntax node ) { }

            public override void VisitPropertyPatternClause( PropertyPatternClauseSyntax node ) { }

            public override void VisitAccessorDeclaration( AccessorDeclarationSyntax node ) { }

            public override void VisitConstructorDeclaration( ConstructorDeclarationSyntax node ) { }

            public override void VisitIndexerDeclaration( IndexerDeclarationSyntax node ) { }

            public override void VisitOperatorDeclaration( OperatorDeclarationSyntax node ) { }

            public override void VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node ) { }

            public override void VisitEventDeclaration( EventDeclarationSyntax node ) { }

            public override void VisitEventFieldDeclaration( EventFieldDeclarationSyntax node ) { }
        }
    }
}