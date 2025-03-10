// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime.Serialization.Serializers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.CodeModel.References
{
    /// <summary>
    /// Base implementation of <see cref="IRef"/> for <see cref="IAttribute"/>.
    /// </summary>
    internal abstract class AttributeRef : IRef<IAttribute>, IEquatable<AttributeRef>, IRefImpl
    {
        // Note: These references are not necessarily full refs in case of deserialization.
        [PublicAPI]
        public abstract IRef<IDeclaration> ContainingDeclaration { get; }

        public abstract IRef<INamedType> AttributeType { get; }

        public abstract string? Name { get; }

        SerializableDeclarationId IRef.ToSerializableId() => throw new NotSupportedException();

        IRef<TOut> IRef.As<TOut>() => this as IRef<TOut> ?? throw new NotSupportedException();

        public IAttribute GetTarget( ICompilation compilation )
        {
            if ( !this.TryGetTarget( (CompilationModel) compilation, out var attribute ) )
            {
                throw new AssertionFailedException( "Attempt to resolve an invalid custom attribute." );
            }

            return attribute;
        }

        public bool IsDurable => false;

        IRef IRefImpl.ToDurable() => throw new NotSupportedException();

        ICompilationElement? IRef.GetTargetInterface( ICompilation compilation, Type? interfaceType, IGenericContext? genericContext, bool throwIfMissing )
        {
            var target = this.GetTargetOrNull( compilation );

            if ( target == null && throwIfMissing )
            {
                throw new InvalidOperationException();
            }

            return target;
        }

        private IAttribute? GetTargetOrNull( ICompilation compilation )
        {
            if ( !this.TryGetTarget( (CompilationModel) compilation, out var attribute ) )
            {
                return null;
            }

            return attribute;
        }

        protected abstract AttributeSyntax? AttributeSyntax { get; }

        public abstract bool TryGetTarget( CompilationModel compilation, [NotNullWhen( true )] out IAttribute? attribute );

        public abstract bool TryGetAttributeSerializationDataKey( [NotNullWhen( true )] out object? serializationDataKey );

        public abstract bool TryGetAttributeSerializationData( [NotNullWhen( true )] out AttributeSerializationData? serializationData );

        ISymbol ISdkRef.GetSymbol( Compilation compilation, bool ignoreAssemblyKey ) => throw new NotSupportedException();

        public bool IsSyntax( AttributeSyntax attribute ) => this.AttributeSyntax == attribute;

        public bool Equals( IRef? other, RefComparison comparison = RefComparison.Default )
        {
            if ( comparison != RefComparison.Default )
            {
                throw new NotSupportedException( "Non-default comparison of attributes is not supported." );
            }

            if ( other is not AttributeRef otherAttributeRef )
            {
                return false;
            }

            return this.Equals( otherAttributeRef );
        }

        public int GetHashCode( RefComparison comparison )
            => comparison switch
            {
                RefComparison.Default => this.GetHashCode(),
                _ => throw new NotSupportedException( "Non-default comparison of attributes is not supported." )
            };

        public virtual bool Equals( AttributeRef? other )
        {
            if ( other == null )
            {
                return false;
            }

            var thisSyntax = this.AttributeSyntax;
            var otherSyntax = other.AttributeSyntax;

            if ( thisSyntax == null )
            {
                throw new AssertionFailedException( "Expected that the method would be overridden." );
            }

            return thisSyntax.Equals( otherSyntax );
        }

        protected abstract int GetHashCodeCore();

        bool IEquatable<IRef>.Equals( IRef? other ) => other is AttributeRef otherAttributeRef && this.Equals( otherAttributeRef );

        public override int GetHashCode() => this.GetHashCodeCore();
    }
}