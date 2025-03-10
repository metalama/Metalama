// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.CompileTimeContracts
{
    /// <summary>
    /// Custom attribute added by the Metalama template compiler to the compile-time assembly. It stores the original XML documentation
    /// id of the original class, typically a nested class that has been relocated out of its parent class.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class )]
    [PublicAPI]
    public sealed class OriginalIdAttribute : Attribute
    {
        public string Id { get; }

        public OriginalIdAttribute( string id )
        {
            this.Id = id;
        }
    }
}