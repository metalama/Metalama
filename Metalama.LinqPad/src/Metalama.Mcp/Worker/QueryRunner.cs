// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading.Tasks;

namespace Metalama.Mcp.Worker;

/// <summary>
/// Runs a compiled query in a collectible <see cref="AssemblyLoadContext"/>, and unloads it afterwards.
/// </summary>
internal static class QueryRunner
{
    /// <summary>
    /// Runs the query and passes its value to <paramref name="consume"/> while the query assembly is still loaded, because
    /// a lazy LINQ result runs code of the query assembly when it is enumerated.
    /// </summary>
    /// <returns>The result of <paramref name="consume"/>, and a weak reference to the load context, which tests use to check that it unloads.</returns>
    [MethodImpl( MethodImplOptions.NoInlining )]
    public static async Task<(T Result, WeakReference LoadContext)> RunAsync<T>( byte[] assembly, QueryContext context, Func<object?, T> consume )
    {
        var loadContext = new QueryLoadContext();
        var weakReference = new WeakReference( loadContext );

        try
        {
            var queryAssembly = loadContext.LoadFromStream( new MemoryStream( assembly ) );
            var queryType = queryAssembly.GetType( QueryCompiler.QueryTypeName, throwOnError: true )!;
            var query = (QueryBase) Activator.CreateInstance( queryType )!;
            query.Initialize( context );

            var value = await query.ExecuteAsync();

            if ( value is Task task )
            {
                // A query whose value is a task, such as an async method call, is awaited, as LINQPad does.
                await task;
                value = task.GetType().GetProperty( "Result", BindingFlags.Public | BindingFlags.Instance )?.GetValue( task );

                if ( value?.GetType().FullName == "System.Threading.Tasks.VoidTaskResult" )
                {
                    value = null;
                }
            }

            return (consume( value ), weakReference);
        }
        finally
        {
            loadContext.Unload();
        }
    }

    private sealed class QueryLoadContext : AssemblyLoadContext
    {
        public QueryLoadContext() : base( "MetalamaQuery", isCollectible: true ) { }

        // Every dependency of a query, including Metalama and the workspace, resolves from the default context.
        protected override Assembly? Load( AssemblyName assemblyName ) => null;
    }
}
