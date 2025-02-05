// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CodeModel;

public partial class DerivedTypeIndex
{
    // We can't use IFullRef because we don't have a RefFactory when we build the DerivedTypeIndex.
    private readonly struct NamedTypeRef : IEquatable<NamedTypeRef>
    {
        public object? Value { get; }

        public IFullRef<INamedType> ToRef( RefFactory refFactory )
            => this.Value switch
            {
                INamedTypeSymbol symbol => refFactory.FromSymbol<INamedType>( symbol ),
                NamedTypeBuilderData builderData => builderData.ToRef(),
                _ => throw new AssertionFailedException()
            };

        public NamedTypeRef( IFullRef<INamedType> reference )
        {
            this.Value = reference switch
            {
                IIntroducedRef builtDeclarationRef => builtDeclarationRef.BuilderData,
                ISymbolRef symbolRef => symbolRef.Symbol,
                _ => throw new ArgumentException()
            };
        }

        public NamedTypeRef( INamedTypeSymbol namedTypeSymbol )
        {
            this.Value = namedTypeSymbol;
        }

        public NamedTypeRef( NamedTypeBuilderData builderData )
        {
            this.Value = builderData;
        }

        public bool Equals( NamedTypeRef other )
        {
            if ( this.Value is INamedTypeSymbol thisNamedTypeSymbol && other.Value is INamedTypeSymbol otherNamedTypeSymbol )
            {
                return thisNamedTypeSymbol.Equals( otherNamedTypeSymbol );
            }
            else if ( this.Value is NamedTypeBuilderData thisNamedTypeBuilderData && other.Value is NamedTypeBuilderData otherNamedTypeBuilderData )
            {
                return thisNamedTypeBuilderData.Equals( otherNamedTypeBuilderData );
            }
            else
            {
                return false;
            }
        }

        public override bool Equals( object? obj ) => obj is NamedTypeRef other && this.Equals( other );

        public override int GetHashCode() => this.Value?.GetHashCode() ?? 0;
    }
}