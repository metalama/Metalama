// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Threading;
using Metalama.Framework.Engine.Utilities.AssemblyLoaders;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

// ReSharper disable NullableWarningSuppressionIsUsed
// Resharper disable EmptyGeneralCatchClause

namespace Metalama.Framework.CompilerExtensions;

/// <summary>
/// Extract dependency assemblies packed as managed resources and provides instances of classes implemented by these dependencies.
/// </summary>
public static class ResourceExtractor
{
    private const string _designTimeContractsAssemblyName = "Metalama.Framework.DesignTime.Contracts.v2";

    private static readonly object _initializeLock = new();

    private static readonly bool _isNetFramework =
        RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.OrdinalIgnoreCase );

    private static readonly Dictionary<string, (string Path, AssemblyName Name)> _embeddedAssemblies = new( StringComparer.OrdinalIgnoreCase );

    private static readonly ConcurrentDictionary<string, Assembly?> _assemblyCache = new( StringComparer.OrdinalIgnoreCase );

    private static readonly string _snapshotDirectory;
    private static readonly string _buildId;
    private static volatile bool _initialized;
    private static AssemblyLoader? _assemblyLoader;
    private static readonly string? _overriddenTempPath;
    private static readonly string? _variantName;
    private static int _unsupportedHostReported;

    /// <summary>
    /// Gets the Roslyn version of the host, which is the version of the assembly that contains
    /// <see cref="SyntaxNode"/>.
    /// </summary>
    public static Version HostRoslynVersion { get; }

    static ResourceExtractor()
    {
        if ( !string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "METALAMA_DEBUG_RESOURCE_EXTRACTOR" ) ) )
        {
            Debugger.Launch();
        }

        // This mimics the logic implemented by TempPathHelper and backed by Metalama.Backstage, however without having a reference to Metalama.Backstage.
        var assembly = typeof(ResourceExtractor).Assembly;
        var moduleId = assembly.ManifestModule.ModuleVersionId;
        var assemblyVersion = assembly.GetName().Version;

        _buildId = assemblyVersion.ToString( 4 ) + "-" +
                   string.Join( "", moduleId.ToByteArray().Take( 4 ).Select( i => i.ToString( "x2", CultureInfo.InvariantCulture ) ) );

        // Read the METALAMA_TEMP override before computing any path that depends on it.
        var overriddenTempPath = Environment.GetEnvironmentVariable( "METALAMA_TEMP" );
        _overriddenTempPath = string.IsNullOrEmpty( overriddenTempPath ) ? null : overriddenTempPath;

        _snapshotDirectory = GetTempDirectory( "Extract" );

        HostRoslynVersion = GetHostRoslynVersion();

        _variantName = RoslynVariantPolicy.TryGetVariantName( HostRoslynVersion, out var variantName ) ? variantName : null;
    }

    private static string GetTempDirectory( string purpose )
        => Path.Combine( GetTempBaseDirectory(), purpose, _buildId, _isNetFramework ? "desktop" : "core" );

    // Mirrors Metalama.Backstage.Utilities.MetalamaPathUtilities.GetTempDirectory (we cannot reference Metalama.Backstage here).
    // The directory holds assemblies that Metalama loads and executes, so on Unix it must not live under the world-writable
    // /tmp (issue #1650); we use the per-user application-data directory instead. On Windows the temp directory is already
    // specific to the current user.
    private static string GetTempBaseDirectory()
    {
        if ( _overriddenTempPath != null )
        {
            return Path.Combine( _overriddenTempPath, "Metalama" );
        }

        if ( RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return Path.Combine( Path.GetTempPath(), "Metalama" );
        }

        var localApplicationData = Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData );

        var applicationDataDirectory = !string.IsNullOrEmpty( localApplicationData )
            ? Path.Combine( localApplicationData, "Metalama" )
            : Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.UserProfile ), ".metalama" );

        return Path.Combine( applicationDataDirectory, "Temp" );
    }

    private static void Initialize()
    {
        if ( !_initialized )
        {
            lock ( _initializeLock )
            {
                if ( !_initialized )
                {
                    var currentAssembly = typeof(ResourceExtractor).Assembly;

                    // Get a temp directory. AssemblyName.GetAssemblyName does not support long paths.

                    // Extract embedded assemblies to a temp directory.
                    ExtractEmbeddedAssemblies( currentAssembly );

                    // Load assemblies from the temp directory.
                    foreach ( var file in Directory.GetFiles( _snapshotDirectory, "*.dll" ) )
                    {
                        // We don't load assemblies using Assembly.LoadFile here, because the assemblies may be loaded in
                        // the main load context, or may be loaded later. We will use Assembly.LoadFile in last chance in the AssemblyResolve event.
                        // This scenario is used in Metalama.Try.

                        var loadedAssemblyName = AssemblyName.GetAssemblyName( file );
                        _embeddedAssemblies[loadedAssemblyName.Name] = (file, loadedAssemblyName);
                    }

                    // Since GetAssemblyCore loads the DesignTime.Contracts assembly outside of the AssemblyLoader ALC,
                    // we also need to handle loading its dependencies by specifying the globalResolveHandlerFilter.
                    _assemblyLoader = AssemblyLoaderFactory.CreateAssemblyLoader(
                        name => GetAssembly( name ),
                        a => a?.GetName().Name == _designTimeContractsAssemblyName );

                    _initialized = true;
                }
            }
        }
    }

    /// <summary>
    /// Creates an instance of a type from the Roslyn-version-specific Metalama assembly that serves the host, unless
    /// no embedded payload variant serves it.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly, without the suffix that names the variant.</param>
    /// <param name="typeName">The full name of the type to instantiate.</param>
    /// <param name="instance">When this method returns <c>true</c>, the new instance.</param>
    /// <returns><c>false</c> when the Roslyn version of the host is below the lowest supported one, in which case the
    /// caller must hold no implementation and behave as if Metalama were not installed.</returns>
    public static bool TryCreateInstance<T>( string assemblyName, string typeName, out T? instance )
        where T : class
    {
        if ( _variantName == null )
        {
            ReportUnsupportedHost();

            instance = null;

            return false;
        }

        instance = (T) CreateInstance( assemblyName, typeName );

        return true;
    }

    /// <summary>
    /// Writes a report naming the Roslyn version of the host and the lowest supported one. It is written once per
    /// process, in the directory that holds the crash reports. A design-time host has no other channel: the integrated
    /// development environment shows no diagnostic for a payload that did not load, so a host that does nothing would
    /// otherwise also say nothing.
    /// </summary>
    private static void ReportUnsupportedHost()
    {
        if ( Interlocked.CompareExchange( ref _unsupportedHostReported, 1, 0 ) != 0 )
        {
            return;
        }

        try
        {
            var directory = CreateCrashReportsDirectory();

            var report = new StringBuilder();
            var process = Process.GetCurrentProcess();

            report.AppendLine(
                $"Metalama requires Roslyn {RoslynVariantPolicy.MinimumSupportedRoslynVersion} or later. This process runs Roslyn " +
                $"{HostRoslynVersion}, for which this build of Metalama embeds no implementation, so Metalama is doing nothing here." );

            report.AppendLine( $"Metalama Version: {typeof(ResourceExtractor).Assembly.GetName().Version}" );
            report.AppendLine( $"Runtime: {RuntimeInformation.FrameworkDescription}" );
            report.AppendLine( $"Process Name: {process.ProcessName}" );
            report.AppendLine( $"Process Id: {process.Id}" );
            report.AppendLine( $"Process Kind: {ProcessKindHelper.CurrentProcessKind}" );
            report.AppendLine( $"Command Line: {Environment.CommandLine}" );

            File.WriteAllText( Path.Combine( directory, $"unsupported-roslyn-{HostRoslynVersion}.txt" ), report.ToString() );
        }
        catch
        {
            // A failure to write the report must not fail the host, and there is no channel left to report it on.
        }
    }

    /// <summary>
    /// Returns the path of the directory that holds the crash reports, and creates it if it does not exist.
    /// </summary>
    private static string CreateCrashReportsDirectory()
    {
        var directory = GetTempDirectory( "CrashReports" );

        if ( !Directory.Exists( directory ) )
        {
            Directory.CreateDirectory( directory );

            try
            {
                // Mark the directory for automatic clean up when unused.
                var cleanupJsonFilePath = Path.Combine( directory, "cleanup.json" );
                File.WriteAllText( cleanupJsonFilePath, "{\"Strategy\":1}" );
            }
            catch ( IOException ) { }
        }

        return directory;
    }

    private static object CreateInstance( string assemblyName, string typeName )
    {
        var log = new StringBuilder();

        try
        {
            Initialize();

            assemblyName = assemblyName + "." + _variantName;

            var assemblyQualifiedName = _embeddedAssemblies[assemblyName].Name.ToString();
            log.AppendLine( $"Creating an instance of '{typeName}' from '{assemblyQualifiedName}'." );

            var assembly =
                GetAssembly( assemblyQualifiedName, log )
                ?? throw new ArgumentOutOfRangeException( nameof(assemblyName), $"Cannot load the assembly '{assemblyQualifiedName}'" );

            var type =
                assembly.GetType( typeName, true )
                ?? throw new ArgumentOutOfRangeException( nameof(typeName), $"Cannot load the type '{typeName}' in assembly '{assemblyQualifiedName}'" );

            return Activator.CreateInstance( type );
        }
        catch ( Exception e )
        {
            var directory = CreateCrashReportsDirectory();

            var path = Path.Combine( directory, Guid.NewGuid().ToString() + ".txt" );

            var exceptionText = new StringBuilder();
            var process = Process.GetCurrentProcess();

            exceptionText.AppendLine( $"Metalama Version: {typeof(ResourceExtractor).Assembly.GetName().Version}" );
            exceptionText.AppendLine( $"Runtime: {RuntimeInformation.FrameworkDescription}" );
            exceptionText.AppendLine( $"Processor Architecture: {RuntimeInformation.ProcessArchitecture}" );
            exceptionText.AppendLine( $"OS Description: {RuntimeInformation.OSDescription}" );
            exceptionText.AppendLine( $"OS Architecture: {RuntimeInformation.OSArchitecture}" );
            exceptionText.AppendLine( $"Process Name: {process.ProcessName}" );
            exceptionText.AppendLine( $"Process Id: {process.Id}" );
            exceptionText.AppendLine( $"Process Kind: {ProcessKindHelper.CurrentProcessKind}" );
            exceptionText.AppendLine( $"Command Line: {Environment.CommandLine}" );
            exceptionText.AppendLine( $"Exception type: {e.GetType()}" );
            exceptionText.AppendLine( $"Exception message: {e.Message}" );

            try
            {
                // The next line may fail.
                var exceptionToString = e.ToString();
                exceptionText.AppendLine( "===== Exception ===== " );
                exceptionText.AppendLine( exceptionToString );
            }
            catch { }

            exceptionText.AppendLine( "===== Loaded assemblies ===== " );

            foreach ( var assembly in AppDomain.CurrentDomain.GetAssemblies() )
            {
                if ( !assembly.IsDynamic )
                {
                    try
                    {
                        exceptionText.AppendLine( assembly.Location );
                    }
                    catch { }
                }
            }

            exceptionText.AppendLine( "===== Log ===== " );
            exceptionText.AppendLine( log.ToString() );
            File.WriteAllText( path, exceptionText.ToString() );

            throw;
        }
    }

    private static string GetHash( string input )
    {
        // Can't use HashUtilities here, because of dependency on K4os.Hash.xxHash.
        using var sha256 = SHA256.Create();
        var inputBytes = Encoding.UTF8.GetBytes( input );
        var hashBytes = sha256.ComputeHash( inputBytes );

        // Replace '/' with '_' to avoid exception on Unix.
        return Convert.ToBase64String( hashBytes ).Replace( '/', '_' );
    }

    private static void ExtractEmbeddedAssemblies( Assembly currentAssembly )
    {
        // Extract managed resources to a snapshot directory.
        var completedFilePath = Path.Combine( _snapshotDirectory, ".completed" );
        var cleanupJsonFilePath = Path.Combine( _snapshotDirectory, "cleanup.json" );
        var mutexName = $"Global\\Metalama_Extract_{GetHash( _snapshotDirectory )}";

        var deleteCompletedFile = false;

        if ( File.Exists( completedFilePath ) )
        {
            var allExpectedFilesExist = GetEmbeddedAssemblies( currentAssembly ).All( x => File.Exists( x.FilePath ) );

            if ( allExpectedFilesExist )
            {
                if ( File.GetLastWriteTime( cleanupJsonFilePath ) < DateTime.Now.AddHours( -1 ) )
                {
                    // Touch the cleanup.json file so the periodic cleanup script does not remove it.

                    try
                    {
                        File.SetLastAccessTime( cleanupJsonFilePath, DateTime.Now );
                    }
                    catch { }
                }

                return;
            }
            else
            {
                // The .completed file exists, but not all expected files are present.
                // This is an inconsistent state, so we will delete the .completed file and re-extract the resources.

                deleteCompletedFile = true;
            }
        }

        // NamedLockService is shared with Metalama.Backstage by compiling the same source files, because this
        // assembly embeds Metalama.Backstage and extracts it here, and can therefore reference nothing.
        // A process that crashed while holding the lock is not a problem: the presence of the `.completed` file
        // alone says that the extraction was successful.
        // When the operating system cannot provide a named object at all, which is issue #272, the lock excludes
        // only the threads of this process. That is enough, because two processes extracting at once still
        // converge: each file is either written or, if another process holds it open, read back and compared.
        // A concurrent queue, because the events are reported on whichever thread caused them, which is not
        // necessarily the thread running this method.
        var lockEvents = new ConcurrentQueue<string>();
        var lockService = new NamedLockService();

        lockService.LockEventReported += ( _, lockEvent ) => lockEvents.Enqueue( lockEvent.ToString() );

        using var extractLock = lockService.GetLock( mutexName );
        using var extractLockHandle = extractLock.Acquire();

        StreamWriter? log = null;

        try
        {
            if ( deleteCompletedFile )
            {
                File.Delete( completedFilePath );
            }

            if ( !File.Exists( completedFilePath ) )
            {
                if ( !Directory.Exists( _snapshotDirectory ) )
                {
                    Directory.CreateDirectory( _snapshotDirectory );

                    // Mark the directory for automatic clean up when unused.
                    File.WriteAllText( cleanupJsonFilePath, """{"Strategy":2}""" );
                }

                log = File.CreateText( Path.Combine( _snapshotDirectory, $"extract-{Guid.NewGuid()}.log" ) );

                log.WriteLine( $"Extracting resources..." );

                var process = Process.GetCurrentProcess();
                log.WriteLine( $"Process Name: {process.ProcessName}" );
                log.WriteLine( $"Process Id: {process.Id}" );
                log.WriteLine( $"Process Kind: {ProcessKindHelper.CurrentProcessKind}" );
                log.WriteLine( $"Command Line: {Environment.CommandLine}" );
                log.WriteLine( $"Source Assembly Name: '{currentAssembly.FullName}'" );
                log.WriteLine( $"Source Assembly Location: '{currentAssembly.Location}'" );
                log.WriteLine( $"Mutex name: '{mutexName}'" );

                // The lock is acquired before this log file exists, so its events are buffered until here. They
                // record whether the operating system could provide a named object, which is the first thing to
                // look at when a machine shows the symptoms of issue #272.
                foreach ( var lockEvent in lockEvents )
                {
                    log.WriteLine( $"Lock: {lockEvent}" );
                }

                log.WriteLine( "----" );

                foreach ( var (resourceName, filePath) in GetEmbeddedAssemblies( currentAssembly, log ) )
                {
                    log.WriteLine( $"Extracting resource '{resourceName}' to '{filePath}'." );

                    // Extract the file to disk.
                    using var stream = currentAssembly.GetManifestResourceStream( resourceName )!;

                    // ReSharper disable once InconsistentNaming
                    const uint ERROR_SHARING_VIOLATION = 0x80070020;

                    try
                    {
                        using var outputStream = File.Create( filePath );

                        stream.CopyTo( outputStream );
                    }
                    catch ( IOException ex ) when ( (uint) ex.HResult == ERROR_SHARING_VIOLATION )
                    {
                        // We couldn't write to the file, so try to read it instead and verify its content is correct.

                        using var readStream = File.OpenRead( filePath );

                        if ( !StreamsContentsAreEqual( stream, readStream ) )
                        {
                            throw new InvalidOperationException(
                                $"Could not open file '{filePath}' for writing and its existing content is not correct",
                                ex );
                        }
                    }
                }

                File.WriteAllText( completedFilePath, "completed" );

                log.WriteLine( "Extracting resources completed." );
            }
        }
        catch ( Exception e )
        {
            log?.WriteLine( e.ToString() );

            throw;
        }
        finally
        {
            log?.Dispose();
        }
    }

    private static IEnumerable<(string ResourceName, string FilePath)> GetEmbeddedAssemblies( Assembly currentAssembly, StreamWriter? log = null )
    {
        var prefix = $"Metalama.Framework.CompilerExtensions.Resources.{(_isNetFramework ? "Desktop" : "Core")}.";

        foreach ( var resourceName in currentAssembly.GetManifestResourceNames() )
        {
            if ( resourceName.EndsWith( ".dll", StringComparison.OrdinalIgnoreCase ) &&
                 resourceName.StartsWith( prefix, StringComparison.OrdinalIgnoreCase ) )
            {
                var fileName = resourceName.Substring( prefix.Length );
                var filePath = Path.Combine( _snapshotDirectory, fileName );

                yield return (resourceName, filePath);
            }
            else
            {
                log?.WriteLine( $"Ignoring resource '{resourceName}'." );
            }
        }
    }

    // https://stackoverflow.com/a/47422179/41071
    private static bool StreamsContentsAreEqual( Stream stream1, Stream stream2 )
    {
        const int bufferSize = 4096;

        var buffer1 = new byte[bufferSize];
        var buffer2 = new byte[bufferSize];

        while ( true )
        {
            var count1 = ReadFullBuffer( stream1, buffer1 );
            var count2 = ReadFullBuffer( stream2, buffer2 );

            if ( count1 != count2 )
            {
                return false;
            }

            if ( count1 == 0 )
            {
                return true;
            }

            if ( !buffer1.AsSpan().SequenceEqual( buffer2 ) )
            {
                return false;
            }
        }

        static int ReadFullBuffer( Stream stream, byte[] buffer )
        {
            var bytesRead = 0;

            while ( bytesRead < buffer.Length )
            {
                var read = stream.Read( buffer, bytesRead, buffer.Length - bytesRead );

                if ( read == 0 )
                {
                    // Reached end of stream.
                    return bytesRead;
                }

                bytesRead += read;
            }

            return bytesRead;
        }
    }

    private static Assembly? GetAssembly( string name, StringBuilder? log = null ) => _assemblyCache.GetOrAdd( name, _ => GetAssemblyCore( name, log ) );

    private static Assembly? GetAssemblyCore( string name, StringBuilder? log )
    {
        var requestedAssemblyName = new AssemblyName( name );

        var isEmbedded = _embeddedAssemblies.TryGetValue( requestedAssemblyName.Name, out var embeddedAssembly );

        // Find an assembly in the current AppDomain.
        // This is important for Metalama.Try. Without that, we may have several copies of the same assemblies loaded, one from the normal
        // loading context, and the other from the LoadFile loading context.
        log?.AppendLine(
            isEmbedded
                ? $"Looking for an exact version match for '{name}', which is embedded in the current build."
                : $"Looking for '{name}' or a higher version, which is not embedded in the current build." );

        var assembly = GetAlreadyLoadedAssembly( requestedAssemblyName, isEmbedded, log );

        if ( assembly != null )
        {
            log?.AppendLine( $"'{requestedAssemblyName.Name}' was already loaded (version '{assembly.GetName().Version}')." );

            return assembly;
        }

        if ( isEmbedded )
        {
            log?.AppendLine( $"Trying to provide the embedded version '{embeddedAssembly.Name.Version}'." );

            if ( embeddedAssembly.Name.Version == requestedAssemblyName.Version )
            {
                log?.AppendLine( $"Loading the embedded assembly '{embeddedAssembly.Path}'." );

                // It seems assemblies loaded into an ALC don't participate in COM type equivalence.
                // Since we need that for the DesignTime.Contracts assembly in devenv.exe and in Rider
                // (where every Roslyn extension is loaded into its own ALC and shares the singleton
                // DesignTimeEntryPointManager via AppDomain.SetData, see #1626), load it without using ALC.
                // However, in other processes (DevHub, ServiceHub, compiler), COM type equivalence is not needed
                // and Assembly.LoadFile causes Microsoft.CodeAnalysis to resolve from the wrong ALC in DevHub,
                // leading to MissingMethodException/TypeLoadException (#1461).

                // ReSharper disable once StringStartsWithIsCultureSpecific
                if ( name.StartsWith( $"{_designTimeContractsAssemblyName}," )
                     && ProcessKindHelper.CurrentProcessKind is ProcessKind.DevEnv or ProcessKind.Rider )
                {
                    return Assembly.LoadFile( embeddedAssembly.Path );
                }

                return _assemblyLoader.LoadFromPath( embeddedAssembly.Path );
            }
            else
            {
                // This is not the expected version.
                // Another assembly version should handle it.

                log?.AppendLine( $"The embedded assembly '{embeddedAssembly.Name}', did not match the required version. Returning null." );

                return null;
            }
        }
        else
        {
            log?.AppendLine( $"'{requestedAssemblyName.Name}' is not an embedded assembly and was not already loaded. Returning null." );

            return null;
        }
    }

    private static Assembly? GetAlreadyLoadedAssembly( AssemblyName requestedAssemblyName, bool isEmbedded, StringBuilder? log )
    {
        // We may get here because one of our assemblies is requesting a lower version of Roslyn
        // assemblies than what we have. In this case, we will return any matching assembly, unless the assembly
        // is embedded in the current build, in which case only the exact version is acceptable.

        var candidates = AppDomain.CurrentDomain.GetAssemblies()
            .Where( x => !_assemblyLoader.IsCollectible( x ) )
            .ToArray();

        var candidateNames = Array.ConvertAll( candidates, x => x.GetName() );

        var index = AssemblyResolutionPolicy.SelectAlreadyLoadedAssembly( requestedAssemblyName, candidateNames, isEmbedded );

        if ( index < 0 )
        {
            log?.AppendLine( "No matching assembly was found in the AppDomain." );

            return null;
        }

        var existingAssembly = candidates[index];

        log?.AppendLine( $"Found '{existingAssembly.Location}'." );

        return existingAssembly;
    }

    private static Version GetHostRoslynVersion()
    {
        var assembly = typeof(SyntaxNode).Assembly;
        var version = assembly.GetName().Version;

        if ( version == new Version( 42, 42, 42, 42 ) )
        {
            // This is the JetBrains build. The real version is in AssemblyInformationalVersionAttribute.

            var informationalVersionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if ( informationalVersionAttribute != null )
            {
                var informationalVersionString = informationalVersionAttribute.InformationalVersion.Split( '-' );

                if ( Version.TryParse( informationalVersionString[0], out var informationVersion ) )
                {
                    version = informationVersion;
                }
            }
        }

        return version;
    }
}