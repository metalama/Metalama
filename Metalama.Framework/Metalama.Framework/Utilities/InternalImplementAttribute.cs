// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Utilities
{
    /// <summary>
    /// A custom attribute that means that the interface cannot be implemented by another assembly than
    /// the one that declared it, except if the referencing assembly sees the internals of the declaring assembly.
    /// </summary>
    [AttributeUsage( AttributeTargets.Interface )]
    public sealed class InternalImplementAttribute : Attribute;

    // TODO: Implement with an analyzer.
}