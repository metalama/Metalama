// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime
{
    /// <summary>
    /// Reads the environment variable that makes the current process host both processes of the Visual Studio design-time
    /// architecture, so that a test can exercise the Visual Studio code paths without Visual Studio.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Visual Studio runs the design-time components of Metalama in two processes: the user process (<c>devenv</c>), which
    /// hosts the services that the Visual Studio extension consumes, and the analysis process, which hosts the aspect
    /// pipeline. The two processes communicate through named pipes. The kind of the current process normally decides which
    /// of the two roles the process takes.
    /// </para>
    /// <para>
    /// When the <see cref="EnvironmentVariableName"/> environment variable is set to <see cref="VisualStudioInProcessValue"/>,
    /// the process takes the role of the user process whatever its name is, and it also creates the analysis process in the
    /// same process. The two roles still communicate through named pipes. This mode exists only for tests. It is off unless
    /// the variable is set, so it does not change the behavior of Metalama in Visual Studio, in Rider, or in the compiler.
    /// </para>
    /// <para>
    /// This file is compiled into <c>Metalama.Framework.DesignTime</c>, <c>Metalama.Framework.CompilerExtensions</c> and
    /// <c>Metalama.Framework.EditorExtensions</c>, because the entry points of the two extension assemblies must take the
    /// same decision as the design-time assembly that they load.
    /// </para>
    /// </remarks>
    internal static class DesignTimeHostOverride
    {
        /// <summary>
        /// The name of the environment variable that selects the design-time host mode.
        /// </summary>
        public const string EnvironmentVariableName = "METALAMA_DESIGN_TIME_HOST";

        /// <summary>
        /// The value of <see cref="EnvironmentVariableName"/> that selects the in-process Visual Studio mode.
        /// </summary>
        public const string VisualStudioInProcessValue = "VisualStudioInProcess";

        /// <summary>
        /// Gets a value indicating whether the current process hosts both Visual Studio processes. The value is read once,
        /// when the class is initialized, and it does not change for the lifetime of the process.
        /// </summary>
        public static bool IsVisualStudioInProcess { get; } = string.Equals(
            System.Environment.GetEnvironmentVariable( EnvironmentVariableName ),
            VisualStudioInProcessValue,
            System.StringComparison.OrdinalIgnoreCase );
    }
}
