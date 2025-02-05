// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Serialization
{
    /// <summary>
    /// An attribute that marks a field of a type that implements <see cref="ICompileTimeSerializable"/> as non-serialized.
    /// </summary>
    [AttributeUsage( AttributeTargets.Field | AttributeTargets.Property )]
    public sealed class NonCompileTimeSerializedAttribute : Attribute;
}