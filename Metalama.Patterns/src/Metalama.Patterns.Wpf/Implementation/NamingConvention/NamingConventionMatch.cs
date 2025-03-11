// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Immutable;

namespace Metalama.Patterns.Wpf.Implementation.NamingConvention;

// ReSharper disable once NotAccessedPositionalProperty.Global
[CompileTime]
internal abstract record NamingConventionMatch( INamingConvention NamingConvention, IReadOnlyList<InspectedMember> InspectedMembers )
{
    private ImmutableArray<MemberMatchDiagnosticInfo> _members;

    public abstract NamingConventionOutcome Outcome { get; }

    protected abstract ImmutableArray<MemberMatchDiagnosticInfo> GetMemberDiagnostics();

    public ImmutableArray<MemberMatchDiagnosticInfo> Members => this._members.IsDefault ? this._members = this.GetMemberDiagnostics() : this._members;
}