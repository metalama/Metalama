// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Patterns.Observability.Implementation;

/// <summary>
/// Builds the aspect on one target type. An implementation is created by
/// <see cref="IObservabilityStrategy.BuildAspect"/>, used once and discarded.
/// </summary>
/// <remarks>
/// Separate from <see cref="IObservabilityStrategy"/>, which is held by an options object and is therefore durable and
/// immutable. An implementation is the opposite: it is constructed for one aspect application, from the aspect builder
/// of that application, and it holds the code model references, dictionaries and promises it needs while it runs. One
/// interface cannot carry both requirements.
/// </remarks>
[CompileTime]
internal interface IObservabilityStrategyImplementation
{
    void BuildAspect( IAspectBuilder<INamedType> aspectBuilder );
}
