// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class CompileTimeSerializationBinder : BaseCompileTimeSerializationBinder
{
    private readonly CompileTimeProject? _project;
    private static readonly string _systemAssemblyName = typeof(object).Assembly.FullName.AssertNotNull();
    private readonly IDeserializationSurrogateProvider? _deserializationSurrogateProvider;

    /// <param name="domain">The compile-time domain, or <c>null</c> when no <see cref="CompileTimeProject"/> is available
    /// (see <see cref="BaseCompileTimeSerializationBinder"/> for details).</param>
    /// <param name="serviceProvider">The project service provider.</param>
    /// <param name="project">The compile-time project, or <c>null</c> during early initialization.</param>
    public CompileTimeSerializationBinder( CompileTimeDomain? domain, in ProjectServiceProvider serviceProvider, CompileTimeProject? project ) : base(
        domain,
        serviceProvider )
    {
        this._project = project;
        this._deserializationSurrogateProvider = serviceProvider.GetService<IDeserializationSurrogateProvider>();
    }

    public override void BindToName( Type type, out string typeName, out string assemblyName )
    {
        if ( type is CompileTimeType )
        {
            // A mock cannot be asked for its Assembly, and never stands for a type of a compile-time assembly anyway,
            // so none of the mapping below applies. The base reads the run-time assembly name the mock carries.
            base.BindToName( type, out typeName, out assemblyName );

            return;
        }

        var typeAssemblyName = type.Assembly.GetName().Name;

        if ( typeAssemblyName != null && CompileTimeCompilationBuilder.IsCompileTimeAssemblyName( typeAssemblyName ) )
        {
            // When we have a compile-time, we need to store the run-time name of its assembly because the compile-time name
            // can change according to random factors like the max path or the framework name, which would not be safe accross
            // versions, machines and frameworks.

            if ( this._project != null && this._project.TryGetProjectByCompileTimeAssemblyName( typeAssemblyName, out var project ) )
            {
                typeName = type.FullName.AssertNotNull();
                assemblyName = project.RunTimeIdentity.Name;
            }
            else
            {
                throw new AssertionFailedException( $"'{typeAssemblyName}' is a compile-time assembly but it is not a part of the current project." );
            }
        }
        else
        {
            base.BindToName( type, out typeName, out assemblyName );
        }
    }

    public override Type? BindToType( string typeName, string assemblyName )
    {
        if ( this._deserializationSurrogateProvider?.TryGetDeserializationSurrogate( typeName, out var surrogate ) == true )
        {
            return surrogate;
        }

        if ( assemblyName.Equals( "mscorlib", StringComparison.Ordinal )
             || assemblyName.Equals( "System.Private.CoreLib", StringComparison.Ordinal ) )
        {
            // We have a reference to a system assembly, whose simple name differs between .NET Framework (mscorlib)
            // and .NET Core (System.Private.CoreLib). Resolve the type against the corlib currently loaded in this
            // process. We resolve it directly here rather than delegating to the base binder: the base binder looks up
            // assembly names in a dictionary keyed by simple name, so the full corlib name would always miss and log a
            // spurious "is not a known assembly name" warning (#1732). This short-circuit reproduces the resolution the
            // base binder would have performed anyway (the corlib is never a compile-time domain assembly).
            return Type.GetType( ReflectionHelper.GetAssemblyQualifiedTypeName( typeName, _systemAssemblyName ) );
        }

        if ( this._project != null && this._project.TryGetType( typeName, assemblyName, out var type ) )
        {
            return type;
        }
        else
        {
            return base.BindToType( typeName, assemblyName );
        }
    }
}