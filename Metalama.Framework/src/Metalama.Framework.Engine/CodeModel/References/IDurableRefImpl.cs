// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// The implementation interface of <see cref="IDurableRef"/>, in the same relationship to it as <see cref="ISdkRef"/>
/// is to <see cref="IRef"/>: it carries the members that are expressed in engine types and that therefore cannot appear
/// on the public interface.
/// </summary>
internal interface IDurableRefImpl : ISdkRef, IDurableRef
{
    string Id { get; }

    /// <summary>
    /// Gets a value indicating whether the current reference reaches a compilation.
    /// </summary>
    /// <remarks>
    /// A durable reference is normally backed by an identifier and reaches nothing, which is what makes it safe to
    /// store in an object outliving a single request. During a batch compilation it may instead hold the reference it
    /// was made from, because the compilation outlives every object the run produces. The distinction matters to
    /// whatever reasons about retention, in particular to <c>UserCodeRetentionPolicy</c>.
    /// </remarks>
    bool ReachesCompilation { get; }

    IFullRef ToFullRef( RefFactory refFactory );
}
