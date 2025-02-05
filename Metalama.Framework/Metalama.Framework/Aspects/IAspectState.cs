// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Aspects;

/// <summary>
/// An empty interface that must be implemented by objects assigned to the <see cref="IAspectBuilder.AspectState"/> property of the
/// <see cref="IAspectBuilder"/> interface.
/// </summary>
[CompileTime]
public interface IAspectState : ICompileTimeSerializable;