// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
#if HAS_METALAMA_TESTING_HOOKS
using Metalama.Testing.Hooks;
#endif
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;

namespace Metalama.Backstage.Threading;

/// <summary>
/// The implementation of <see cref="INamedLockService"/> backed by the named synchronization objects of the
/// operating system.
/// </summary>
/// <remarks>
/// <para>
/// The source file declaring this class is compiled into several assemblies. It must therefore depend on nothing
/// but the base class library, which is why it reports its activity through the
/// <see cref="LockEventReported"/> event instead of through a logger, and why the composition of the lock name is
/// left to the caller.
/// </para>
/// <para>
/// This class replaces three divergent copies of the same logic, which used to live in <c>MutexHelper</c>, in
/// <c>DesignTimeEntryPointManager</c> and inline in <c>ResourceExtractor</c>. Two of them carried comments asking
/// for the copies to be kept in sync, and they had drifted apart in eight respects, the most consequential being
/// that the copy running earliest in the build pipeline had no fallback at all when the operating system refused
/// to create the object.
/// </para>
/// </remarks>
[PublicAPI]
#if METALAMA_BACKSTAGE
public
#else

// See the remark on the accessibility of INamedLockService.
internal
#endif
    sealed partial class NamedLockService : INamedLockService
{
    /// <summary>
    /// The time after which holding a lock is reported as <see cref="LockEventKind.HeldTooLong"/>.
    /// </summary>
    private const int _longHoldThresholdMilliseconds = 1000;

    /// <summary>
    /// The maximal number of times the creation of a mutex is retried after an
    /// <see cref="UnauthorizedAccessException"/>, which indicates that another process created the object in the
    /// meantime with a more restrictive security descriptor.
    /// </summary>
    private const int _maxCreationAttempts = 4;

    /// <summary>
    /// The names of the locks held by the current thread, used to detect a reentrant acquisition. The field is
    /// static, and therefore shared by every instance of this class in the process, because reentrancy is a
    /// property of the thread and of the name, and an attempt to acquire the same name through two different
    /// instances deadlocks just as surely as through one.
    /// </summary>
    [ThreadStatic]
    private static HashSet<string>? _namesHeldByCurrentThread;

    /// <summary>
    /// Serializes the creation of the operating system objects. It has no correctness purpose, because the
    /// operating system already serializes the creation. It exists to make the method easier to debug, which is
    /// the reason given by the implementation this one replaces.
    /// </summary>
    private readonly object _creationSync = new();

    /// <summary>
    /// The monitors backing the locks for which the operating system could not provide a named object, keyed by
    /// name so that two locks of the same name still exclude each other within the process.
    /// </summary>
    /// <remarks>
    /// The field is static because the degraded mode has nothing but this table to rely on, and because several
    /// instances of this service can coexist in one process. A table held per instance would silently provide no
    /// exclusion at all between them.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _processLocalMonitors = new( StringComparer.Ordinal );

    /// <summary>
    /// Whether to pass a security descriptor when creating a mutex. It is latched to <see langword="false"/> the
    /// first time the platform rejects one, so that the cost is paid only once.
    /// </summary>
    private volatile bool _useAccessControl = true;

    /// <summary>
    /// Whether the operating system has proved unable to provide named objects at all, in which case every
    /// subsequent lock goes straight to a process-local monitor.
    /// </summary>
    /// <remarks>
    /// The flag is latched only for the failures that are a property of the machine rather than of one name,
    /// because throwing and catching an exception for every lock would be expensive on a machine where the
    /// condition of issue 272 holds, and that condition affects every name at once.
    /// </remarks>
    private volatile bool _areNamedObjectsUnavailable;

#if HAS_METALAMA_TESTING_HOOKS
    /// <summary>
    /// The provider of the test synchronization points, which is never registered in production and is therefore
    /// normally <see langword="null"/>.
    /// </summary>
    private readonly ITestSynchronizationProvider? _testSynchronizationProvider;

    /// <summary>
    /// The injector of the test faults, which is never registered in production and is therefore normally
    /// <see langword="null"/>.
    /// </summary>
    private readonly ITestFaultInjector? _testFaultInjector;

    /// <summary>
    /// Initializes a new instance of the <see cref="NamedLockService"/> class.
    /// </summary>
    /// <param name="serviceProvider">
    /// An optional service provider, from which the test synchronization points are resolved. It is
    /// <see langword="null"/> in production, where nothing is registered for them anyway.
    /// </param>
    /// <remarks>
    /// This constructor exists only in the copies of this class that can reference <c>Metalama.Testing.Hooks</c>.
    /// The copies compiled into the projects that run before <c>Metalama.Backstage</c> has been extracted cannot
    /// reference it, and use the implicit parameterless constructor instead.
    /// </remarks>
    public NamedLockService( IServiceProvider? serviceProvider = null )
    {
        // Resolved untyped, because ITestSynchronizationProvider is shared with the layers above and therefore
        // cannot derive from IBackstageService.
        this._testSynchronizationProvider = (ITestSynchronizationProvider?) serviceProvider?.GetService( typeof(ITestSynchronizationProvider) );
        this._testFaultInjector = (ITestFaultInjector?) serviceProvider?.GetService( typeof(ITestFaultInjector) );
    }
#endif

    /// <summary>
    /// The location of the synchronization point reached after an acquisition has found the lock owned and is
    /// about to wait for it.
    /// </summary>
    internal const string BeforeWaitLocation = "TryAcquireBeforeWait";

    /// <summary>
    /// The location of the synchronization point reached after an acquisition has succeeded, while the lock is
    /// owned but before the caller has been given anything with which to release it.
    /// </summary>
    internal const string AfterWaitLocation = "TryAcquireAfterWait";

    /// <summary>
    /// The location of the synchronization point reached when a lock is about to be released, while it is still
    /// owned.
    /// </summary>
    internal const string BeforeReleaseLocation = "ReleaseBeforeRelease";

    /// <summary>
    /// The location of the fault injection point reached immediately before the operating system is asked to
    /// create the object with a security descriptor.
    /// </summary>
    /// <remarks>
    /// It exists because the two failures the surrounding code handles cannot be produced by any operating system
    /// this product runs on. An <see cref="UnauthorizedAccessException"/> requires a peer process that has already
    /// created the object with a security descriptor denying this one the right to create it again, which no
    /// single-process test can arrange. A <see cref="PlatformNotSupportedException"/> is not merely difficult but
    /// impossible, because <c>MutexAcl.Create</c> returns before it could be raised on the platforms that have no
    /// security descriptors. Injecting before the call is therefore the only way to reach either branch.
    /// </remarks>
    internal const string BeforeCreateWithAccessControlLocation = "TryOpenOrCreateMutexBeforeCreateWithAccessControl";

    /// <summary>
    /// The location of the fault injection point reached immediately before the operating system is asked to
    /// create the object without a security descriptor.
    /// </summary>
    internal const string BeforeCreateLocation = "TryOpenOrCreateMutexBeforeCreate";

    /// <summary>
    /// Composes the name of a synchronization point, following the <c>{ClassName}.{Location}:{Context}</c>
    /// convention. The context is the name of the lock, so that a test can pin one lock without pinning every
    /// other lock of the process.
    /// </summary>
    /// <param name="location">One of the <c>Location</c> constants of this class.</param>
    /// <param name="name">The name of the lock.</param>
    /// <returns>The name of the synchronization point.</returns>
    internal static string GetSyncPointName( string location, string name )
        => string.Format( CultureInfo.InvariantCulture, "NamedLockService.{0}:{1}", location, name );

    /// <summary>
    /// Reaches a synchronization point, which does nothing unless a test has armed it.
    /// </summary>
    /// <param name="location">One of the <c>Location</c> constants of this class.</param>
    /// <param name="name">The name of the lock.</param>
    /// <param name="cancellationToken">A token that releases a thread pinned at the point.</param>
    /// <remarks>
    /// The synchronization points exist only in the copies of this class that can reference
    /// <c>Metalama.Testing.Hooks</c>. The call sites are removed from the other copies by
    /// <see cref="ConditionalAttribute"/>, so they cost nothing there, not even the composition of the name.
    /// </remarks>
    // CA1822: the method accesses instance data only in the copies that have the hooks. In the other copies the
    // body is empty, and it must nonetheless remain an instance method, because the call sites are in a shared
    // file and cannot differ between the copies.
#pragma warning disable CA1822
    [Conditional( "HAS_METALAMA_TESTING_HOOKS" )]
    private void SyncPoint( string location, string name, CancellationToken cancellationToken )
    {
#if HAS_METALAMA_TESTING_HOOKS
        this._testSynchronizationProvider?.SyncPoint( GetSyncPointName( location, name ), cancellationToken );
#else
        _ = location;
        _ = name;
        _ = cancellationToken;
#endif
    }

    /// <summary>
    /// Reaches a fault injection point, which does nothing unless a test has armed it.
    /// </summary>
    /// <param name="location">One of the <c>Location</c> constants of this class.</param>
    /// <param name="name">The name of the lock.</param>
    /// <remarks>
    /// Like the synchronization points, the injection points exist only in the copies of this class that can
    /// reference <c>Metalama.Testing.Hooks</c>, and the call sites are removed from the other copies by
    /// <see cref="ConditionalAttribute"/>.
    /// </remarks>
    // CA1822: see the remark on SyncPoint above.
    [Conditional( "HAS_METALAMA_TESTING_HOOKS" )]
    private void InjectFault( string location, string name )
    {
#if HAS_METALAMA_TESTING_HOOKS
        this._testFaultInjector?.InjectFault( GetSyncPointName( location, name ) );
#else
        _ = location;
        _ = name;
#endif
    }
#pragma warning restore CA1822

    /// <summary>
    /// Occurs when something happens to a lock created by this service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handlers are invoked synchronously on the thread that caused the event, and sometimes while a lock is
    /// held, therefore they must be fast. An exception thrown by a handler is swallowed, because a defect in a
    /// diagnostic facility must not break the locking of a compilation, and because there is no second facility
    /// through which such an exception could be reported.
    /// </para>
    /// <para>
    /// This member is declared on the implementation rather than on <see cref="INamedLockService"/>, because a
    /// substitute implementation used by a test reports what it observed in its own way.
    /// </para>
    /// </remarks>
    public event EventHandler<LockEventArgs>? LockEventReported;

    /// <summary>
    /// Gets or sets a predicate deciding which kinds of event are reported, or <see langword="null"/> to report
    /// every kind.
    /// </summary>
    /// <remarks>
    /// A subscriber that is interested only in the unusual events, which is what a logger normally wants, sets
    /// this to <see cref="LockEventArgs.IsWarningKind"/>. Without it, subscribing at all would cost one object
    /// per acquisition and per release, on the critical path of every compilation, to produce events that the
    /// subscriber would discard.
    /// </remarks>
    public Predicate<LockEventKind>? ReportFilter { get; set; }

    /// <inheritdoc />
    public INamedLock GetLock( string name, CancellationToken cancellationToken = default )
    {
        if ( name == null )
        {
            throw new ArgumentNullException( nameof(name) );
        }

        var mutex = this.TryOpenOrCreateMutex( name, cancellationToken );

        if ( mutex != null )
        {
            return new OperatingSystemLock( this, name, mutex );
        }

        // The operating system refused to provide a named object. Fall back to a lock that excludes only the
        // threads of the current process: see the remarks of INamedLockService.GetLock.
        return new ProcessLocalLock( this, name, _processLocalMonitors.GetOrAdd( name, _ => new SemaphoreSlim( 1, 1 ) ) );
    }

    /// <summary>
    /// Makes this service behave as if the operating system were unable to provide named objects, so that every
    /// subsequent lock is process-local.
    /// </summary>
    /// <remarks>
    /// This exists so that a test can exercise the degraded mode on a machine where the operating system works.
    /// The degraded mode is otherwise reachable only under the conditions of issue 272, which are a property of
    /// the machine and cannot be arranged from inside the process.
    /// </remarks>
    internal void ForceProcessLocalLocks() => this._areNamedObjectsUnavailable = true;

    /// <summary>
    /// Opens or creates the named mutex, or returns <see langword="null"/> when the operating system cannot
    /// provide one.
    /// </summary>
    /// <param name="name">The name of the operating system object.</param>
    /// <param name="cancellationToken">A token that aborts the retries.</param>
    /// <returns>The mutex, or <see langword="null"/>.</returns>
    private Mutex? TryOpenOrCreateMutex( string name, CancellationToken cancellationToken )
    {
        if ( this._areNamedObjectsUnavailable )
        {
            // A previous attempt established that this machine cannot provide named objects at all. Checking
            // before taking the monitor keeps the degraded mode cheap, which matters because every lock of every
            // subsequent operation goes through here.
            return null;
        }

        lock ( this._creationSync )
        {
            for ( var attempt = 0; /* Intentionally empty. */; attempt++ )
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    // Opening an existing object first avoids an UnauthorizedAccessException in the common case
                    // where another process created the object with a security descriptor that does not grant
                    // this process the right to create it again.
                    if ( Mutex.TryOpenExisting( name, out var existingMutex ) )
                    {
                        this.Report( LockEventKind.Created, name, detail: "opened an existing object" );

                        return existingMutex;
                    }

                    if ( this._useAccessControl )
                    {
                        try
                        {
                            // Creating a mutex without a security descriptor uses the default security, which
                            // differs between systems and can make the object unusable by another user.
                            // MutexAcl.Create falls back to the default security on the platforms where a
                            // security descriptor is not a meaningful concept.
                            this.InjectFault( BeforeCreateWithAccessControlLocation, name );

                            var securedMutex = MutexAcl.Create( false, name, MutexAcl.AllowUsingMutexToEveryone );

                            this.Report( LockEventKind.Created, name );

                            return securedMutex;
                        }
                        catch ( PlatformNotSupportedException e )
                        {
                            this._useAccessControl = false;
                            if ( this.IsReportEnabled )
                            {
                                this.Report( LockEventKind.Created, name, detail: $"security descriptors are unavailable: {e.Message}" );
                            }
                        }
                    }

                    this.InjectFault( BeforeCreateLocation, name );

                    var mutex = new Mutex( false, name );

                    this.Report( LockEventKind.Created, name, detail: "created without a security descriptor" );

                    return mutex;
                }
                catch ( UnauthorizedAccessException e )
                {
                    if ( attempt + 1 >= _maxCreationAttempts )
                    {
                        // This name is unusable by this process, but the other names may well be usable, so the
                        // failure is not generalized to the whole machine.
                        return this.ReportDegraded( name, e, isMachineWide: false );
                    }

                    // The object was probably created in the meantime by a process with a different set of
                    // rights. Yield and start over, so that the next iteration opens it instead of creating it.
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep( 0 );
                }
                catch ( Exception e ) when ( IsNameSpecificRefusal( e ) )
                {
                    return this.ReportDegraded( name, e, isMachineWide: false );
                }
                catch ( Exception e ) when ( IsMachineWideRefusal( e ) )
                {
                    return this.ReportDegraded( name, e, isMachineWide: true );
                }
                catch ( Exception e ) when ( !IsCallerDefect( e ) )
                {
                    // Nothing that this method does not recognize may escape it. Two of the three callers of
                    // GetLock, the resource extractor and the design-time entry point manager, run during the
                    // bootstrap and have nowhere to catch an exception, so one escaping here fails the compilation
                    // outright, which is the class of failure this service exists to eliminate. Degrading is
                    // always available and always better.
                    return this.ReportDegraded( name, e, isMachineWide: false );
                }
            }
        }
    }

    /// <summary>
    /// Determines whether an exception means that the operating system cannot provide named synchronization
    /// objects at all, so that every subsequent lock can go straight to a process-local monitor.
    /// </summary>
    /// <param name="exception">The exception thrown while opening or creating the object.</param>
    /// <returns><see langword="true"/> if no name is usable on this machine.</returns>
    /// <remarks>
    /// This is the case of issue 272. On Unix the runtime implements the named objects with files under
    /// <c>/tmp/.dotnet/shm</c>, and when that tree cannot be used it raises <see cref="IOException"/>. Until this
    /// method existed, that exception escaped and failed the whole compilation with LAMA0623. The condition is a
    /// property of the machine, not of the name, so it is worth latching.
    /// </remarks>
    private static bool IsMachineWideRefusal( Exception exception ) => exception is IOException or PlatformNotSupportedException or NotSupportedException;

    /// <summary>
    /// Determines whether an exception means that one particular name is unusable while the others may still be
    /// usable.
    /// </summary>
    /// <param name="exception">The exception thrown while opening or creating the object.</param>
    /// <returns><see langword="true"/> if only this name must degrade to a process-local lock.</returns>
    /// <remarks>
    /// <see cref="WaitHandleCannotBeOpenedException"/> means that an object of a different kind already has this
    /// name. An <see cref="ArgumentException"/>, by contrast, means that the name is invalid or too long, which is
    /// a defect in the caller and must remain visible, so it is deliberately absent from this list.
    /// </remarks>
    private static bool IsNameSpecificRefusal( Exception exception ) => exception is WaitHandleCannotBeOpenedException;

    /// <summary>
    /// Determines whether an exception reports a defect of the caller rather than a refusal of the operating
    /// system, in which case it must remain visible instead of being degraded away.
    /// </summary>
    /// <param name="exception">The exception thrown while opening or creating the object.</param>
    /// <returns><see langword="true"/> if the exception must propagate.</returns>
    /// <remarks>
    /// An <see cref="ArgumentException"/> means that the name is invalid or too long, which no degradation can
    /// repair and which the caller must be told about. An <see cref="OperationCanceledException"/> is the answer to
    /// a cancellation the caller itself requested.
    /// </remarks>
    private static bool IsCallerDefect( Exception exception ) => exception is ArgumentException or OperationCanceledException;

    /// <summary>
    /// Reports that the lock of a given name has to degrade to a process-local one, and returns
    /// <see langword="null"/> so that the caller can return the result of this method directly.
    /// </summary>
    /// <param name="name">The name of the lock.</param>
    /// <param name="exception">The exception that caused the degradation.</param>
    /// <param name="isMachineWide">
    /// Whether no name is usable on this machine, in which case the subsequent locks skip the operating system
    /// instead of throwing and catching an exception each.
    /// </param>
    /// <returns>Always <see langword="null"/>.</returns>
    private Mutex? ReportDegraded( string name, Exception exception, bool isMachineWide )
    {
        if ( isMachineWide )
        {
            this._areNamedObjectsUnavailable = true;
        }

        if ( this.IsReportEnabled )
        {
            this.Report( LockEventKind.Degraded, name, detail: $"{exception.GetType().Name}: {exception.Message}" );
        }

        return null;
    }

    /// <summary>
    /// Gets a value indicating whether anything is listening to <see cref="LockEventReported"/>, so that a caller
    /// can skip composing a detail that would be discarded.
    /// </summary>
    private bool IsReportEnabled => this.LockEventReported != null;

    /// <summary>
    /// Determines whether an event of a given kind would be reported, so that no object is created for one that
    /// would be discarded.
    /// </summary>
    /// <param name="kind">The kind of event.</param>
    /// <returns><see langword="true"/> if the event must be created and raised.</returns>
    private bool IsReported( LockEventKind kind )
    {
        if ( this.LockEventReported == null )
        {
            return false;
        }

        var filter = this.ReportFilter;

        return filter == null || filter( kind );
    }

    /// <summary>
    /// Raises <see cref="LockEventReported"/>.
    /// </summary>
    /// <param name="kind">The kind of event.</param>
    /// <param name="name">The name of the lock.</param>
    /// <param name="duration">The duration relevant to <paramref name="kind"/>, or <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="detail">Additional information, or <see langword="null"/>.</param>
    /// <remarks>
    /// The arguments are passed separately rather than as a <see cref="LockEventArgs"/> so that nothing is
    /// allocated when no handler is subscribed, which is the case in production. A lock is acquired and released
    /// on the critical path of every compilation, so an allocation for each of those would be paid by every user
    /// to serve a diagnostic facility that is normally switched off.
    /// </remarks>
    private void Report( LockEventKind kind, string name, TimeSpan duration = default, string? detail = null )
    {
        // The delegate is copied to a local so that a handler unsubscribing on another thread between the null
        // check and the invocation cannot cause a NullReferenceException.
        var handler = this.LockEventReported;

        if ( handler == null || !this.IsReported( kind ) )
        {
            return;
        }

        try
        {
            handler.Invoke( this, new LockEventArgs( kind, name, duration, detail ) );
        }
        catch
        {
            // See the remarks of LockEventReported. There is deliberately nowhere to report this.
        }
    }

    /// <summary>
    /// Verifies that the current thread does not already hold the lock of a given name, because the locks are not
    /// reentrant.
    /// </summary>
    /// <param name="name">The name about to be acquired.</param>
    /// <remarks>
    /// The check must happen before the wait, not after, because the operating system grants a recursive
    /// acquisition of a mutex to the thread that already owns it, so there is nothing left to observe once the
    /// wait has returned. A debug build throws, so that a defect surfaces during development; a release build only
    /// reports, so that a defect that reached production behaves as it did before this check existed.
    /// </remarks>
    private void CheckNotReentrant( string name )
    {
        if ( _namesHeldByCurrentThread?.Contains( name ) != true )
        {
            return;
        }

        this.Report( LockEventKind.ReentrancyDetected, name );

#if DEBUG
        throw new InvalidOperationException(
            $"The named lock '{name}' is already held by the current thread, and named locks are not reentrant." );
#endif
    }

    /// <summary>
    /// Records that the current thread holds the lock of a given name.
    /// </summary>
    /// <param name="name">The name that was acquired.</param>
    private static void MarkHeldByCurrentThread( string name ) => (_namesHeldByCurrentThread ??= new HashSet<string>( StringComparer.Ordinal )).Add( name );

    /// <summary>
    /// Records that the current thread no longer holds the lock of a given name.
    /// </summary>
    /// <param name="name">The name that was released.</param>
    private static void MarkReleasedByCurrentThread( string name ) => _namesHeldByCurrentThread?.Remove( name );

    /// <summary>
    /// Converts an interval measured with <see cref="Stopwatch.GetTimestamp"/> into a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="startTimestamp">The value of <see cref="Stopwatch.GetTimestamp"/> when the interval started.</param>
    /// <returns>The elapsed time.</returns>
    /// <remarks>
    /// A raw timestamp is used instead of a <see cref="Stopwatch"/> instance because a lock is acquired very
    /// frequently on the critical path of a compilation and must not allocate.
    /// </remarks>
    private static TimeSpan GetElapsed( long startTimestamp )
        => TimeSpan.FromSeconds( (double) (Stopwatch.GetTimestamp() - startTimestamp) / Stopwatch.Frequency );
}
