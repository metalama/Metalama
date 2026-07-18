// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.Utilities;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    /// <summary>
    /// A <see cref="CompileTimeType"/> that stands for a named type: a class, struct, enum, interface or delegate,
    /// possibly a constructed generic type or an open generic type definition.
    /// </summary>
    /// <remarks>
    /// The type stores its arity-marked metadata name (e.g. <c>List`1</c>), its namespace, its declaring type (when
    /// nested), its generic definition (when constructed) and its generic arguments; from those it computes
    /// <see cref="Name"/>, <see cref="Namespace"/>, <see cref="FullName"/> and <see cref="ToString"/> rather than
    /// storing them.
    /// <para>
    /// Only a <em>constructed</em> type carries generic arguments. An open definition keeps an empty argument list,
    /// unlike reflection, where <c>typeof(List&lt;&gt;).GetGenericArguments()</c> is <c>[T]</c>: a type parameter's
    /// own declaring type is the definition, so populating them would re-enter the factory for the definition that is
    /// still being built. See <c>CompileTimeTypeFactory.CreateFromSymbol</c>.
    /// </para>
    /// </remarks>
    internal sealed class CompileTimeNamedType : CompileTimeType
    {
        private readonly string _metadataName;
        private readonly string? _namespace;
        private readonly CompileTimeNamedType? _declaringType;
        private readonly CompileTimeNamedType? _genericTypeDefinition;
        private readonly ImmutableArray<Type> _genericArguments;

        public bool? IsEnumOrNull { get; }

        public bool? IsValueTypeOrNull { get; }

        internal CompileTimeNamedType(
            SerializableTypeId? typeId,
            string metadataName,
            string? ns,
            string assemblyName,
            bool? isEnum,
            bool? isValueType,
            CompileTimeNamedType? genericTypeDefinition,
            ImmutableArray<Type> genericArguments,
            CompileTimeNamedType? declaringType = null )
            : base( typeId )
        {
            this._metadataName = metadataName;
            this._namespace = ns;
            this.AssemblyName = assemblyName;
            this.IsEnumOrNull = isEnum;
            this.IsValueTypeOrNull = isValueType;
            this._genericTypeDefinition = genericTypeDefinition;
            this._genericArguments = genericArguments.IsDefault ? ImmutableArray<Type>.Empty : genericArguments;
            this._declaringType = declaringType;
        }

        public override string Name => this._metadataName;

        // Reflection reports a nested type's Namespace as that of its (outermost) declaring type.
        public override string? Namespace => this._declaringType?.Namespace ?? this._namespace;

        internal override string? AssemblyName { get; }

        /// <summary>
        /// Gets the type this type is nested in, or <c>null</c> if it is not nested. Reflection separates a nested type
        /// from its declaring type by <c>+</c> in <see cref="Type.FullName"/> -- e.g. <c>Ns.Outer+Inner</c>.
        /// </summary>
        public override Type? DeclaringType => this._declaringType;

        // The name qualified by the declaring type (nested) or the namespace (top-level), i.e. the full name without any
        // generic-argument list. Reflection uses '+' between a nested type and its declaring type.
        private string GetQualifiedName( Func<CompileTimeType, string> nameOf )
            => this._declaringType != null
                ? nameOf( this._declaringType ) + "+" + this._metadataName
                : string.IsNullOrEmpty( this._namespace ) ? this._metadataName : this._namespace + "." + this._metadataName;

        public override string FullName
        {
            get
            {
                var qualifiedName = this.GetQualifiedName( static t => t.FullName );

                // Only a *constructed* generic type appends its arguments to FullName. An open definition does not
                // (typeof(List<>).FullName is 'System.Collections.Generic.List`1', with no argument list).
                return this._genericTypeDefinition == null
                    ? qualifiedName
                    : qualifiedName + "[" + string.Join( ",", this._genericArguments.SelectAsArray( a => a.FullName ) ) + "]";
            }
        }

        public override string ToString()
        {
            var qualifiedName = this.GetQualifiedName( static t => t.ToString() );

            // Unlike FullName, ToString renders the argument list of a constructed type ('List`1[System.Int32]').
            // An open definition has no arguments here (see the remarks on this class), so it renders as 'List`1',
            // where the CLR would append '[T]'.
            return this._genericArguments.IsEmpty
                ? qualifiedName
                : qualifiedName + "[" + string.Join( ",", this._genericArguments.SelectAsArray( a => a.ToString() ) ) + "]";
        }

        public override bool IsEnum => this.IsEnumOrNull ?? throw new AssertionFailedException( $"We don't know if the type '{this}' is an enum." );

        protected override bool IsValueTypeImpl()
            => this.IsValueTypeOrNull ?? throw new AssertionFailedException( $"We don't know if the type '{this}' is a value type." );

        // A type is generic both when it is an open definition and when it is constructed. The arity backtick in the
        // metadata name marks a generic type; a constructed type additionally has a generic definition.
        public override bool IsGenericType => this._metadataName.IndexOfOrdinal( '`' ) >= 0;

        public override bool IsGenericTypeDefinition => this._genericTypeDefinition == null && this.IsGenericType;

        public override Type GetGenericTypeDefinition()
            => this.IsGenericTypeDefinition ? this : this._genericTypeDefinition ?? throw this.CreateNotSupportedException();

        public override Type[] GetGenericArguments() => this._genericArguments.ToArray();

        // True for an open definition (its arguments ARE type parameters) and for a constructed type any of whose
        // arguments still contains one. Matches RuntimeType.
        public override bool ContainsGenericParameters
            => this.IsGenericTypeDefinition || this._genericArguments.Any( a => a.ContainsGenericParameters );

        public override Type MakeGenericType( params Type[] typeArguments )
            => new CompileTimeNamedType(
                null,
                this._metadataName,
                this._namespace,
                this.AssemblyName.AssertNotNull(),

                // A constructed generic type is never an enum, so this is known even when the definition's kind is not.
                false,
                this.IsValueTypeOrNull,
                this,
                typeArguments.ToImmutableArray(),
                this._declaringType );
    }
}
