// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;

namespace Metalama.Framework.Engine.Utilities;

internal sealed class AssemblyLoader : IDisposable
{
    private static readonly PropertyInfo? _isCollectibleProperty = typeof(Assembly).GetProperty( "IsCollectible" );
    private static readonly Version _defaultVersion = new();

    private static Type? _metalamaAlcType;

    private readonly Func<string, Assembly?> _resolveAssembly;
    private readonly Func<string, Assembly> _loadFromAssemblyPath;
    private readonly Func<Stream, Stream?, Assembly> _loadFromStream;
    private readonly Func<IEnumerable<Assembly>> _getAssemblies;
    private readonly Action? _assemblyResolveUnsubscribe;

    public AssemblyLoader(
        Func<string, Assembly?> resolveAssembly,
        Func<Assembly?, bool>? globalResolveHandlerFilter = null,
        string? debugName = null )
    {
        this._resolveAssembly = resolveAssembly;

        // In most cases (devenv, Rider, OmniSharp), compiler assemblies (including System.Collections.Immutable) are loaded into the default AssemblyLoadContext
        // (or ALCs don't exist), which means loading everything using Assembly.LoadFile works (though it's also a memory leak).
        // But in RoslynCodeAnalysisService, compiler assemblies are loaded into a separate ALC, so we have to do something different.

        Type? alcType;

        try
        {
            alcType = Type.GetType( "System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader" );
        }
        catch ( FileLoadException )
        {
            // .Net Framework sometimes throws FileLoadException when trying to resolve this type, instead of returning null.
            alcType = null;
        }

        var currentAlc = alcType?.GetMethod( "GetLoadContext" )!.Invoke( null, [typeof(AssemblyLoader).Assembly] );

        if ( currentAlc != null )
        {
            // On .NET Core, we create a custom ALC, into which we load our assemblies.
            // When resolving an assembly name, it first looks into the parent ALC (which can be the compiler ALC for the main Metalama ALC).
            // If that fails, it calls the resolveAssembly delegate.

            _metalamaAlcType ??= GenerateAssemblyLoadContext( alcType! );
            var metalamaAlc = Activator.CreateInstance( _metalamaAlcType, currentAlc, resolveAssembly, $"Metalama {debugName}".TrimEnd() );

            var loadByPathMethod = alcType!.GetMethod( "LoadFromAssemblyPath" )!;
            this._loadFromAssemblyPath = (Func<string, Assembly>) Delegate.CreateDelegate( typeof(Func<string, Assembly>), metalamaAlc, loadByPathMethod );

            var loadFromStreamMethod = alcType.GetMethod( "LoadFromStream", [typeof(Stream), typeof(Stream)] )!;

            this._loadFromStream = (Func<Stream, Stream?, Assembly>) Delegate.CreateDelegate(
                typeof(Func<Stream, Stream?, Assembly>),
                metalamaAlc,
                loadFromStreamMethod );

            var getAssembliesMethod = alcType.GetProperty( "Assemblies" )!.GetMethod!;

            this._getAssemblies = (Func<IEnumerable<Assembly>>?) Delegate.CreateDelegate(
                typeof(Func<IEnumerable<Assembly>>),
                metalamaAlc,
                getAssembliesMethod )!;

            if ( globalResolveHandlerFilter != null )
            {
                var loadByNameMethod = alcType.GetMethod( "LoadFromAssemblyName" )!;

                var loadAssemblyFromAssemblyName =
                    (Func<AssemblyName, Assembly>) Delegate.CreateDelegate( typeof(Func<AssemblyName, Assembly>), metalamaAlc, loadByNameMethod );

                Assembly? GlobalResolveHandler( object? s, ResolveEventArgs e )
                    => globalResolveHandlerFilter( e.RequestingAssembly ) ? loadAssemblyFromAssemblyName( new AssemblyName( e.Name ) ) : null;

                AppDomain.CurrentDomain.AssemblyResolve += GlobalResolveHandler;
                this._assemblyResolveUnsubscribe = () => AppDomain.CurrentDomain.AssemblyResolve -= GlobalResolveHandler;
            }
        }
        else
        {
            // We are on .NET Framework.
            this._loadFromAssemblyPath = Assembly.LoadFile;

            this._loadFromStream = ( peStream, pdbStream ) => Assembly.Load( ReadBytes( peStream )!, ReadBytes( pdbStream ) );

            AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
            this._assemblyResolveUnsubscribe = () => AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
            this._getAssemblies = () => AppDomain.CurrentDomain.GetAssemblies();

            static byte[]? ReadBytes( Stream? stream )
            {
                if ( stream == null )
                {
                    return null;
                }
                else
                {
                    var memoryStream = new MemoryStream();
                    stream.CopyTo( memoryStream );

                    return memoryStream.ToArray();
                }
            }
        }
    }

    private static Type GenerateAssemblyLoadContext( Type alcType )
    {
        // This method generates code that is equivalent to:

        // sealed class MetalamaAssemblyLoadContext : AssemblyLoadContext
        // {
        //     private readonly AssemblyLoadContext _parentContext;
        //     private readonly Func<string, Assembly?> _resolveAssembly;
        //
        //     public MetalamaAssemblyLoadContext( AssemblyLoadContext parentContext, Func<string, Assembly?> resolveAssembly, string debugName )
        //         : base( debugName, isCollectible: true )
        //     {
        //         this._parentContext = parentContext;
        //         this._resolveAssembly = resolveAssembly;
        //     }
        //
        //     protected override Assembly? Load( AssemblyName assemblyName )
        //     {
        //         try
        //         {
        //             if ( _parentContext.LoadFromAssemblyName( assemblyName ) is { } parentAssembly )
        //             {
        //                 return parentAssembly;
        //             }
        //         }
        //         catch
        //         {
        //         }
        //
        //         return _resolveAssembly.Invoke( assemblyName.FullName );
        //     }
        // }

        var resolveAssemblyType = typeof(Func<string, Assembly?>);

        var assembly = AssemblyBuilder.DefineDynamicAssembly( new AssemblyName( "Metalama.Loader" ), AssemblyBuilderAccess.RunAndCollect );
        var module = assembly.DefineDynamicModule( "Metalama.Loader" );
        var type = module.DefineType( "MetalamaAssemblyLoadContext", TypeAttributes.Sealed, alcType );

        var parentContextField = type.DefineField( "_parentContext", alcType, FieldAttributes.InitOnly );
        var resolveAssemblyField = type.DefineField( "_resolveAssembly", resolveAssemblyType, FieldAttributes.InitOnly );

        var ctor = type.DefineConstructor( MethodAttributes.Public, CallingConventions.Standard, [alcType, resolveAssemblyType, typeof(string)] );

        var ilg = ctor.GetILGenerator();

        ilg.Emit( Ldarg_0 );
        ilg.Emit( Ldarg_3 );
        ilg.Emit( Ldc_I4_1 );
        ilg.Emit( Call, alcType.GetConstructor( [typeof(string), typeof(bool)] )! );

        ilg.Emit( Ldarg_0 );
        ilg.Emit( Ldarg_1 );
        ilg.Emit( Stfld, parentContextField );

        ilg.Emit( Ldarg_0 );
        ilg.Emit( Ldarg_2 );
        ilg.Emit( Stfld, resolveAssemblyField );

        ilg.Emit( Ret );

        var loadMethod = type.DefineMethod(
            "Load",
            MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
            CallingConventions.HasThis,
            typeof(Assembly),
            [typeof(AssemblyName)] );

        ilg = loadMethod.GetILGenerator();

        var assemblyLocal = ilg.DeclareLocal( typeof(Assembly) );

        var leaveNullLabel = ilg.DefineLabel();
        var notNullLabel = ilg.DefineLabel();
        var nullLabel = ilg.DefineLabel();

        ilg.BeginExceptionBlock();

        ilg.Emit( Ldarg_0 );
        ilg.Emit( Ldfld, parentContextField );
        ilg.Emit( Ldarg_1 );
        ilg.Emit( Callvirt, alcType.GetMethod( "LoadFromAssemblyName" )! );
        ilg.Emit( Stloc, assemblyLocal );

        ilg.Emit( Ldloc, assemblyLocal );
        ilg.Emit( Brfalse, leaveNullLabel );

        ilg.Emit( Leave, notNullLabel );
        ilg.MarkLabel( leaveNullLabel );
        ilg.Emit( Leave, nullLabel );

        ilg.BeginCatchBlock( typeof(Exception) );

        ilg.Emit( Pop );
        ilg.Emit( Leave, nullLabel );

        ilg.EndExceptionBlock();

        ilg.MarkLabel( nullLabel );

        ilg.Emit( Ldarg_0 );
        ilg.Emit( Ldfld, resolveAssemblyField );
        ilg.Emit( Ldarg_1 );
        ilg.Emit( Callvirt, typeof(AssemblyName).GetProperty( nameof(AssemblyName.FullName) )!.GetMethod! );
        ilg.Emit( Callvirt, resolveAssemblyType.GetMethod( nameof(Func<string, Assembly>.Invoke) )! );
        ilg.Emit( Ret );

        ilg.MarkLabel( notNullLabel );
        ilg.Emit( Ldloc, assemblyLocal );
        ilg.Emit( Ret );

        return type.CreateTypeInfo()!.AsType();
    }

    private Assembly? OnAssemblyResolve( object? sender, ResolveEventArgs args ) => this._resolveAssembly( args.Name );

    public Assembly LoadFromPath( string assemblyPath ) => this._loadFromAssemblyPath( assemblyPath );

    public Assembly LoadFromStream( Stream peStream, Stream? pdbStream ) => this._loadFromStream( peStream, pdbStream );

    // .NET 5.0 has collectible assemblies, but collectible assemblies cannot be returned to AppDomain.AssemblyResolve.
    internal static bool IsCollectible( Assembly assembly ) => _isCollectibleProperty != null && (bool) _isCollectibleProperty.GetValue( assembly )!;

    public void Dispose() => this._assemblyResolveUnsubscribe?.Invoke();

    public bool TryGetLoadedAssembly( AssemblyName assemblyName, out Assembly? existingAssembly )
    {
        // The assembly might have been already loaded. In this situation, we must use the copy that was previously loaded.

        var assembliesOfSameIdentity = this._getAssemblies()
            .Where(
                a =>
                {
                    var candidateName = a.GetName();

                    return candidateName.Name == assemblyName.Name &&
                           (candidateName.Version ?? _defaultVersion).Equals( assemblyName.Version ?? _defaultVersion ) &&
                           (candidateName.GetPublicKeyToken() ?? []).SequenceEqual( assemblyName.GetPublicKeyToken() ?? [] );
                } )
            .ToList();

        if ( assembliesOfSameIdentity.Count >= 1 )
        {
            existingAssembly = assembliesOfSameIdentity[0];

            return true;
        }
        else
        {
            existingAssembly = null;

            return false;
        }
    }
}