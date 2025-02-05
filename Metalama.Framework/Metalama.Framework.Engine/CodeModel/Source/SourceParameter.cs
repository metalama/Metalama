// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RefKind = Metalama.Framework.Code.RefKind;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal sealed class SourceParameter : SourceDeclaration, IParameterImpl
    {
        private readonly IParameterSymbol _parameterSymbol;

        [Memo]
        private SourceMember DeclaringMember
            => (SourceMember) this.Compilation.Factory.GetDeclaration( this._parameterSymbol.ContainingSymbol, this.GenericContextForSymbolMapping );

        public ParameterInfo ToParameterInfo() => CompileTimeParameterInfo.Create( this );

        public bool IsReturnParameter => false;

        IHasParameters IParameter.DeclaringMember => (IHasParameters) this.DeclaringMember;

        public SourceParameter( IParameterSymbol symbol, CompilationModel compilation, GenericContext? genericContextForSymbolMapping )
            : base( compilation, genericContextForSymbolMapping )
        {
            if ( symbol.IsThis )
            {
                throw new ArgumentException( $"The hidden this parameter '{symbol}' cannot be represented in Metalama code model." );
            }

            this._parameterSymbol = symbol;
        }

        public RefKind RefKind
            => this._parameterSymbol.RefKind switch
            {
                Microsoft.CodeAnalysis.RefKind.None => RefKind.None,
                Microsoft.CodeAnalysis.RefKind.Ref => RefKind.Ref,
                Microsoft.CodeAnalysis.RefKind.Out => RefKind.Out,
                Microsoft.CodeAnalysis.RefKind.In => RefKind.In,
#if ROSLYN_4_8_0_OR_GREATER
                Microsoft.CodeAnalysis.RefKind.RefReadOnlyParameter => RefKind.RefReadOnly,
#endif
                _ => throw new InvalidOperationException( $"Roslyn RefKind {this._parameterSymbol.RefKind} not recognized." )
            };

        [Memo]
        public IType Type => this.Compilation.Factory.GetIType( this._parameterSymbol.Type, this.GenericContextForSymbolMapping );

        public string Name => this._parameterSymbol.Name;

        public int Index => this._parameterSymbol.Ordinal;

        public bool IsParams => this._parameterSymbol.IsParams;

        public bool IsThis => this.Index == 0 && this._parameterSymbol.ContainingSymbol is IMethodSymbol { IsExtensionMethod: true };

        public override IDeclaration ContainingDeclaration => this.DeclaringMember;

        public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

        public override ISymbol Symbol => this._parameterSymbol;

        public override bool CanBeInherited => this.DeclaringMember.CanBeInherited;

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default )
            => this.DeclaringMember.GetDerivedDeclarations( options ).Select( d => ((IHasParameters) d).Parameters[this.Index] );

        public TypedConstant? DefaultValue
            => this._parameterSymbol.HasExplicitDefaultValue
                ? TypedConstant.Create(
                    this._parameterSymbol.ExplicitDefaultValue,
                    this.Compilation.Factory.Translate( this.Type, this.GenericContextForSymbolMapping ) )
                : null;

        public override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
            => this.DeclaringMember.ToDisplayString( format, context ) + "/" + this.Name;

        public override string ToString() => this.DeclaringMember + "/" + this.Name;

        public override SyntaxTree? PrimarySyntaxTree => ((IDeclarationImpl) this.DeclaringMember).PrimarySyntaxTree;

        bool IExpression.IsAssignable => true;

        public ref object? Value => ref RefHelper.Wrap( new SyntaxVariableExpression( SyntaxFactory.IdentifierName( this.Name ), this.Type, this.RefKind ) );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => new(
                new TypedExpressionSyntaxImpl(
                    SyntaxFactory.IdentifierName( this.Name ),
                    this.Type,
                    ((SyntaxSerializationContext) syntaxGenerationContext).CompilationModel,
                    true ) );

        [Memo]
        private IFullRef<IParameter> Ref => this.RefFactory.FromSymbolBasedDeclaration<IParameter>( this );

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        IRef<IParameter> IParameter.ToRef() => this.Ref;
    }
}