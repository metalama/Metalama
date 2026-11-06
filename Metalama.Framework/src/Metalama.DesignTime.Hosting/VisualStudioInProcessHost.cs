// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#if NETFRAMEWORK
using System.Reflection;
#endif

namespace Metalama.DesignTime.Hosting;

/// <summary>
/// Makes Metalama host both processes of the Visual Studio design-time architecture in the current process, so that a test
/// can call the Visual Studio services of Metalama without Visual Studio.
/// </summary>
/// <remarks>
/// <para>
/// This class is the client side of a test contract that Metalama supports. The contract has three parts, all of which are
/// defined in <c>Metalama.Framework.DesignTime</c> and must not change without a change of this class:
/// </para>
/// <list type="bullet">
/// <item>The environment variable <see cref="EnvironmentVariableName"/> set to <see cref="EnvironmentVariableValue"/>.
/// Metalama reads it once per process, when its assemblies are loaded, so it must be set before that.</item>
/// <item>The <see cref="AppDomain"/> data slot <see cref="WaitForAnalysisDataSlotName"/>, which holds a
/// <c>Func&lt;CancellationToken, Task&gt;</c>. The task completes when Metalama has analyzed the current solution of the
/// workspace.</item>
/// <item>The <see cref="AppDomain"/> data slot <see cref="SetWorkspaceDataSlotName"/>, which holds an
/// <c>Action&lt;Workspace&gt;</c>. It does what <c>IWorkspaceReceiver.SetWorkspace</c> does, for a caller that does not
/// reference <c>Metalama.Framework.DesignTime.Contracts</c>.</item>
/// </list>
/// <para>
/// Metalama sets both slots when it creates its Visual Studio services, which happens when the source generator of
/// <c>Metalama.Framework.CompilerExtensions.dll</c> is created. <see cref="CreateGeneratorsAsync"/> creates it.
/// </para>
/// <para>
/// Metalama accepts a single workspace per process. A test process must therefore share one workspace among its tests.
/// </para>
/// </remarks>
public static class VisualStudioInProcessHost
{
    /// <summary>
    /// The name of the environment variable that selects the design-time host mode of Metalama.
    /// </summary>
    public const string EnvironmentVariableName = "METALAMA_DESIGN_TIME_HOST";

    /// <summary>
    /// The value of <see cref="EnvironmentVariableName"/> that selects the in-process Visual Studio mode.
    /// </summary>
    public const string EnvironmentVariableValue = "VisualStudioInProcess";

    /// <summary>
    /// The name of the <see cref="AppDomain"/> data slot that holds the <c>Func&lt;CancellationToken, Task&gt;</c> delegate
    /// that waits for the analysis of the current solution.
    /// </summary>
    public const string WaitForAnalysisDataSlotName = "Metalama.Framework.DesignTime.VisualStudioInProcess.WaitForAnalysis";

    /// <summary>
    /// The name of the <see cref="AppDomain"/> data slot that holds the <c>Action&lt;Workspace&gt;</c> delegate that gives the
    /// workspace to Metalama.
    /// </summary>
    public const string SetWorkspaceDataSlotName = "Metalama.Framework.DesignTime.VisualStudioInProcess.SetWorkspace";

    /// <summary>
    /// The file name of the Metalama assembly whose source generator registers the Visual Studio services.
    /// </summary>
    public const string CompilerExtensionsFileName = "Metalama.Framework.CompilerExtensions.dll";

    /// <summary>
    /// An <c>App.config</c> fragment that gives a .NET Framework test process the binding redirects that Visual Studio gives
    /// to <c>devenv</c> for the assemblies that Metalama shares with it. Put it under <c>configuration/runtime</c>, and copy
    /// the <c>System.Text.Json</c> package of version 10.0.10 or higher to the output directory.
    /// </summary>
    public const string BindingRedirectsAppConfigFragment = """
                                                            <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
                                                              <dependentAssembly>
                                                                <assemblyIdentity name="System.Text.Json" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
                                                                <bindingRedirect oldVersion="0.0.0.0-10.0.0.10" newVersion="10.0.0.10" />
                                                              </dependentAssembly>
                                                              <dependentAssembly>
                                                                <assemblyIdentity name="System.Text.Encodings.Web" publicKeyToken="cc7b13ffcd2ddd51" culture="neutral" />
                                                                <bindingRedirect oldVersion="0.0.0.0-10.0.0.10" newVersion="10.0.0.10" />
                                                              </dependentAssembly>
                                                            </assemblyBinding>
                                                            """;

    /// <summary>
    /// Gets a value indicating whether <see cref="EnvironmentVariableName"/> is set to <see cref="EnvironmentVariableValue"/>
    /// in the current process.
    /// </summary>
    public static bool IsEnabled
        => string.Equals( Environment.GetEnvironmentVariable( EnvironmentVariableName ), EnvironmentVariableValue, StringComparison.OrdinalIgnoreCase );

    /// <summary>
    /// Selects the in-process Visual Studio mode of Metalama for the current process. Call it before any Metalama assembly is
    /// loaded, that is before any Metalama analyzer or source generator is created.
    /// </summary>
    /// <param name="tempDirectory">
    /// The directory that Metalama uses instead of the temporary directory of the user, through the <c>METALAMA_TEMP</c>
    /// environment variable, or <c>null</c> to keep the current value. Metalama extracts its assemblies there, and writes its
    /// caches and its logs there.
    /// </param>
    /// <remarks>
    /// The method also sets <c>METALAMA_TELEMETRY_OPT_OUT</c> to <c>1</c> unless it is already set. On .NET Framework, it calls
    /// <see cref="VerifyBindingRedirects"/>.
    /// </remarks>
    public static void Enable( string? tempDirectory = null )
    {
        VerifyBindingRedirects();

        Environment.SetEnvironmentVariable( EnvironmentVariableName, EnvironmentVariableValue );

        if ( tempDirectory != null )
        {
            Directory.CreateDirectory( tempDirectory );
            Environment.SetEnvironmentVariable( "METALAMA_TEMP", tempDirectory );
        }

        if ( string.IsNullOrEmpty( Environment.GetEnvironmentVariable( "METALAMA_TELEMETRY_OPT_OUT" ) ) )
        {
            Environment.SetEnvironmentVariable( "METALAMA_TELEMETRY_OPT_OUT", "1" );
        }
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when the current .NET Framework process lacks the binding redirects
    /// that Metalama needs. It does nothing on .NET, which has no binding redirects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Metalama embeds <c>System.Text.Json</c> 10.0.0.10, and some of its assemblies are compiled against 9.0.0.10. Metalama
    /// provides an embedded assembly only for the exact version requested. In Visual Studio, the binding redirects of
    /// <c>devenv</c> bind every version to the one of Visual Studio. A test process must do the same, with
    /// <see cref="BindingRedirectsAppConfigFragment"/>. Without them, the creation of the Metalama source generator fails with a
    /// <see cref="FileNotFoundException"/> or a <see cref="FileLoadException"/>.
    /// </para>
    /// <para>
    /// <c>Metalama.Framework.DesignTime.Rpc</c>, which implements the named pipes, is compiled against
    /// <c>Microsoft.Bcl.AsyncInterfaces</c> 8.0.0.0, and Metalama embeds 10.0.0.10. The test process must reference the
    /// <c>Microsoft.Bcl.AsyncInterfaces</c> package and have a binding redirect to its version, which
    /// <c>AutoGenerateBindingRedirects</c> generates when another reference requests a lower version. Without it, the
    /// analysis process never registers its projects and the services wait forever.
    /// </para>
    /// </remarks>
    public static void VerifyBindingRedirects()
    {
#if NETFRAMEWORK
        const string fragmentAdvice = "Add the fragment of VisualStudioInProcessHost.BindingRedirectsAppConfigFragment under configuration/runtime "
                                      + "in the App.config file of the test project, and reference the System.Text.Json package of version 10.0.10 or higher.";

        VerifyBindingRedirect( "System.Text.Json", new Version( 9, 0, 0, 10 ), new Version( 10, 0, 0, 10 ), fragmentAdvice );
        VerifyBindingRedirect( "System.Text.Encodings.Web", new Version( 9, 0, 0, 10 ), new Version( 10, 0, 0, 10 ), fragmentAdvice );

        VerifyBindingRedirect(
            "Microsoft.Bcl.AsyncInterfaces",
            new Version( 8, 0, 0, 0 ),
            new Version( 8, 0, 0, 0 ),
            "Reference the Microsoft.Bcl.AsyncInterfaces package of version 10.0 or higher, and add a binding redirect to its version, "
            + "for instance with AutoGenerateBindingRedirects." );
#endif
    }

#if NETFRAMEWORK
    private static void VerifyBindingRedirect( string assemblyName, Version requestedVersion, Version minimumVersion, string advice )
    {
        var requestedName = $"{assemblyName}, Version={requestedVersion}, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51";
        var effectiveName = new AssemblyName( AppDomain.CurrentDomain.ApplyPolicy( requestedName ) );

        string? failure = null;

        if ( effectiveName.Version == null || effectiveName.Version < minimumVersion )
        {
            failure = $"The process has no binding redirect of '{assemblyName}' to version {minimumVersion} or higher.";
        }
        else
        {
            try
            {
                Assembly.Load( effectiveName );
            }
            catch ( IOException e )
            {
                failure = $"The version {effectiveName.Version} of '{assemblyName}', to which the process binds version {requestedVersion}, cannot be loaded: {e.Message}";
            }
        }

        if ( failure != null )
        {
            throw new InvalidOperationException( $"{failure} {advice}{Environment.NewLine}{BindingRedirectsAppConfigFragment}" );
        }
    }
#endif

    /// <summary>
    /// Creates the source generators of the <see cref="CompilerExtensionsFileName"/> analyzer reference of a project, as the
    /// Metalama extension of Visual Studio does, and returns their number. Creating them registers the Visual Studio services
    /// of Metalama in <c>DesignTimeEntryPointManager.Instance</c> and sets the data slots.
    /// </summary>
    /// <remarks>
    /// The analyzer reference caches the generators, so the workspace later uses the same instances when it computes the
    /// compilation. The generators are created on a thread of the thread pool, because the constructor extracts and loads the
    /// Metalama assemblies.
    /// </remarks>
    public static async Task<int> CreateGeneratorsAsync( Project project )
    {
        var count = 0;

        foreach ( var reference in project.AnalyzerReferences )
        {
            if ( reference.FullPath == null
                 || !string.Equals( Path.GetFileName( reference.FullPath ), CompilerExtensionsFileName, StringComparison.OrdinalIgnoreCase ) )
            {
                continue;
            }

            var language = project.Language;
            var generators = await Task.Run( () => reference.GetGenerators( language ) );
            count += generators.Length;
        }

        return count;
    }

    /// <summary>
    /// Gives a workspace to Metalama through the <see cref="SetWorkspaceDataSlotName"/> data slot. A caller that references
    /// <c>Metalama.Framework.DesignTime.Contracts</c> can call <c>IWorkspaceReceiver.SetWorkspace</c> instead, which has the
    /// same effect.
    /// </summary>
    public static void SetWorkspace( Workspace workspace )
        => GetDataSlot<Action<Workspace>>( SetWorkspaceDataSlotName )( workspace );

    /// <summary>
    /// Returns a task that completes when Metalama has analyzed the current solution of its workspace. Call it after each
    /// change of the workspace, before calling a Metalama service.
    /// </summary>
    /// <remarks>
    /// The task compares the current solution of the workspace with the last one that Metalama analyzed, so it does not depend
    /// on the order in which the workspace raises its events. When the workspace has not been given to Metalama yet, the task
    /// waits for it. The task fails when the analysis fails.
    /// </remarks>
    public static Task WaitForAnalysisAsync( CancellationToken cancellationToken )
        => GetDataSlot<Func<CancellationToken, Task>>( WaitForAnalysisDataSlotName )( cancellationToken );

    private static T GetDataSlot<T>( string name )
        where T : class
        => AppDomain.CurrentDomain.GetData( name ) as T
           ?? throw new InvalidOperationException(
               $"The data slot '{name}' is not set. Metalama sets it when the source generator of {CompilerExtensionsFileName} is created "
               + $"in a process where {nameof(Enable)} was called before any Metalama assembly was loaded. Call {nameof(CreateGeneratorsAsync)} first." );
}
