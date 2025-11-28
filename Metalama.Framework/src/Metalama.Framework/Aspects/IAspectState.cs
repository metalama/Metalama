// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Aspects;

/// <summary>
/// An empty interface that must be implemented by objects assigned to the <see cref="IAspectBuilder.AspectState"/> property of the
/// <see cref="IAspectBuilder"/> interface.
/// </summary>
/// <remarks>
/// <para><b>Writing aspect state:</b> Set the <see cref="IAspectBuilder.AspectState"/> property during <see cref="IAspect{T}.BuildAspect"/>
/// to store arbitrary data specific to the target declaration.</para>
/// <para><b>Reading aspect state:</b> Access <see cref="IAspectInstance.AspectState"/> to read your own aspect's state (e.g., in later layers).
/// To access predecessor aspect states, iterate through <see cref="IAspectPredecessor.Predecessors"/>, check <see cref="AspectPredecessor.Kind"/>,
/// and cast <see cref="AspectPredecessor.Instance"/> to <see cref="IAspectInstance"/> before accessing its <see cref="IAspectInstance.AspectState"/>.</para>
/// <para><b>Serialization requirement:</b> Aspect state must be compile-time serializable (via <see cref="ICompileTimeSerializable"/>) because
/// it may be persisted across projects when aspects are inherited or affect downstream projects through reference validation.</para>
/// </remarks>
/// <seealso cref="IAspectBuilder"/>
/// <seealso cref="IAspectInstance"/>
/// <seealso href="@aspects"/>
/// <seealso href="@sharing-state-with-advice"/>
[CompileTime]
public interface IAspectState : ICompileTimeSerializable;