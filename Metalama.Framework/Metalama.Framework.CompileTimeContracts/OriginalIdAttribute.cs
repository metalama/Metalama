// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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