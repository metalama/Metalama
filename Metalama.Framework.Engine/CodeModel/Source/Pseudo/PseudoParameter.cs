// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Collections;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.Collections;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using RefKind = Metalama.Framework.Code.RefKind;
using SyntaxReference = Microsoft.CodeAnalysis.SyntaxReference;
using TypedConstant = Metalama.Framework.Code.TypedConstant;

namespace Metalama.Framework.Engine.CodeModel.Source.Pseudo
{
    internal sealed class PseudoParameter : BaseDeclaration, IParameter, IUserExpression
    {
        private readonly string? _name;

        public SymbolBasedDeclaration PropertyOrEvent => (SymbolBasedDeclaration) this.DeclaringAccessor.DeclaringMember.AssertNotNull();

        private IMethod DeclaringAccessor { get; }

        public IHasParameters DeclaringMember => this.DeclaringAccessor;

        public RefKind RefKind
            => this.DeclaringAccessor.ContainingDeclaration switch
            {
                SourceProperty property => property.RefKind,
                SourceField _ => RefKind.None,
                SourceEvent _ => RefKind.None,
                _ => throw new AssertionFailedException( $"Unexpected member: '{this.DeclaringAccessor.ContainingDeclaration}'." )
            };

        public IType Type { get; }

        public string Name => this._name ?? "<return>";

        public int Index { get; }

        public TypedConstant? DefaultValue => default;

        public bool IsParams => false;

        public bool IsThis => false;

        public override IDeclarationOrigin Origin => this.DeclaringMember.Origin;

        public override IDeclaration ContainingDeclaration => this.DeclaringAccessor;

        public override IAttributeCollection Attributes => AttributeCollection.Empty;

        public override DeclarationKind DeclarationKind => DeclarationKind.Parameter;

        public override bool IsImplicitlyDeclared => true;

        public override CompilationModel Compilation => this.DeclaringAccessor.GetCompilationModel();

        public PseudoParameter( PseudoAccessor declaringAccessor, int index, IType type, string? name )
        {
            this.DeclaringAccessor = declaringAccessor;
            this.Index = index;
            this.Type = type;
            this._name = name;
        }

        public override string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
            => this.DeclaringMember.ToDisplayString( format, context ) + "/" + this.Name;

        public ParameterInfo ToParameterInfo() => throw new NotSupportedException();

        public bool IsReturnParameter => this.Index < 0;

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override bool CanBeInherited => ((IDeclarationImpl) this.DeclaringMember).CanBeInherited;

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => throw new NotImplementedException();

        internal override DeclarationImplementationKind ImplementationKind => DeclarationImplementationKind.Pseudo;

        public override IAssembly DeclaringAssembly => this.DeclaringMember.DeclaringAssembly;

        public override Location? DiagnosticLocation => this.DeclaringMember.GetDiagnosticLocation();

        public override SyntaxTree? PrimarySyntaxTree => ((IDeclarationImpl) this.DeclaringAccessor).PrimarySyntaxTree;

        public override bool Equals( IDeclaration? other )
            => other is PseudoParameter pseudoParameter && this.DeclaringMember.Equals( pseudoParameter.DeclaringMember )
                                                        && this.Index == pseudoParameter.Index;

        protected override int GetHashCodeCore() => HashCode.Combine( this.DeclaringMember.GetHashCode(), this.Index );

        bool IExpression.IsAssignable => true;

        public ref object? Value
            => ref RefHelper.Wrap( new SyntaxUserExpression( SyntaxFactory.IdentifierName( this.Name ), this.Type, isReferenceable: true ) );

        public TypedExpressionSyntax ToTypedExpressionSyntax( ISyntaxGenerationContext syntaxGenerationContext, IType? targetType = null )
            => new TypedExpressionSyntaxImpl(
                SyntaxFactory.IdentifierName( this.Name ),
                this.Type,
                ((SyntaxSerializationContext) syntaxGenerationContext).CompilationModel,
                isReferenceable: true );

        public override bool BelongsToCurrentProject => this.ContainingDeclaration.BelongsToCurrentProject;

        public override ImmutableArray<SourceReference> Sources => ImmutableArray<SourceReference>.Empty;

        internal override GenericContext GenericContext => (GenericContext) this.ContainingDeclaration.GenericContext;

        [Memo]
        private IFullRef<IParameter> Ref => this.RefFactory.FromPseudoParameter( this );

        IRef<IParameter> IParameter.ToRef() => this.Ref;

        IRef<IDeclaration> IDeclaration.ToRef() => this.Ref;

        internal override ICompilationElement Translate(
            CompilationModel newCompilation,
            IGenericContext? genericContext = null,
            Type? interfaceType = null )
            => newCompilation.Factory.Translate( this.DeclaringMember, genericContext ).AssertNotNull().Parameters[this.Index];
    }
}