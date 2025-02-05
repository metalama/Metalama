// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Microsoft.CodeAnalysis;
using System.IO;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class CompileTimeSerializer
{
    static CompileTimeSerializer()
    {
        MetalamaEngineModuleInitializer.EnsureInitialized();
    }

    private readonly ProjectServiceProvider _serviceProvider;

    /// <summary>
    /// Gets the <see cref="BaseCompileTimeSerializationBinder"/> used by the current <see cref="CompileTimeSerializer"/> to bind types to/from type names.
    /// </summary>
    internal BaseCompileTimeSerializationBinder Binder { get; }

    internal Compilation Compilation => this.CompilationContext.Compilation;

    internal CompilationContext CompilationContext { get; }

    internal SerializerProvider SerializerProvider { get; }

    private CompileTimeSerializer( CompileTimeProject? project, in ProjectServiceProvider serviceProvider, CompilationContext compilationContext ) : this(
        serviceProvider,
        new CompileTimeSerializationBinder( serviceProvider, project ),
        compilationContext ) { }

    public static CompileTimeSerializer CreateInstance( in ProjectServiceProvider serviceProvider, CompilationContext compilationContext )
        => new( serviceProvider.GetService<CompileTimeProject>(), serviceProvider, compilationContext );

    /// <summary>
    /// Initializes a new instance of the <see cref="CompileTimeSerializer"/> class.
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="binder">A <see cref="BaseCompileTimeSerializationBinder"/> customizing bindings between types and type names, or <c>null</c> to use the default implementation.</param>
    private CompileTimeSerializer( in ProjectServiceProvider serviceProvider, BaseCompileTimeSerializationBinder binder, CompilationContext compilationContext )
    {
        this._serviceProvider = serviceProvider;
        this.Binder = binder;
        this.CompilationContext = compilationContext;
        this.SerializerProvider = new SerializerProvider( serviceProvider.GetRequiredService<ISerializerFactoryProvider>() );
    }

    /// <summary>
    /// Serializes an object (and the complete graph whose this object is the root) into a <see cref="Stream"/>.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <param name="stream">The stream where <paramref name="obj"/> needs to be serialized.</param>
    public void Serialize( object? obj, Stream stream )
    {
        try
        {
            var serializationWriter = new SerializationWriter( this._serviceProvider, stream, this, false );
            serializationWriter.Serialize( obj );
        }
        catch ( CompileTimeSerializationException )
        {
            var serializationWriter = new SerializationWriter( this._serviceProvider, Stream.Null, this, true );
            serializationWriter.Serialize( obj );
        }
    }

    /// <summary>
    /// Deserializes a stream.
    /// </summary>
    /// <param name="stream">A <see cref="Stream"/> containing a serialized object graph.</param>
    /// <returns>The root object of the object graph serialized in <paramref name="stream"/>.</returns>
    public object? Deserialize( Stream stream, string? assemblyName = null )
    {
        assemblyName ??= "unknown";

        try
        {
            return Try( false );
        }
        catch ( CompileTimeSerializationException ) when ( stream.CanSeek )
        {
            stream.Seek( 0, SeekOrigin.Begin );

            return Try( true );
        }

        object? Try( bool shouldReportExceptionCause )
        {
            var serializationReader = new SerializationReader(
                stream,
                this,
                shouldReportExceptionCause,
                assemblyName,
                this.CompilationContext );

            return serializationReader.Deserialize();
        }
    }
}