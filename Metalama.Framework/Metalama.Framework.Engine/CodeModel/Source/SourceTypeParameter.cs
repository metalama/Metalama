// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;
using TypeParameterKind = Metalama.Framework.Code.TypeParameterKind;
using VarianceKind = Metalama.Framework.Code.VarianceKind;

namespace Metalama.Framework.Engine.CodeModel.Source
{
    internal sealed class SourceTypeParameter : SourceDeclaration, ITypeParameter, ITypeImpl
    {
        private readonly ITypeParameterSymbol _typeParameterSymbol;

        internal SourceTypeParameter(
            ITypeParameterSymbol typeParameterSymbol,
            CompilationModel compilation,
            GenericContext? genericContext ) : base(
            compilation,
            genericContext )
        {
            this._typeParameterSymbol = typeParameterSymbol;
        }

        public TypeKind TypeKind => TypeKind.TypeParameter;

        public SpecialType SpecialType => SpecialType.None;

        public Type ToType() => this.Compilation.Factory.GetReflectionType( this._typeParameterSymbol );

        public bool? IsReferenceType => this.IsReferenceTypeImpl();

        public bool? IsNullable => this.IsNullableImpl();

        public string Name => this._typeParameterSymbol.Name;

        public int Index => this._typeParameterSymbol.Ordinal;

        [Memo]
        public IReadOnlyList<IType> TypeConstraints
            => this._typeParameterSymbol.ConstraintTypes.Select( t => this.Compilation.Factory.GetIType( t ) ).ToImmutableArray();

        public TypeKindConstraint TypeKindConstraint
        {
            get
            {
                if ( this._typeParameterSymbol.HasUnmanagedTypeConstraint )
                {
                    return TypeKindConstraint.Unmanaged;
                }
                else if ( this._typeParameterSymbol.HasValueTypeConstraint )
                {
                    return TypeKindConstraint.Struct;
                }
                else if ( this._typeParameterSymbol.HasReferenceTypeConstraint )
                {
                    return TypeKindConstraint.Class;
                }
                else if ( this._typeParameterSymbol.HasNotNullConstraint )
                {
                    return TypeKindConstraint.NotNull;
                }
                else
                {
                    return TypeKindConstraint.None;
                }
            }
        }

        public bool AllowsRefStruct
            =>
#if ROSLYN_4_12_0_OR_GREATER
                this._typeParameterSymbol.AllowsRefLikeType;
#else
            false;
#endif

        public VarianceKind Variance
            => this._typeParameterSymbol.Variance switch
            {
                Microsoft.CodeAnalysis.VarianceKind.In => VarianceKind.In,
                Microsoft.CodeAnalysis.VarianceKind.Out => VarianceKind.Out,
                _ => VarianceKind.None
            };

        public bool? IsConstraintNullable => this._typeParameterSymbol.ReferenceTypeConstraintNullableAnnotation.ToIsAnnotated();

        public bool HasDefaultConstructorConstraint => this._typeParameterSymbol.HasConstructorConstraint;

        // Anomaly: this.TypeParameterSymbol is NOT bound to a generic context in Roslyn. We need to map the generic context ourselves.

        [Memo]
        public override IDeclaration ContainingDeclaration
            => this.Compilation.Factory.GetDeclaration(
                this._typeParameterSymbol.ContainingSymbol,
                RefTargetKind.Default,
                genericContext: this.GenericContext );

        public override DeclarationKind DeclarationKind => DeclarationKind.TypeParameter;

        public override ISymbol Symbol => this._typeParameterSymbol;

        public override bool CanBeInherited => ((IDeclarationImpl) this.ContainingDeclaration).CanBeInherited;

        public override IEnumerable<IDeclaration> GetDerivedDeclarations( DerivedTypesOptions options = default ) => throw new NotImplementedException();

        DeclarationKind ICompilationElement.DeclarationKind => DeclarationKind.TypeParameter;

        ICompilation ICompilationElement.Compilation => this.Compilation;

        public override SyntaxTree? PrimarySyntaxTree => ((IDeclarationImpl) this.ContainingDeclaration).PrimarySyntaxTree;

        bool IType.Equals( SpecialType specialType ) => false;

        public bool Equals( IType? otherType, TypeComparison typeComparison )
            => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this, otherType );

        public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
            => otherType != null && this.Equals( this.Compilation.Factory.GetTypeByReflectionType( otherType ), typeComparison );

        public override bool Equals( object? obj )
            => obj switch
            {
                IType otherType => this.Equals( otherType ),
                Type otherType => this.Equals( otherType ),
                _ => false
            };

        public IArrayType MakeArrayType( int rank = 1 ) => this.Compilation.Factory.MakeArrayType( this._typeParameterSymbol, rank ); // TODO: GenericContext?

        public IPointerType MakePointerType() => this.Compilation.Factory.MakePointerType( this._typeParameterSymbol );

        public IType ToNullable() => this.Compilation.Factory.MakeNullableType( this, true );

        public ITypeParameter ToNonNullable() => (ITypeParameter) this.Compilation.Factory.MakeNullableType( this, false );

        IType IType.ToNonNullable() => this.ToNonNullable();

        public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

        public override string ToString() => this.ContainingDeclaration + "/" + this.Name;

        public IType Accept( TypeRewriter visitor ) => visitor.Visit( this );

        public override int GetHashCode() => this.Compilation.CompilationContext.SymbolComparer.GetHashCode( this.Symbol );

        [Memo]
        private IFullRef<ITypeParameter> Ref => this.RefFactory.FromSymbol<ITypeParameter>( this._typeParameterSymbol );

        private protected override IFullRef<IDeclaration> ToFullDeclarationRef() => this.Ref;

        IRef<ITypeParameter> ITypeParameter.ToRef() => this.Ref;

        [Memo]
        public IType ResolvedType => this.GenericContext.IsEmptyOrIdentity ? this : this.GenericContext.Map( this );

        public TypeParameterKind TypeParameterKind
            => this.ContainingDeclaration.DeclarationKind switch
            {
                DeclarationKind.NamedType => TypeParameterKind.Type,
                DeclarationKind.Method => TypeParameterKind.Method,
                _ => throw new AssertionFailedException()
            };

        IRef<IType> IType.ToRef() => this.Ref;
    }
}