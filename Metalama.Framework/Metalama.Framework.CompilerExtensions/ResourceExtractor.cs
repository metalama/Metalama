// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Threading;
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
    private const string _designTimeContractsAssemblyName = "Metalama.Framework.DesignTime.Contracts";

    private static readonly object _initializeLock = new();
    private static readonly string[] _assembliesShippedWithMetalamaCompiler = new[] { "Metalama.Backstage", "Metalama.Compiler.Interfaces" };

    private static readonly bool _isNetFramework =
        RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.OrdinalIgnoreCase );

    private static readonly Dictionary<string, (string Path, AssemblyName Name)> _embeddedAssemblies = new( StringComparer.OrdinalIgnoreCase );

    private static readonly ConcurrentDictionary<string, Assembly?> _assemblyCache = new( StringComparer.OrdinalIgnoreCase );

    private static readonly string _snapshotDirectory;
    private static readonly string _buildId;
    private static volatile bool _initialized;
    private static string? _versionNumber;
    private static AssemblyLoader? _assemblyLoader;
    private static readonly string? _overriddenTempPath;

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

        _snapshotDirectory = GetTempDirectory( "Extract" );

        var overriddenTempPath = Environment.GetEnvironmentVariable( "METALAMA_TEMP" );
        _overriddenTempPath = string.IsNullOrEmpty( overriddenTempPath ) ? null : overriddenTempPath;
    }

    private static string GetTempDirectory( string purpose )
        => Path.Combine( _overriddenTempPath ?? Path.GetTempPath(), "Metalama", purpose, _buildId, _isNetFramework ? "desktop" : "core" );

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

                    _versionNumber = GetRoslynVersion();

                    // Since GetAssemblyCore loads the DesignTime.Contracts assembly outside of the AssemblyLoader ALC,
                    // we also need to handle loading its dependencies by specifying the globalResolveHandlerFilter.
                    _assemblyLoader = new AssemblyLoader(
                        name => GetAssembly( name ),
                        a => a?.GetName().Name == _designTimeContractsAssemblyName );

                    _initialized = true;
                }
            }
        }
    }

    /// <summary>
    /// Creates an instance of a type from a Roslyn-version-specific Metalama assembly.
    /// </summary>
    public static object CreateInstance( string assemblyName, string typeName )
    {
        var log = new StringBuilder();

        try
        {
            Initialize();

            assemblyName = assemblyName + "." + _versionNumber;

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

        // We cannot use MutexHelper because of dependencies on an embedded assembly.

        using var extractMutex = MutexAcl.Create( false, mutexName, MutexAcl.AllowUsingMutexToEveryone );

        try
        {
            extractMutex.WaitOne();
        }
        catch ( AbandonedMutexException )
        {
            // Another process crashed while holding the mutex.
            // This situation can be ignored because the presence of the `.completed` file alone says
            // that the extraction was successful.
        }

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
            extractMutex.ReleaseMutex();
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
        // Version operator <= throws on .Net Framework when the first operand is null, so we have to check for null explicitly.
        static bool VersionTolerantReferenceMatchesDefinition( AssemblyName requestedAssemblyName, AssemblyName candidate )
        {
            return AssemblyName.ReferenceMatchesDefinition( requestedAssemblyName, candidate )
                   && (requestedAssemblyName.Version == null || requestedAssemblyName.Version <= candidate.Version);
        }

        static bool StrictReferenceMatchesDefinition( AssemblyName requestedAssemblyName, AssemblyName candidate )
        {
            return AssemblyName.ReferenceMatchesDefinition( requestedAssemblyName, candidate ) && requestedAssemblyName.Version == candidate.Version;
        }

        var requestedAssemblyName = new AssemblyName( name );

        // Find for an assembly in the current AppDomain.
        // This is important for Metalama.Try. Without that, we may have several copies of the same assemblies loaded, one from the normal
        // loading context, and the other from the LoadFile loading context.
        log?.AppendLine( $"Looking for an exact version match for '{name}'." );
        var assembly = GetAlreadyLoadedAssembly( requestedAssemblyName, StrictReferenceMatchesDefinition, log );

        if ( assembly != null )
        {
            return assembly;
        }

        if ( _embeddedAssemblies.TryGetValue( requestedAssemblyName.Name, out var embeddedAssembly ) )
        {
            if ( _assembliesShippedWithMetalamaCompiler.Contains( requestedAssemblyName.Name ) )
            {
                // When the assembly is shipped with the Metalama.Compiler process, we need to pay attention.
                // It seems that MSBuild will use any Metalama.Compiler process of a higher version if one is available, so a project
                // compiled with a lower version of Metalama.Backstage and Metalama.Compiler.Interfaces may end up with a higher version.

                log?.AppendLine( $"'{requestedAssemblyName.Name}' is an assembly provided by Metalama.Compiler. A higher version can be accepted." );
                assembly = GetAlreadyLoadedAssembly( requestedAssemblyName, VersionTolerantReferenceMatchesDefinition, log );

                if ( assembly != null )
                {
                    log?.AppendLine( $"'{requestedAssemblyName.Name}' was already loaded (version '{assembly.GetName().Version}')" );

                    return assembly;
                }

                log?.AppendLine(
                    $"'{requestedAssemblyName.Name}' was not loaded yet. Trying to provide the embedded version '{embeddedAssembly.Name.Version}'." );
            }
            else
            {
                log?.AppendLine( $"'{requestedAssemblyName.Name}' is an embedded assembly. Requiring the exact version." );
            }

            if ( embeddedAssembly.Name.Version == requestedAssemblyName.Version )
            {
                log?.AppendLine( $"Loading the embedded assembly '{embeddedAssembly.Path}'." );

                // It seems assemblies loaded into an ALC don't participate in COM type equivalence.
                // Since we need that for the DesignTime.Contracts assembly, load it without using ALC.

                // ReSharper disable once StringStartsWithIsCultureSpecific
                if ( name.StartsWith( $"{_designTimeContractsAssemblyName}," ) )
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
            log?.AppendLine( $"'{requestedAssemblyName.Name}' is not an embedded assembly. Accepting any upper version." );

            return GetAlreadyLoadedAssembly( requestedAssemblyName, VersionTolerantReferenceMatchesDefinition, log );
        }
    }

    private static Assembly? GetAlreadyLoadedAssembly(
        AssemblyName requestedAssemblyName,
        Func<AssemblyName, AssemblyName, bool> matchFunc,
        StringBuilder? log )
    {
        // We may get here because one of our assemblies is requesting a lower version of Roslyn
        // assemblies than what we have. In this case, we will return any matching assembly.

        var existingAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault( x => !AssemblyLoader.IsCollectible( x ) && matchFunc( requestedAssemblyName, x.GetName() ) );

        if ( existingAssembly != null )
        {
            log?.AppendLine( $"Found '{existingAssembly.Location}'." );
        }
        else
        {
            log?.AppendLine( "No matching assembly was found in the AppDomain." );
        }

        return existingAssembly;
    }

    private static string GetRoslynVersion()
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

        if ( version >= new Version( 4, 12 ) )
        {
            return "4.12.0";
        }
        else if ( version >= new Version( 4, 8 ) )
        {
            return "4.8.0";
        }
        else
        {
            return "4.4.0";
        }
    }
}