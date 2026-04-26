// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Engine.Advising;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.AdviceImpl.Initialization;

internal sealed class AddInitializerAdviceResult : AdviceResult, IAddInitializerAdviceResult
{
    public AddInitializerAdviceResult( AdviceOutcome outcome, IAdviceFactoryImpl adviceFactory, ImmutableArray<Diagnostic> diagnostics = default ) : base(
        AdviceKind.AddInitializer,
        outcome,
        adviceFactory,
        diagnostics ) { }

    public static AddInitializerAdviceResult Skipped( IAdviceFactoryImpl adviceFactory ) => new( AdviceOutcome.Skipped, adviceFactory );
}