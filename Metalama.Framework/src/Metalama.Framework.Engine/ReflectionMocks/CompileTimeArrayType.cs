// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using System;

namespace Metalama.Framework.Engine.ReflectionMocks
{
    /// <summary>
    /// A <see cref="CompileTimeType"/> that stands for an array type. Its name, namespace and full name are all derived
    /// from the element type and the rank, so none of them is stored.
    /// </summary>
    internal sealed class CompileTimeArrayType : CompileTimeType
    {
        private readonly Type _elementType;
        private readonly int _rank;

        internal CompileTimeArrayType( SerializableTypeId? typeId, Type elementType, int rank )
            : base( typeId )
        {
            this._elementType = elementType;
            this._rank = rank;
        }

        // The '[]' / '[,]' suffix reflection appends to the element's name for a one-/multi-dimensional array.
        private string Brackets => this._rank == 1 ? "[]" : "[" + new string( ',', this._rank - 1 ) + "]";

        // Reflection reports an array's Namespace as that of its element type (e.g. typeof(int[]).Namespace == "System"),
        // even though the array symbol itself has no containing namespace.
        public override string? Namespace => this._elementType.Namespace;

        public override string Name => this._elementType.Name + this.Brackets;

        public override string FullName => this._elementType.FullName + this.Brackets;

        public override string ToString() => this._elementType + this.Brackets;

        // An array has no declaring assembly of its own; it belongs to the assembly of its element type.
        internal override string? AssemblyName => (this._elementType as CompileTimeType)?.AssemblyName ?? this._elementType.Assembly.GetName().Name;

        protected override bool IsArrayImpl() => true;

        protected override bool HasElementTypeImpl() => true;

        public override Type GetElementType() => this._elementType;

        public override bool ContainsGenericParameters => this._elementType.ContainsGenericParameters;

        public override int GetArrayRank() => this._rank;
    }
}
