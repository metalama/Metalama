// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class CompileTimeSerializationBinder : BaseCompileTimeSerializationBinder
{
    private readonly CompileTimeProject? _project;
    private static readonly string _systemAssemblyName = typeof(object).Assembly.FullName.AssertNotNull();
    private readonly IDeserializationSurrogateProvider? _deserializationSurrogateProvider;

    public CompileTimeSerializationBinder( in ProjectServiceProvider serviceProvider, CompileTimeProject? project ) : base( serviceProvider )
    {
        this._project = project;
        this._deserializationSurrogateProvider = serviceProvider.GetService<IDeserializationSurrogateProvider>();
    }

    public override void BindToName( Type type, out string typeName, out string assemblyName )
    {
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
            // We have a reference to a system assembly, which is different under .NET Framework and .NET Core.
            // Replace by the current system assembly.
            assemblyName = _systemAssemblyName;
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