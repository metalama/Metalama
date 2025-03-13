// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using PostSharp.Constraints;
using PostSharp.Reflection;
using System;

namespace PostSharp.Aspects
{
    /// <summary>
    /// In PostSharp, a binding was a run-time object that allowed the run-time code of the aspect to call the target code. In Metalama, aspects no longer
    /// have run-time code. Instead, they have templates that are expanded at compile time and generate run-time code. Templates can generate run-time code
    /// that accesses target code using dynamic code or invokers. For fields and properties, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IExpression.Value"/>.
    /// </summary>
    [InternalImplement]
    public interface ILocationBinding
    {
        /// <summary>
        /// In Metalama, get <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IExpression.Value"/>.
        /// </summary>
        object GetValue( ref object instance, Arguments index );

        /// <summary>
        /// In Metalama, set <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IExpression.Value"/>.
        /// </summary>
        void SetValue( ref object instance, Arguments index, object value );

        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>. If you need a run-time object, use
        ///  <see cref="IFieldOrProperty"/>.<see cref="IFieldOrProperty.ToFieldOrPropertyInfo"/>.
        /// </summary>
        LocationInfo LocationInfo { get; }

        /// <summary>
        /// In Metalama, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IHasType.Type"/>>. If you need a run-time object, use
        ///  <see cref="IType"/>.<see cref="IType.ToType"/>.
        /// </summary>
        Type LocationType { get; }

        /// <summary>
        /// There is no equivalent in Metalama.
        /// </summary>
        DeclarationIdentifier DeclarationIdentifier { get; }

        /// <summary>
        /// There is no equivalent in Metalama.
        /// </summary>
        void Execute<TPayload>( ILocationBindingAction<TPayload> action, ref TPayload payload );
    }

    /// <summary>
    /// In PostSharp, a binding was a run-time object that allowed the run-time code of the aspect to call the target code. In Metalama, aspects no longer
    /// have run-time code. Instead, they have templates that are expanded at compile time and generate run-time code. Templates can generate run-time code
    /// that accesses target code using dynamic code or invokers. For fields and properties, use <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IExpression.Value"/>.
    /// </summary>
    public interface ILocationBinding<T> : ILocationBinding
    {
        /// <summary>
        /// In Metalama, get <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IExpression.Value"/>.
        /// </summary>
        new T GetValue( ref object instance, Arguments index );

        /// <summary>
        /// In Metalama, set <see cref="meta"/>.<see cref="meta.Target"/>.<see cref="IMetaTarget.FieldOrProperty"/>.<see cref="IExpression.Value"/>.
        /// </summary>
        void SetValue( ref object instance, Arguments index, T value );
    }
}