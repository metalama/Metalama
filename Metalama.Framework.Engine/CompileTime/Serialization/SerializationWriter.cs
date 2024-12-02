// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class SerializationWriter
{
    private readonly SerializationBinaryWriter _binaryWriter;

    private readonly Queue<SerializationQueueItem<object>> _serializationQueue = new();

    private readonly ProjectServiceProvider _serviceProvider;
    private readonly CompileTimeSerializer _formatter;
    private readonly bool _shouldReportExceptionCause;

    private readonly Dictionary<Type, AssemblyTypeName> _typeNameCache = new();
    private readonly Dictionary<ITypeSymbol, AssemblyTypeName> _typeSymbolNameCache = new();
    private readonly Dictionary<object, ObjectInfo> _objects = new( new CanonicalComparer() );

    private readonly UserCodeInvoker _userCodeInvoker;

    public SerializationWriter( in ProjectServiceProvider serviceProvider, Stream stream, CompileTimeSerializer formatter, bool shouldReportExceptionCause )
    {
        this._serviceProvider = serviceProvider;
        this._formatter = formatter;
        this._shouldReportExceptionCause = shouldReportExceptionCause;
        this._binaryWriter = new SerializationBinaryWriter( new BinaryWriter( stream ) );
        this._userCodeInvoker = serviceProvider.GetRequiredService<UserCodeInvoker>();
    }

    public void Serialize( object? obj )
    {
        this._binaryWriter.WriteCompressedInteger( SerializationProtocol.CurrentVersion );

        // Assertion was added after importing code from PostSharp.
        var cause = this._shouldReportExceptionCause ? SerializationCause.Root( obj.AssertNotNull().GetType() ) : null;

        this._serializationQueue.Enqueue( new SerializationQueueItem<object>( obj, cause ) );

        while ( this._serializationQueue.Count > 0 )
        {
            var item = this._serializationQueue.Dequeue();

            this.WriteObject( item.Value, item.Cause );
        }

        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicType.None );
    }

    private static void CallOnSerialization( object obj )
    {
        ICompileTimeSerializationCallback? callback;

        if ( (callback = obj as ICompileTimeSerializationCallback) != null )
        {
            callback.OnSerializing();
        }
    }

    private ObjectInfo GetObjectInfo( object obj, SerializationCause? cause )
    {
        var type = obj.GetType();

        if ( type.IsValueType )
        {
            throw new ArgumentOutOfRangeException( nameof(obj) );
        }

        if ( !this._objects.TryGetValue( obj, out var objectInfo ) )
        {
            CallOnSerialization( obj );

            var serializer = type.IsArray ? null : this._formatter.SerializerProvider.GetSerializer( type, cause );

            objectInfo = new ObjectInfo( this._formatter, obj, this._objects.Count + 1 );

            if ( !type.IsArray )
            {
                this.TrySerialize( serializer.AssertNotNull(), obj, objectInfo.ConstructorArguments, objectInfo.InitializationArguments, cause );
            }

            this._objects.Add( obj, objectInfo );
        }

        return objectInfo;
    }

    private void TrySerialize(
        ISerializer serializer,
        object obj,
        IArgumentsWriter constructorArguments,
        IArgumentsWriter initializationArguments,
        SerializationCause? cause )
    {
        try
        {
            var context = new UserCodeExecutionContext( this._serviceProvider, UserCodeDescription.Create( "deserializing the {0}", obj.GetType() ) );

            this._userCodeInvoker.Invoke(
                () => serializer.SerializeObject( obj, constructorArguments, initializationArguments ),
                context );
        }
        catch ( Exception exception )
        {
            throw CompileTimeSerializationException.CreateWithCause( $"Serialization of object '{obj}'", cause, exception );
        }
    }

    private void WriteType( Type type, SerializationCause? cause, SerializationIntrinsicType intrinsicType = SerializationIntrinsicType.None )
    {
        if ( type is CompileTimeType compileTimeType )
        {
            var typeSymbol = (ITypeSymbol) compileTimeType.Target.GetSymbol( this._formatter.Compilation.AssertNotNull() ).AssertNotNull();
            this.WriteType( typeSymbol, cause, intrinsicType );

            return;
        }

        if ( intrinsicType == SerializationIntrinsicType.None )
        {
            intrinsicType = type.GetIntrinsicType();
        }

        switch ( intrinsicType )
        {
            case SerializationIntrinsicType.None:
            case SerializationIntrinsicType.Boolean:
            case SerializationIntrinsicType.Char:
            case SerializationIntrinsicType.SByte:
            case SerializationIntrinsicType.Byte:
            case SerializationIntrinsicType.Int16:
            case SerializationIntrinsicType.UInt16:
            case SerializationIntrinsicType.Int32:
            case SerializationIntrinsicType.UInt32:
            case SerializationIntrinsicType.Int64:
            case SerializationIntrinsicType.UInt64:
            case SerializationIntrinsicType.Single:
            case SerializationIntrinsicType.Double:
            case SerializationIntrinsicType.String:
            case SerializationIntrinsicType.ObjRef:
            case SerializationIntrinsicType.DottedString:
            case SerializationIntrinsicType.Type:
                this._binaryWriter.WriteByte( (byte) intrinsicType );

                break;

            case SerializationIntrinsicType.Enum:
                this._binaryWriter.WriteByte( (byte) intrinsicType );

                this._binaryWriter.WriteByte( (byte) SerializationIntrinsicTypeFlags.Default );
                this.WriteTypeName( type );

                break;

            case SerializationIntrinsicType.Array:
                this._binaryWriter.WriteByte( (byte) intrinsicType );
                this._binaryWriter.WriteCompressedInteger( type.GetArrayRank() );
                this.WriteType( type.GetElementType()!, cause );

                break;

            case SerializationIntrinsicType.Struct:
            case SerializationIntrinsicType.Class:
                {
                    var genericTypeDefinition = type is { IsGenericType: true, IsGenericTypeDefinition: false } ? type.GetGenericTypeDefinition() : type;
                    this._binaryWriter.WriteByte( (byte) intrinsicType );

                    if ( type is { IsGenericType: true, IsGenericTypeDefinition: false } )
                    {
                        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicTypeFlags.Generic );
                        this.WriteTypeName( genericTypeDefinition );

                        var genericTypeArguments = type.GetGenericArguments();

                        this._binaryWriter.WriteCompressedInteger( genericTypeArguments.Length );

                        foreach ( var genericTypeArgument in genericTypeArguments )
                        {
                            this.WriteType( genericTypeArgument, cause );
                        }
                    }
                    else
                    {
                        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicTypeFlags.Default );
                        this.WriteTypeName( genericTypeDefinition );
                    }
                }

                break;

            case SerializationIntrinsicType.GenericTypeParameter:
                this.WriteGenericTypeParameter( type, cause );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(type) );
        }
    }

    private void WriteType( ITypeSymbol typeSymbol, SerializationCause? cause, SerializationIntrinsicType intrinsicType = SerializationIntrinsicType.None )
    {
        if ( intrinsicType == SerializationIntrinsicType.None )
        {
            intrinsicType = typeSymbol.GetIntrinsicType();
        }

        switch ( intrinsicType )
        {
            case SerializationIntrinsicType.None:
            case SerializationIntrinsicType.Boolean:
            case SerializationIntrinsicType.Char:
            case SerializationIntrinsicType.SByte:
            case SerializationIntrinsicType.Byte:
            case SerializationIntrinsicType.Int16:
            case SerializationIntrinsicType.UInt16:
            case SerializationIntrinsicType.Int32:
            case SerializationIntrinsicType.UInt32:
            case SerializationIntrinsicType.Int64:
            case SerializationIntrinsicType.UInt64:
            case SerializationIntrinsicType.Single:
            case SerializationIntrinsicType.Double:
            case SerializationIntrinsicType.String:
            case SerializationIntrinsicType.ObjRef:
            case SerializationIntrinsicType.DottedString:
            case SerializationIntrinsicType.Type:
                this._binaryWriter.WriteByte( (byte) intrinsicType );

                break;

            case SerializationIntrinsicType.Enum:
                this._binaryWriter.WriteByte( (byte) intrinsicType );

                this._binaryWriter.WriteByte( (byte) SerializationIntrinsicTypeFlags.Default );
                this.WriteTypeName( typeSymbol );

                break;

            case SerializationIntrinsicType.Array:
                {
                    var arrayTypeSymbol = (IArrayTypeSymbol) typeSymbol;

                    this._binaryWriter.WriteByte( (byte) intrinsicType );
                    this._binaryWriter.WriteCompressedInteger( arrayTypeSymbol.Rank );
                    this.WriteType( arrayTypeSymbol.ElementType, cause );

                    break;
                }

            case SerializationIntrinsicType.Struct:
            case SerializationIntrinsicType.Class:
                {
                    var genericTypeDefinition = typeSymbol.OriginalDefinition;
                    this._binaryWriter.WriteByte( (byte) intrinsicType );

                    if ( typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && namedTypeSymbol.OriginalDefinition != namedTypeSymbol )
                    {
                        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicTypeFlags.Generic );
                        this.WriteTypeName( genericTypeDefinition );

                        var genericTypeArguments = namedTypeSymbol.TypeArguments;

                        this._binaryWriter.WriteCompressedInteger( genericTypeArguments.Length );

                        foreach ( var genericTypeArgument in genericTypeArguments )
                        {
                            this.WriteType( genericTypeArgument, cause );
                        }
                    }
                    else
                    {
                        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicTypeFlags.Default );
                        this.WriteTypeName( genericTypeDefinition );
                    }
                }

                break;

            case SerializationIntrinsicType.GenericTypeParameter:
                this.WriteGenericTypeParameter( (ITypeParameterSymbol) typeSymbol, cause );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(typeSymbol) );
        }
    }

    private void WriteTypeName( Type type )
    {
        if ( type is { IsGenericType: true, IsGenericTypeDefinition: false } )
        {
            throw new ArgumentOutOfRangeException( nameof(type) );
        }

        if ( !this._typeNameCache.TryGetValue( type, out var assemblyTypeName ) )
        {
            this._formatter.Binder.BindToName( type, out var typeName, out var assemblyName );

            if ( CompileTimeCompilationBuilder.IsCompileTimeAssemblyName( assemblyName ) )
            {
                throw new AssertionFailedException();
            }

            assemblyTypeName = new AssemblyTypeName( typeName, assemblyName );

            this._typeNameCache.Add( type, assemblyTypeName );
        }

        this.WriteTypeName( assemblyTypeName );
    }

    private void WriteTypeName( ITypeSymbol typeSymbol )
    {
        if ( typeSymbol is INamedTypeSymbol { IsGenericType: true } namedTypeSymbol && namedTypeSymbol.ConstructedFrom != namedTypeSymbol )
        {
            throw new ArgumentOutOfRangeException( nameof(typeSymbol) );
        }

        if ( !this._typeSymbolNameCache.TryGetValue( typeSymbol, out var assemblyTypeName ) )
        {
            this._formatter.Binder.BindToName( typeSymbol, out var typeName, out var assemblyName );
            assemblyTypeName = new AssemblyTypeName( typeName, assemblyName );

            this._typeSymbolNameCache.Add( typeSymbol, assemblyTypeName );
        }

        this.WriteTypeName( assemblyTypeName );
    }

    private void WriteTypeName( AssemblyTypeName type )
    {
        this._binaryWriter.WriteDottedString( type.TypeName );

        // We are writing the assembly name including the hash.
        this._binaryWriter.WriteString( type.AssemblyName );
    }

    private void WriteConstructionData( ObjectInfo objectInfo, SerializationCause? cause )
    {
        if ( objectInfo.ConstructionDataWritten )
        {
            throw new InvalidOperationException();
        }

        objectInfo.ConstructionDataWritten = true;

        var objectType = objectInfo.Object.GetType();
        var intrinsicType = objectType.GetIntrinsicType();

        this.WriteType( objectType, cause, intrinsicType );

        var array = objectInfo.Object as Array;

        if ( objectType.IsArray )
        {
            for ( var i = 0; i < objectType.GetArrayRank(); i++ )
            {
                this._binaryWriter.WriteCompressedInteger( array!.GetLength( i ) );
                this._binaryWriter.WriteCompressedInteger( array.GetLowerBound( i ) );
            }
        }
        else
        {
            this.WriteArguments( objectInfo.ConstructorArguments, true, cause );

            // we write construction objects inline because they can be needed during construction
        }
    }

    private void WriteInitializationData( ObjectInfo objectInfo, SerializationCause? cause )
    {
        if ( !objectInfo.ConstructionDataWritten || objectInfo.InitializationArgumentsWritten )
        {
            throw new InvalidOperationException();
        }

        objectInfo.InitializationArgumentsWritten = true;

        var type = objectInfo.Object.GetType();
        var intrinsicType = type.GetIntrinsicType();

        var array = objectInfo.Object as Array;

        if ( type.IsArray )
        {
            var indices = new int[array!.Rank];
            this.WriteArrayElements( array, array.GetType().GetElementType()!, indices, 0, cause );
        }
        else
        {
            if ( intrinsicType == SerializationIntrinsicType.Class )
            {
                this.WriteArguments( objectInfo.InitializationArguments, false, cause );
            }
        }
    }

    private void WriteTypedValue( object value, bool writeInitializationDataInline, SerializationCause? cause )
    {
        var type = value.GetType();
        var intrinsicType = type.GetIntrinsicType( true );
        this.WriteType( type, cause, intrinsicType );

        this.WriteValue( value, intrinsicType, writeInitializationDataInline, cause );
    }

    private void WriteValue( object value, SerializationIntrinsicType intrinsicType, bool writeInitializationDataInline, SerializationCause? cause )
    {
        switch ( intrinsicType )
        {
            case SerializationIntrinsicType.Byte:
                this._binaryWriter.WriteByte( (byte) value );

                break;

            case SerializationIntrinsicType.Int16:
                this._binaryWriter.WriteCompressedInteger( (short) value );

                break;

            case SerializationIntrinsicType.UInt16:
                this._binaryWriter.WriteCompressedInteger( (ushort) value );

                break;

            case SerializationIntrinsicType.Int32:
                this._binaryWriter.WriteCompressedInteger( (int) value );

                break;

            case SerializationIntrinsicType.UInt32:
                this._binaryWriter.WriteCompressedInteger( (uint) value );

                break;

            case SerializationIntrinsicType.Int64:
                this._binaryWriter.WriteCompressedInteger( (long) value );

                break;

            case SerializationIntrinsicType.UInt64:
                this._binaryWriter.WriteCompressedInteger( (ulong) value );

                break;

            case SerializationIntrinsicType.Single:
                this._binaryWriter.WriteSingle( (float) value );

                break;

            case SerializationIntrinsicType.Double:
                this._binaryWriter.WriteDouble( (double) value );

                break;

            case SerializationIntrinsicType.String:
                this._binaryWriter.WriteString( (string) value );

                break;

            case SerializationIntrinsicType.DottedString:
                this._binaryWriter.WriteDottedString( (DottedString) value );

                break;

            case SerializationIntrinsicType.Char:
                this._binaryWriter.WriteCompressedInteger( (char) value );

                break;

            case SerializationIntrinsicType.Boolean:
                this._binaryWriter.WriteByte( (byte) ((bool) value ? 1 : 0) );

                break;

            case SerializationIntrinsicType.SByte:
                this._binaryWriter.WriteSByte( (sbyte) value );

                break;

            case SerializationIntrinsicType.Struct:
                this.WriteStruct( value, cause );

                break;

            case SerializationIntrinsicType.ObjRef:
                this.WriteObjectReference( value, writeInitializationDataInline, cause );

                break;

            case SerializationIntrinsicType.Type:
                this.WriteType( (Type) value, cause );

                break;

            case SerializationIntrinsicType.Enum:
                this._binaryWriter.WriteCompressedInteger( Convert.ToInt64( value, CultureInfo.InvariantCulture ) );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(intrinsicType) );
        }
    }

    private void WriteGenericTypeParameter( Type type, SerializationCause? cause )
    {
        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicType.GenericTypeParameter );
        this.WriteType( type.DeclaringType!, cause );
        this._binaryWriter.WriteCompressedInteger( type.GenericParameterPosition );
    }

    private void WriteGenericTypeParameter( ITypeParameterSymbol typeParameterSymbol, SerializationCause? cause )
    {
        this._binaryWriter.WriteByte( (byte) SerializationIntrinsicType.GenericTypeParameter );
        this.WriteType( typeParameterSymbol.ContainingType, cause );
        this._binaryWriter.WriteCompressedInteger( typeParameterSymbol.Ordinal );
    }

    private void WriteObjectReference( object value, bool writeInitializationDataInline, SerializationCause? cause )
    {
        var objectInfo = this.GetObjectInfo( value, cause );

        this._binaryWriter.WriteCompressedInteger( objectInfo.ObjectId );

        if ( !objectInfo.ConstructionDataWritten )
        {
            // we enqueue the object first to maintain order of objects. During writing construction data objects can be added to queue
            if ( !writeInitializationDataInline )
            {
                this._serializationQueue.Enqueue( new SerializationQueueItem<object>( objectInfo.Object, cause ) );
            }

            this.WriteConstructionData( objectInfo, cause );

            if ( writeInitializationDataInline )
            {
                this.WriteInitializationData( objectInfo, cause );
            }
        }
    }

    private void WriteStruct( object value, SerializationCause? cause )
    {
        var type = value.GetType();
        var serializer = this._formatter.SerializerProvider.GetSerializer( type, cause );
        var arguments = new Arguments( this._formatter );

        this.TrySerialize( serializer, value, arguments, ThrowingArguments.Instance, cause );

        // structs have one phase serializers so we have to write initialization data inline
        this.WriteArguments( arguments, true, cause );
    }

    private void WriteArguments( Arguments arguments, bool writeInitializationArgumentsInline, SerializationCause? cause )
    {
        var count = 0;

        if ( arguments.Values.Count > 0 )
        {
            foreach ( var argument in arguments.Values )
            {
                if ( argument.Value != null )
                {
                    count++;
                }
            }
        }

        this._binaryWriter.WriteCompressedInteger( count );

        if ( count > 0 )
        {
            foreach ( var argument in arguments.Values )
            {
                if ( argument.Value == null )
                {
                    continue;
                }

                this._binaryWriter.WriteDottedString( argument.Key );

                var newCause = cause?.WithFieldAccess( argument.Key );

                this.WriteTypedValue( argument.Value, writeInitializationArgumentsInline, newCause );
            }
        }
    }

    private void WriteObject( object? obj, SerializationCause? cause )
    {
        if ( obj == null )
        {
            this._binaryWriter.WriteByte( (byte) SerializationIntrinsicType.None );

            return;
        }

        var intrinsicType = obj.GetType().GetIntrinsicType();

        switch ( intrinsicType )
        {
            case SerializationIntrinsicType.Byte:
            case SerializationIntrinsicType.Int16:
            case SerializationIntrinsicType.UInt16:
            case SerializationIntrinsicType.Int32:
            case SerializationIntrinsicType.UInt32:
            case SerializationIntrinsicType.Int64:
            case SerializationIntrinsicType.UInt64:
            case SerializationIntrinsicType.Single:
            case SerializationIntrinsicType.Double:
            case SerializationIntrinsicType.String:
            case SerializationIntrinsicType.DottedString:
            case SerializationIntrinsicType.Char:
            case SerializationIntrinsicType.Boolean:
            case SerializationIntrinsicType.SByte:
            case SerializationIntrinsicType.Struct:
            case SerializationIntrinsicType.Enum:
            case SerializationIntrinsicType.Type:
                this.WriteTypedValue( obj, false, cause );

                break;

            case SerializationIntrinsicType.Class:
            case SerializationIntrinsicType.Array:
                var objectInfo = this.GetObjectInfo( obj, cause );

                if ( !objectInfo.ConstructionDataWritten )
                {
                    // we enqueue the object first to maintain order of objects. During writing construction data objects can be added to queue
                    this._serializationQueue.Enqueue( new SerializationQueueItem<object>( obj, cause ) );
                    this.WriteConstructionData( objectInfo, cause );
                }
                else if ( objectInfo is { ConstructionDataWritten: true, InitializationArgumentsWritten: false } )
                {
                    // we can check this only in Array|Class because only these items get added on the queue from serialization code
                    this.WriteInitializationData( objectInfo, cause );
                }

                break;

            default:
                throw new AssertionFailedException( $"Invalid serialization type: {intrinsicType}." );
        }
    }

    private void WriteArrayElements( Array array, Type elementType, int[] indices, int currentDimension, SerializationCause? cause )
    {
        var length = array.GetLength( currentDimension );
        var lowerBound = array.GetLowerBound( currentDimension );

        if ( currentDimension + 1 < indices.Length )
        {
            for ( var i = 0; i < length; i++ )
            {
                indices[currentDimension] = i;
                this.WriteArrayElements( array, elementType, indices, currentDimension + 1, cause );
            }
        }
        else
        {
            var elementIntrinsicType = elementType.GetIntrinsicType( true );

            for ( var i = lowerBound; i < lowerBound + length; i++ )
            {
                indices[currentDimension] = i;
                var value = array.GetValue( indices );

                // shouldn’t structs be written with type (inheritance is possible) 
                var newCause = cause?.WithArrayAccess( i );

                if ( elementType.IsValueType )
                {
                    this.WriteValue( value!, elementIntrinsicType, false, newCause );
                }
                else if ( value == null )
                {
                    this._binaryWriter.WriteByte( (byte) SerializationIntrinsicType.None );
                }
                else
                {
                    // array can be constructed without fully deserializing its elements
                    this.WriteTypedValue( value, false, newCause );
                }
            }
        }
    }

    private sealed class ObjectInfo
    {
        public object Object { get; }

        public int ObjectId { get; }

        public Arguments ConstructorArguments { get; }

        public Arguments InitializationArguments { get; }

        public bool ConstructionDataWritten { get; set; }

        public bool InitializationArgumentsWritten { get; set; }

        public ObjectInfo( CompileTimeSerializer serializer, object o, int objectId )
        {
            this.InitializationArguments = new Arguments( serializer );
            this.ConstructorArguments = new Arguments( serializer );
            this.Object = o;
            this.ObjectId = objectId;
        }
    }

    private sealed class CanonicalComparer : IEqualityComparer<object>
    {
        bool IEqualityComparer<object>.Equals( object? x, object? y ) => ReferenceEquals( x, y );

        public int GetHashCode( object obj ) => RuntimeHelpers.GetHashCode( obj );
    }

    private sealed class Arguments : IArgumentsWriter, ISerializationContext
    {
        private readonly CompileTimeSerializer _serializer;
        private Dictionary<string, object?>? _values;
        private Dictionary<string, object?>? _contextProperties;

        public Arguments( CompileTimeSerializer serializer )
        {
            this._serializer = serializer;
        }

        public void SetValue( string name, object? value, string? scope = null )
        {
            if ( value == null )
            {
                return;
            }

            if ( scope != null )
            {
                name = scope + "." + name;
            }

            this._values ??= new Dictionary<string, object?>( StringComparer.Ordinal );
            this._values[name] = value;
        }

        public IReadOnlyDictionary<string, object?> Values
            => (IReadOnlyDictionary<string, object?>?) this._values ?? ImmutableDictionary<string, object?>.Empty;

        public CompilationContext CompilationContext => this._serializer.CompilationContext;

        public Dictionary<string, object?> ContextProperties => this._contextProperties ??= new Dictionary<string, object?>( StringComparer.Ordinal );
    }

    private sealed class ThrowingArguments : IArgumentsWriter
    {
        public static readonly ThrowingArguments Instance = new();

        public void SetValue( string name, object? value, string? scope = null ) => throw new NotSupportedException();
    }
}