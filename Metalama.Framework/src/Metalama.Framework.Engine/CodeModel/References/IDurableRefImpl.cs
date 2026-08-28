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
    /// Gets a value indicating whether the current reference holds a reference to a compilation.
    /// </summary>
    /// <remarks>
    /// A durable reference is usually identified by a serializable identifier and holds no reference to a compilation,
    /// which is what allows it to be stored in an object that outlives a single request. During a batch compilation it
    /// stores instead the reference it was created from, because the compilation lives until the build ends. This
    /// distinction is used by the code that analyzes memory retention, in particular by
    /// <c>UserCodeRetentionPolicy</c>.
    /// </remarks>
    bool ReachesCompilation { get; }

    IFullRef ToFullRef( RefFactory refFactory );
}
