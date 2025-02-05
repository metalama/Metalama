// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;

namespace Metalama.Framework.Engine.Utilities
{
    [AttributeUsage( AttributeTargets.Property )]
    [PublicAPI]
    public sealed class MemoAttribute : Attribute;
}