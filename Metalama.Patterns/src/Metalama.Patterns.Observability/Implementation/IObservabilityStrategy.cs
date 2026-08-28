// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;
using Metalama.Framework.Utilities;

namespace Metalama.Patterns.Observability.Implementation;

// Deliberately not [ImmutableType]. The interface is implemented both by the strategy that an options object holds,
// which is stateless, and by ClassicObservabilityStrategyImpl, which is a per-target worker constructed for one
// aspect application and holding the dictionaries, promises and code model references it needs while it runs. The
// contract cannot bind both, and separating them is a design change of its own.
[Durable]
[CompileTime]
public interface IObservabilityStrategy : ICompileTimeSerializable
{
    void BuildAspect( IAspectBuilder<INamedType> aspectBuilder );
}