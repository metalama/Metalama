// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Serialization;

namespace Metalama.Patterns.Observability.Implementation;

[CompileTime]
public interface IObservabilityStrategy : ICompileTimeSerializable
{
    void BuildAspect( IAspectBuilder<INamedType> aspectBuilder );
}