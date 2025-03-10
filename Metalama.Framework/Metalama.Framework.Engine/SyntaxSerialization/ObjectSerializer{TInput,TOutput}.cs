// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;

namespace Metalama.Framework.Engine.SyntaxSerialization
{
    /// <summary>
    /// As <see cref="ObjectSerializer"/>, except strongly-typed.
    /// </summary>
    internal abstract class ObjectSerializer<TInput, TOutput> : ObjectSerializer
        where TInput : TOutput
    {
        /// <inheritdoc />
        public sealed override ExpressionSyntax Serialize( object obj, SyntaxSerializationContext serializationContext )
            => this.Serialize( (TInput) obj, serializationContext );

        /// <summary>
        /// Serializes an object of a type supported by this object serializer into a Roslyn expression that creates such an object.
        /// </summary>
        /// <param name="obj">An object to serialize. Not null.</param>
        /// <param name="serializationContext"></param>
        /// <returns>An expression that creates such an object.</returns>
        [UsedImplicitly]
        public abstract ExpressionSyntax Serialize( TInput obj, SyntaxSerializationContext serializationContext );

        protected ObjectSerializer( SyntaxSerializationService service ) : base( service ) { }

        public sealed override Type InputType => typeof(TInput);

        public override Type? OutputType => typeof(TOutput);
    }
}