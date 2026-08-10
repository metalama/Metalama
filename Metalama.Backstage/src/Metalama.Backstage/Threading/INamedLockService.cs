// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
#if METALAMA_BACKSTAGE
using Metalama.Backstage.Extensibility;
#endif
using System;
using System.Threading;

namespace Metalama.Backstage.Threading;

/// <summary>
/// Creates locks that are identified by a name and that, when the operating system permits it, are shared by all
/// processes running on the current machine.
/// </summary>
/// <remarks>
/// <para>
/// This service abstracts inter-process synchronization for the same reason as
/// <c>Metalama.Backstage.Infrastructure.IFileSystem</c> abstracts the file system: so that a test can substitute
/// an implementation that is isolated from the rest of the machine, that is deterministic, and that verifies the
/// locking discipline of the code under test. The production implementation, <see cref="NamedLockService"/>, is
/// backed by a named <see cref="System.Threading.Mutex"/>.
/// </para>
/// <para>
/// The source file declaring this interface is compiled into several assemblies, because the earliest code of the
/// build pipeline needs named locks and cannot reference any assembly. Only the assembly that defines the
/// <c>METALAMA_BACKSTAGE</c> compilation symbol derives the interface from <c>IBackstageService</c> and therefore
/// participates in dependency injection. The other assemblies instantiate <see cref="NamedLockService"/> directly.
/// </para>
/// <para>
/// This service is a factory of locks and is not itself a lock. The caller owns the <see cref="INamedLock"/>
/// returned by <see cref="GetLock"/> and must dispose it. The service caches nothing, because the set of names is
/// unbounded and caching would leak one operating system handle per distinct name. A component that repeatedly
/// locks a small and known set of names is therefore expected to create its locks once and to keep them for its
/// whole lifetime, whereas a component that locks arbitrary paths is expected to create, use and dispose the lock
/// in a single operation.
/// </para>
/// </remarks>
[PublicAPI]
public interface INamedLockService
#if METALAMA_BACKSTAGE
    : IBackstageService
#endif
{
    /// <summary>
    /// Gets an object representing the lock of a given name. This method does not acquire the lock. It only
    /// creates the object that represents it, which is comparatively expensive because it opens or creates an
    /// operating system object.
    /// </summary>
    /// <param name="name">
    /// The name of the operating system object, used verbatim. The caller is responsible for the prefix that
    /// scopes the name, for hashing a path into a name of acceptable length, and for using the same name in every
    /// process that protects the same resource. Composing the name is left to the caller because the callers of
    /// this service do not agree on a scheme: the earliest code of the build pipeline cannot reference the hashing
    /// library that the rest of the product uses.
    /// </param>
    /// <param name="cancellationToken">A token that aborts the creation of the operating system object.</param>
    /// <returns>The lock. The caller must dispose it when it no longer needs it.</returns>
    /// <remarks>
    /// This method never returns <see langword="null"/>, and throws only <see cref="OperationCanceledException"/>
    /// when <paramref name="cancellationToken"/> is cancelled. When the operating system cannot provide a named
    /// object, which happens on Unix when the directory backing the named objects is unusable, the returned lock
    /// excludes only the threads of the current process, and a <see cref="LockEventKind.Degraded"/> event is
    /// reported. Callers must be correct, although possibly less efficient, under that degradation, because
    /// failing to build is a worse outcome than losing mutual exclusion between processes that are rarely
    /// concurrent.
    /// </remarks>
    INamedLock GetLock( string name, CancellationToken cancellationToken = default );
}
