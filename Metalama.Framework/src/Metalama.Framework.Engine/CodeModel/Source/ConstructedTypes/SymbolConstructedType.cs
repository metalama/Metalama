// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Code.Comparers;
using Metalama.Framework.Code.Types;
using Metalama.Framework.Engine.CodeModel.Abstractions;
using Metalama.Framework.Engine.CodeModel.GenericContexts;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;
using SpecialType = Metalama.Framework.Code.SpecialType;
using TypeKind = Metalama.Framework.Code.TypeKind;

namespace Metalama.Framework.Engine.CodeModel.Source.ConstructedTypes
{
    internal abstract class SymbolConstructedType<T> : ITypeImpl, ISymbolBasedCompilationElement
        where T : ITypeSymbol
    {
        protected GenericContext GenericContextForSymbolMapping { get; }

        public bool SymbolMustBeMapped => !this.GenericContextForSymbolMapping.IsEmptyOrIdentity;

        IGenericContext ISymbolBasedCompilationElement.GenericContextForSymbolMapping => this.GenericContextForSymbolMapping;

        public CompilationModel Compilation { get; }

        DeclarationKind ICompilationElement.DeclarationKind => DeclarationKind.Type;

        public ICompilationElement? Translate(
            CompilationModel newCompilation,
            IGenericContext? genericContext = null,
            Type? interfaceType = null )
            => newCompilation.Factory.GetCompilationElement( this.Symbol, RefTargetKind.Default, genericContext, interfaceType );

        ISymbol ISymbolBasedCompilationElement.Symbol => this.Symbol;

        protected T Symbol { get; }

        protected SymbolConstructedType( T symbol, CompilationModel compilation, GenericContext genericContextForSymbolMapping )
        {
            this.GenericContextForSymbolMapping = genericContextForSymbolMapping;
            this.Compilation = compilation;
            this.Symbol = symbol;
        }

        public string ToDisplayString( CodeDisplayFormat? format = null, CodeDisplayContext? context = null )
            => DisplayStringFormatter.Format( this, format, context, this.GenericContextForSymbolMapping );

        [Memo]
        private IRef<IType> Ref => this.Symbol.ToRef( this.Compilation.RefFactory, this.GenericContextForSymbolMapping );

        IRef<IType> IType.ToRef() => this.Ref;

        public abstract TypeKind TypeKind { get; }

        public SpecialType SpecialType => this.Symbol.SpecialType.ToOurSpecialType();

        public Type ToType() => this.Compilation.Factory.GetReflectionType( this.Symbol ); // TODO: mapping?

        public bool? IsReferenceType => this.Symbol.IsReferenceType;

        public bool? IsNullable => this.Symbol.IsNullable();

        public bool Equals( SpecialType specialType ) => this.SpecialType == specialType;

        public bool Equals( IType? otherType, TypeComparison typeComparison )
            => this.Compilation.Comparers.GetTypeComparer( typeComparison ).Equals( this, otherType ); // TODO: Mapping

        public bool Equals( Type? otherType, TypeComparison typeComparison = TypeComparison.Default )
            => otherType != null && this.Compilation.Comparers.GetTypeComparer( typeComparison )
                .Equals( this, this.Compilation.Factory.GetTypeByReflectionType( otherType ) );

        public IArrayType MakeArrayType( int rank = 1 ) => this.Compilation.Factory.MakeArrayType( this.Symbol, rank );

        public IPointerType MakePointerType() => this.Compilation.Factory.MakePointerType( this.Symbol );

        public IType ToNullable() => this.Compilation.Factory.MakeNullableType( this, true );

        public IType ToNonNullable() => this.Compilation.Factory.MakeNullableType( this, false );

        ICompilation ICompilationElement.Compilation => this.Compilation;

        public bool Equals( IType? other ) => this.Equals( other, TypeComparison.Default );

        public override bool Equals( object? obj )
            => obj switch
            {
                IType otherType => this.Equals( otherType ),
                Type otherType => this.Equals( otherType ),
                _ => false
            };

        public override string ToString() => this.ToDisplayString();

        public abstract IType Accept( TypeRewriter visitor );

        public override int GetHashCode() => this.Compilation.CompilationContext.SymbolComparer.GetHashCode( this.Symbol );
    }
}