// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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