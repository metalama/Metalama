// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Engine.Advising;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.AdviceImpl.InterfaceImplementation;

internal sealed class ImplementInterfaceAdviceResult : AdviceResult, IImplementInterfaceAdviceResult
{
    public ImplementInterfaceAdviceResult(
        AdviceOutcome outcome,
        IAdviceFactoryImpl adviceFactory,
        IReadOnlyCollection<IInterfaceImplementationResult>? interfaces = null,
        IReadOnlyCollection<IInterfaceMemberImplementationResult>? interfaceMembers = null,
        ImmutableArray<Diagnostic> reportedDiagnostics = default ) : base( AdviceKind.ImplementInterface, outcome, adviceFactory, reportedDiagnostics )
    {
        this.Interfaces = interfaces ?? [];
        this.InterfaceMembers = interfaceMembers ?? [];
    }

    public IReadOnlyCollection<IInterfaceImplementationResult> Interfaces { get; }

    public IReadOnlyCollection<IInterfaceMemberImplementationResult> InterfaceMembers { get; }

    public IInterfaceImplementationAdviser ExplicitMembers
        => this.Interfaces.FirstOrDefault()?.ExplicitMembers
           ?? throw new InvalidOperationException( "No interfaces were implemented, so explicit implementation is not possible." );
}