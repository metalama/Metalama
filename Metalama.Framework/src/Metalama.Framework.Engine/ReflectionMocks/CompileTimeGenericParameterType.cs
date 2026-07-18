// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    /// <summary>
    /// A <see cref="CompileTimeType"/> that stands for a generic type parameter, i.e. the <c>T</c> of <c>Foo&lt;T&gt;</c>.
    /// A type parameter has no namespace, and its name, full name and string form are all the parameter name itself.
    /// </summary>
    internal sealed class CompileTimeGenericParameterType : CompileTimeType
    {
        private readonly Type? _declaringType;
        private readonly int _position;

        internal CompileTimeGenericParameterType( SerializableTypeId? typeId, string name, Type? declaringType, int position )
            : base( typeId )
        {
            this.Name = name;
            this._declaringType = declaringType;
            this._position = position;
        }

        public override string? Namespace => null;

        public override string Name { get; }

        public override string FullName => this.Name;

        public override string ToString() => this.Name;

        // A type parameter has no declaring assembly of its own; it belongs to the assembly of its declaring type.
        internal override string? AssemblyName => (this._declaringType as CompileTimeType)?.AssemblyName;

        public override bool IsGenericParameter => true;

        public override bool ContainsGenericParameters => true;

        public override Type? DeclaringType => this._declaringType;

        public override int GenericParameterPosition => this._position;

        // A type parameter is never a generic type, even though it belongs to one.
        public override bool IsGenericType => false;
    }
}
