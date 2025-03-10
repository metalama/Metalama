// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.CompileTimeContracts
{
    /// <summary>
    /// Custom attribute added by the Metalama template compiler to the compile-time assembly. It stores original
    /// path of the source file that contained the declaration (typically a fabric). This is used to order the fabrics by depth
    /// of directory.
    /// </summary>
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface )]
    [PublicAPI]
    public sealed class OriginalPathAttribute : Attribute
    {
        public string Path { get; }

        public OriginalPathAttribute( string path )
        {
            this.Path = path;
        }
    }
}