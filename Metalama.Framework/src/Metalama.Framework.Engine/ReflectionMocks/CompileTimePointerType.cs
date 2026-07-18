// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    /// <summary>
    /// A <see cref="CompileTimeType"/> that stands for a pointer type. Its name, namespace and full name are all derived
    /// from the pointed-at type, so none of them is stored.
    /// </summary>
    internal sealed class CompileTimePointerType : CompileTimeType
    {
        private readonly Type _pointedAtType;

        internal CompileTimePointerType( SerializableTypeId? typeId, Type pointedAtType )
            : base( typeId )
        {
            this._pointedAtType = pointedAtType;
        }

        // Reflection reports a pointer's Namespace as that of the type it points at (e.g. typeof(int*).Namespace == "System").
        public override string? Namespace => this._pointedAtType.Namespace;

        public override string Name => this._pointedAtType.Name + "*";

        public override string FullName => this._pointedAtType.FullName + "*";

        public override string ToString() => this._pointedAtType + "*";

        // A pointer has no declaring assembly of its own; it belongs to the assembly of the type it points at.
        internal override string? AssemblyName => (this._pointedAtType as CompileTimeType)?.AssemblyName ?? this._pointedAtType.Assembly.GetName().Name;

        protected override bool IsPointerImpl() => true;

        protected override bool HasElementTypeImpl() => true;

        public override Type GetElementType() => this._pointedAtType;

        public override bool ContainsGenericParameters => this._pointedAtType.ContainsGenericParameters;
    }
}
