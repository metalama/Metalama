// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Pipeline.DesignTime;

// ReSharper disable NotAccessedPositionalProperty.Global
public sealed record TestDesignTimeAspectPipelineResult(
    bool Success,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<ScopedSuppression> Suppressions,
    ImmutableArray<IntroducedSyntaxTree> AdditionalSyntaxTrees );