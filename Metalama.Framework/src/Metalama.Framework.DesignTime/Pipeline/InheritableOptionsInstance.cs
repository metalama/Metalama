// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.HierarchicalOptions;
using Metalama.Framework.Options;
using Metalama.Framework.Utilities;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.DesignTime.Pipeline;

/// <remarks>
/// The suppression is on the type rather than on the <c>Options</c> parameter, because a diagnostic reported at a
/// positional record parameter is resolved by Roslyn to the parameter and not to the property that the parameter
/// generates, so <c>[property: SuppressMessage]</c> does not reach it. This is coarser than the per-member
/// suppressions used elsewhere; it is acceptable here only because the sole other member, <c>Key</c>, is of a type
/// that is itself marked, so there is little for the wider suppression to hide.
/// </remarks>
[Durable]
[SuppressMessage(
    "Metalama",
    "LAMA0876:An interface or abstract type used by a durable type is not marked [Durable]",
    Justification =
        "Marking IHierarchicalOptions would require every options class a user writes to be durable, which is a "
        + "decision about the public contract of the framework. See \"Should the contract propagate to the "
        + "user-implementable interfaces?\" in design-time-memory.md." )]
internal sealed record InheritableOptionsInstance( HierarchicalOptionsKey Key, IHierarchicalOptions Options );