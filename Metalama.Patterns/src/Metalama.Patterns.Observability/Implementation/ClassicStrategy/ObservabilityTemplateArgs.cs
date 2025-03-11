// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Patterns.Observability.Configuration;

namespace Metalama.Patterns.Observability.Implementation.ClassicStrategy;

/*
 * NB: ObservabilityTemplateArgs members must not hold any reference to the IAspectBuilder<> passed
 * to BuildAspect. Members must be immutable, with the exception of cached computed values. Lazy behaviour
 * should be avoided.
 */

// ReSharper disable NotAccessedPositionalProperty.Global
/// <summary>
/// Immutable context for template execution.
/// </summary>
[CompileTime]
internal sealed record ObservabilityTemplateArgs(
    ObservabilityOptions CommonOptions,
    ClassicObservabilityStrategyOptions Options,
    INamedType TargetType,
    Assets Assets,
    InpcInstrumentationKindLookup InpcInstrumentationKindLookup,
    ClassicObservableTypeInfo ObservableTypeInfo,
    IMethod? OnObservablePropertyChangedMethod,
    IMethod OnPropertyChangedInvocableMethod,
    IMethod? OnChildPropertyChangedMethod,
    IMethod? BaseOnPropertyChangedMethod,
    IMethod? BaseOnChildPropertyChangedMethod,
    IMethod? BaseOnObservablePropertyChangedMethod );