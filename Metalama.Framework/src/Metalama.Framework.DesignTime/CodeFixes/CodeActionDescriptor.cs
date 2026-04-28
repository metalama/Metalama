// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.DesignTime.Rpc;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.CodeFixes;

/// <summary>
/// Wire-format descriptor for a code action or code-action menu rendered by the Visual Studio light-bulb.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately a <em>concrete</em> record (no polymorphism). It crosses the analyzer ↔ devenv RPC boundary
/// without depending on cross-process / cross-TFM assembly-identity round-tripping — which is the fragility
/// behind issue #1606 and the reason Roslyn-disable-the-provider failures surface in production. Owning this
/// type in <c>Metalama.Framework.DesignTime</c> (rather than each extension package) gives one canonical
/// wire shape that all code-action-producing extensions can share.
/// </para>
/// <para>
/// The descriptor carries only what the IDE needs to <em>render</em>: the user-visible <see cref="Title"/> and
/// an optional <see cref="NestedItems"/> tree for menus. Plus an opaque payload (<see cref="PayloadTypeName"/>
/// + <see cref="Payload"/>) that round-trips through devenv unchanged. Devenv never deserializes
/// <see cref="Payload"/>; only the analyzer does — typically by looking <see cref="PayloadTypeName"/> up in an
/// extension-private dictionary of <c>(name → Type)</c> and calling <c>MessagePackHelper.Deserialize&lt;TConcrete&gt;</c>.
/// This keeps the wire boundary free of polymorphism while letting the analyzer dispatch without any per-click cache.
/// </para>
/// </remarks>
[RpcContract]
public sealed record CodeActionDescriptor
{
    /// <summary>
    /// User-visible title shown by the IDE.
    /// </summary>
    public string Title { get; init; } = "";

    /// <summary>
    /// Children when this descriptor represents a menu. Empty for leaf actions.
    /// </summary>
    public ImmutableArray<CodeActionDescriptor> NestedItems { get; init; } = ImmutableArray<CodeActionDescriptor>.Empty;

    /// <summary>
    /// Analyzer-side identifier for the concrete payload type — opaque to devenv. Used by the analyzer to
    /// dispatch <see cref="Payload"/> deserialization without polymorphism. The naming convention is owned by
    /// the extension that produces the descriptor (e.g., a short name like <c>nameof(UserCodeActionModel)</c>
    /// resolved against an extension-private <c>Dictionary&lt;string, Type&gt;</c>). <c>null</c> for menus.
    /// </summary>
    public string? PayloadTypeName { get; init; }

    /// <summary>
    /// Opaque-to-devenv bytes describing the leaf action. Devenv stores and round-trips them unchanged on
    /// invoke. The analyzer deserializes via <c>MessagePackHelper.Deserialize&lt;TConcrete&gt;</c> using the
    /// type resolved from <see cref="PayloadTypeName"/>. <c>null</c> for menus.
    /// </summary>
    public byte[]? Payload { get; init; }

    /// <summary>
    /// True when this descriptor is a menu (has at least one nested item). False for leaf code actions.
    /// </summary>
    public bool IsMenu => !this.NestedItems.IsDefaultOrEmpty;
}
