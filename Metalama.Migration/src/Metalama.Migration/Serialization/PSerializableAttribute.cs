// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;
using PostSharp.Aspects;
using PostSharp.Extensibility;
using System;

namespace PostSharp.Serialization
{
    /// <summary>
    /// In Metalama, make the type implement the <see cref="ICompileTimeSerializable"/> interface.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Assembly )]
    [MulticastAttributeUsage( MulticastTargets.Class | MulticastTargets.Struct )]
    [RequirePostSharp( null, "PortableSerializer" )]
    [LinesOfCodeAvoided( 0 )]
    public sealed class PSerializableAttribute : MulticastAttribute, IAspect { }
}