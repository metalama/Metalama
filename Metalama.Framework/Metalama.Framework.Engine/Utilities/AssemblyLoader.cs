// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Reflection;
using System.Reflection.Emit;
using static System.Reflection.Emit.OpCodes;

namespace Metalama.Framework.Engine.Utilities;

internal sealed class AssemblyLoader : IDisposable
{
    private static readonly PropertyInfo? _isCollectibleProperty = typeof(Assembly).GetProperty( "IsCollectible" );

    private static Type? _metalamaAlcType;

    private readonly Func<string, Assembly?> _resolveAssembly;
    private readonly Func<string, Assembly> _loadAssembly;
    private readonly Action? _assemblyResolveUnsubscribe;

    public AssemblyLoader( Func<string, Assembly?> resolveAssembly, Func<Assembly?, bool>? globalResolveHandlerFilter = null, string? debugName = null )
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
        catch ( NullReferenceException )
        {
            // .Net Framework sometimes throws NullReferenceException when trying to resolve this type, instead of returning null.
            alcType = null;
        }

        var currentAlc = alcType?.GetMethod( "GetLoadContext" )!.Invoke( null, [typeof(AssemblyLoader).Assembly] );

        if ( currentAlc != null )
        {
            // On .Net, we create a custom ALC, into which we load our assemblies.
            // When resolving an assembly name, it first looks into the parent ALC (which can be the compiler ALC for the main Metalama ALC).
            // If that fails, it calls the resolveAssembly delegate.

            _metalamaAlcType ??= GenerateAssemblyLoadContext( alcType! );
            var metalamaAlc = Activator.CreateInstance( _metalamaAlcType, currentAlc, resolveAssembly, $"Metalama {debugName}".TrimEnd() );

            var loadByPathMethod = alcType!.GetMethod( "LoadFromAssemblyPath" )!;
            this._loadAssembly = (Func<string, Assembly>) Delegate.CreateDelegate( typeof(Func<string, Assembly>), metalamaAlc, loadByPathMethod );

            if ( globalResolveHandlerFilter != null )
            {
                var loadByNameMethod = alcType.GetMethod( "LoadFromAssemblyName" )!;

                var loadByNameDelegate =
                    (Func<AssemblyName, Assembly>) Delegate.CreateDelegate( typeof(Func<AssemblyName, Assembly>), metalamaAlc, loadByNameMethod );

                Assembly? GlobalResolveHandler( object? s, ResolveEventArgs e )
                    => globalResolveHandlerFilter( e.RequestingAssembly ) ? loadByNameDelegate( new AssemblyName( e.Name ) ) : null;

                AppDomain.CurrentDomain.AssemblyResolve += GlobalResolveHandler;
                this._assemblyResolveUnsubscribe = () => AppDomain.CurrentDomain.AssemblyResolve -= GlobalResolveHandler;
            }

            return;
        }

        this._loadAssembly = Assembly.LoadFile;

        AppDomain.CurrentDomain.AssemblyResolve += this.OnAssemblyResolve;
        this._assemblyResolveUnsubscribe = () => AppDomain.CurrentDomain.AssemblyResolve -= this.OnAssemblyResolve;
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

    public Assembly LoadAssembly( string assemblyPath ) => this._loadAssembly( assemblyPath );

    // .NET 5.0 has collectible assemblies, but collectible assemblies cannot be returned to AppDomain.AssemblyResolve.
    internal static bool IsCollectible( Assembly assembly ) => _isCollectibleProperty != null && (bool) _isCollectibleProperty.GetValue( assembly )!;

    public void Dispose() => this._assemblyResolveUnsubscribe?.Invoke();
}