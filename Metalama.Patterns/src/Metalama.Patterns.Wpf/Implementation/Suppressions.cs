// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Diagnostics;

// ReSharper disable InconsistentNaming

namespace Metalama.Patterns.Wpf.Implementation;

[CompileTime]
internal static class Suppressions
{
    public static readonly SuppressionDefinition SuppressRemoveUnusedPrivateMembersIDE0051 = new( "IDE0051" );
}