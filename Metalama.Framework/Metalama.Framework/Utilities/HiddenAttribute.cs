// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Utilities
{
    /// <summary>
    /// Specifies how the interface must be hidden by tools like our LinqPad adapter.
    /// </summary>
    [AttributeUsage( AttributeTargets.Interface | AttributeTargets.Property )]
    [PublicAPI]
    public sealed class HiddenAttribute : Attribute;
}