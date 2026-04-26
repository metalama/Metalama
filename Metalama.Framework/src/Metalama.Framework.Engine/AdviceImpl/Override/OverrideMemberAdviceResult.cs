// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Advising;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Override;

/// <summary>
/// An <see cref="AdviceResult"/> that does not have any property.
/// </summary>
internal sealed class OverrideMemberAdviceResult<TMember> : AdviceResult, IOverrideAdviceResult<TMember>
    where TMember : class, IMember
{
    private readonly IRef<TMember>? _declaration;

    public OverrideMemberAdviceResult(
        AdviceKind adviceKind,
        AdviceOutcome outcome,
        IAdviceFactoryImpl adviceFactory,
        IRef<TMember>? declaration = null,
        ImmutableArray<Diagnostic> reportedDiagnostics = default ) : base( adviceKind, outcome, adviceFactory, reportedDiagnostics )
    {
        this._declaration = declaration;
    }

    public TMember Declaration => this.Resolve( this._declaration );

    public OverrideAccessorAdviceResult<TMember> GetAccessor( Func<TMember, IMethod?> getAccessor ) => new( this, getAccessor );

    public static OverrideMemberAdviceResult<TMember> Skipped( AdviceKind adviceKind, IAdviceFactoryImpl adviceFactory )
        => new( adviceKind, AdviceOutcome.Skipped, adviceFactory );
}