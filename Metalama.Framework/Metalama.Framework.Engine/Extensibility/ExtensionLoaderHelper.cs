// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Extensibility;

public static class ExtensionLoaderHelper
{
    public static IEnumerable<string> GetExtensionAssemblies( IEnumerable<ExtensionAssemblyReference> assemblyReferences )
    {
        var targetFramework = RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net6.0";

        return assemblyReferences.Where( a => a.TargetFramework == targetFramework || string.IsNullOrEmpty( a.TargetFramework ) )
            .Select( a => a.Path );
    }

    internal static List<Type> LoadExtensionTypes(
        CompileTimeDomain domain,
        ExtensionKind extensionKind,
        IEnumerable<ExtensionAssemblyReference> assemblyReferences )
    {
        // First load assemblies, because we don't know their order and dependencies.
        var assemblies = LoadExtensionAssemblies( domain, assemblyReferences );

        // Now we can load the types.
        return LoadExtensionTypes( extensionKind, assemblies );
    }

    private static List<Assembly> LoadExtensionAssemblies(
        CompileTimeDomain domain,
        IEnumerable<ExtensionAssemblyReference> assemblyReferences )
    {
        // It is essential to materialize the query into a list, otherwise assemblies are not loaded if the caller does not evaluate the query.

        return GetExtensionAssemblies( assemblyReferences ).Select( path => domain.LoadAssembly( path, false ) ).ToList();
    }

    public static List<Type> LoadExtensionTypes( CompileTimeDomain domain, ExtensionKind extensionKind, IEnumerable<string> assemblies )
    {
        var loadedAssemblies = assemblies.Select( path => domain.LoadAssembly( path, false ) ).ToList();

        return LoadExtensionTypes( extensionKind, loadedAssemblies );
    }

    private static List<Type> LoadExtensionTypes( ExtensionKind extensionKind, IEnumerable<Assembly> assemblies )
    {
        return assemblies
            .SelectMany(
                assembly => assembly.GetCustomAttributes<ExportExtensionAttribute>()
                    .Where( attribute => attribute.ExtensionKind == extensionKind )
                    .Select( attribute => attribute.ExtensionType ) )
            .ToList();
    }
}