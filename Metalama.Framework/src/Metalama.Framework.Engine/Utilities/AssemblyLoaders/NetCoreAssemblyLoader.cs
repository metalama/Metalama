// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Metalama.Framework.Engine.Utilities.AssemblyLoaders;

internal class NetCoreAssemblyLoader : AssemblyLoader
{
    private static readonly PropertyInfo _isCollectibleProperty = typeof(Assembly).GetProperty( "IsCollectible" )
                                                                  ?? throw new InvalidOperationException( "Cannot find the Assembly.IsCollectible property" );

    private static Type? _metalamaAlcType;

    private readonly Func<string, Assembly> _loadFromAssemblyPath;
    private readonly Func<Stream, Stream?, Assembly> _loadFromStream;
    private readonly Func<IEnumerable<Assembly>> _getAssemblies;
    private readonly ResolveEventHandler? _globalResolveHandler;

    public NetCoreAssemblyLoader( Func<string, Assembly?> resolveAssembly, Func<Assembly?, bool>? globalResolveHandlerFilter, string? debugName ) : base(
        debugName )
    {
        var alcType = Type.GetType( "System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader" )
                      ?? throw new InvalidOperationException( "Cannot find the AssemblyLoadContext type." );

        var getLoadContextMethod = alcType.GetMethod( "GetLoadContext" )
                                   ?? throw new InvalidOperationException( "Cannot find the GetLoadContext method." );

        var currentAlc = getLoadContextMethod.Invoke( null, [typeof(AssemblyLoader).Assembly] );

        // On .NET Core, we create a custom ALC, into which we load our assemblies.
        // When resolving an assembly name, it first looks into the parent ALC (which can be the compiler ALC for the main Metalama ALC).
        // If that fails, it calls the resolveAssembly delegate.

        _metalamaAlcType ??= GenerateAssemblyLoadContext( alcType );
        var metalamaAlc = Activator.CreateInstance( _metalamaAlcType, currentAlc, resolveAssembly, debugName );

        var loadByPathMethod = alcType!.GetMethod( "LoadFromAssemblyPath" )
                               ?? throw new InvalidOperationException( "cannot find the LoadFromAssemblyPath method." );

        this._loadFromAssemblyPath = (Func<string, Assembly>) Delegate.CreateDelegate( typeof(Func<string, Assembly>), metalamaAlc, loadByPathMethod );

        var loadFromStreamMethod = alcType.GetMethod( "LoadFromStream", [typeof(Stream), typeof(Stream)] )
                                   ?? throw new InvalidOperationException( "Cannot find the LoadFromStream method." );

        this._loadFromStream = (Func<Stream, Stream?, Assembly>) Delegate.CreateDelegate(
            typeof(Func<Stream, Stream?, Assembly>),
            metalamaAlc,
            loadFromStreamMethod );

        var getAssembliesMethod = alcType.GetProperty( "Assemblies" )?.GetMethod
                                  ?? throw new InvalidOperationException( "Cannot find the Assemblies property." );

        this._getAssemblies = (Func<IEnumerable<Assembly>>?) Delegate.CreateDelegate(
            typeof(Func<IEnumerable<Assembly>>),
            metalamaAlc,
            getAssembliesMethod )!;

        if ( globalResolveHandlerFilter != null )
        {
            var loadByNameMethod = alcType.GetMethod( "LoadFromAssemblyName" )
                                   ?? throw new InvalidOperationException( "Cannot find the LoadFromAssemblyName method." );

            var loadAssemblyFromAssemblyName =
                (Func<AssemblyName, Assembly>) Delegate.CreateDelegate( typeof(Func<AssemblyName, Assembly>), metalamaAlc, loadByNameMethod );

            this._globalResolveHandler = ( sender, e )
                => globalResolveHandlerFilter( e.RequestingAssembly ) ? loadAssemblyFromAssemblyName( new AssemblyName( e.Name ) ) : null;

            AppDomain.CurrentDomain.AssemblyResolve += this._globalResolveHandler;
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

        ilg.Emit( OpCodes.Ldarg_0 );
        ilg.Emit( OpCodes.Ldarg_3 );
        ilg.Emit( OpCodes.Ldc_I4_1 );
        ilg.Emit( OpCodes.Call, alcType.GetConstructor( [typeof(string), typeof(bool)] )! );

        ilg.Emit( OpCodes.Ldarg_0 );
        ilg.Emit( OpCodes.Ldarg_1 );
        ilg.Emit( OpCodes.Stfld, parentContextField );

        ilg.Emit( OpCodes.Ldarg_0 );
        ilg.Emit( OpCodes.Ldarg_2 );
        ilg.Emit( OpCodes.Stfld, resolveAssemblyField );

        ilg.Emit( OpCodes.Ret );

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

        ilg.Emit( OpCodes.Ldarg_0 );
        ilg.Emit( OpCodes.Ldfld, parentContextField );
        ilg.Emit( OpCodes.Ldarg_1 );
        ilg.Emit( OpCodes.Callvirt, alcType.GetMethod( "LoadFromAssemblyName" )! );
        ilg.Emit( OpCodes.Stloc, assemblyLocal );

        ilg.Emit( OpCodes.Ldloc, assemblyLocal );
        ilg.Emit( OpCodes.Brfalse, leaveNullLabel );

        ilg.Emit( OpCodes.Leave, notNullLabel );
        ilg.MarkLabel( leaveNullLabel );
        ilg.Emit( OpCodes.Leave, nullLabel );

        ilg.BeginCatchBlock( typeof(Exception) );

        ilg.Emit( OpCodes.Pop );
        ilg.Emit( OpCodes.Leave, nullLabel );

        ilg.EndExceptionBlock();

        ilg.MarkLabel( nullLabel );

        ilg.Emit( OpCodes.Ldarg_0 );
        ilg.Emit( OpCodes.Ldfld, resolveAssemblyField );
        ilg.Emit( OpCodes.Ldarg_1 );
        ilg.Emit( OpCodes.Callvirt, typeof(AssemblyName).GetProperty( nameof(AssemblyName.FullName) )!.GetMethod! );
        ilg.Emit( OpCodes.Callvirt, resolveAssemblyType.GetMethod( nameof(Func<string, Assembly>.Invoke) )! );
        ilg.Emit( OpCodes.Ret );

        ilg.MarkLabel( notNullLabel );
        ilg.Emit( OpCodes.Ldloc, assemblyLocal );
        ilg.Emit( OpCodes.Ret );

        return type.CreateTypeInfo()!.AsType();
    }

    public override Assembly LoadFromPath( string assemblyPath ) => this._loadFromAssemblyPath( assemblyPath );

    public override Assembly LoadFromStream( Stream peStream, Stream? pdbStream ) => this._loadFromStream( peStream, pdbStream );

    public override bool IsCollectible( Assembly assembly ) => (bool) _isCollectibleProperty.GetValue( assembly )!;

    protected override IEnumerable<Assembly> GetAssemblies() => this._getAssemblies();

    public override void Dispose()
    {
        base.Dispose();

        if ( this._globalResolveHandler != null )
        {
            AppDomain.CurrentDomain.AssemblyResolve -= this._globalResolveHandler;
        }
    }
}