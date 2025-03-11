// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Factories;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Serialization;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Metalama.Framework.Engine.CompileTime.Serialization;

internal sealed class SerializationReader
{
    private readonly Dictionary<int, SerializationQueueItem<ObjRef>> _referenceTypeInstances = new();

    private readonly CompileTimeSerializer _formatter;
    private readonly bool _shouldReportExceptionCause;
    private readonly string _assemblyName;
    private readonly SerializationBinaryReader _binaryReader;
    private readonly CompileTimeTypeFactory? _compileTimeTypeFactory;
    private readonly InstanceFields _emptyInstanceFields;

    internal SerializationReader(
        Stream stream,
        CompileTimeSerializer formatter,
        bool shouldReportExceptionCause,
        string assemblyName,
        CompilationContext compilationContext )
    {
        this._formatter = formatter;
        this._shouldReportExceptionCause = shouldReportExceptionCause;
        this._assemblyName = assemblyName;
        this._binaryReader = new SerializationBinaryReader( new BinaryReader( stream ) );
        this._compileTimeTypeFactory = compilationContext.CompileTimeTypeFactory;
        this._emptyInstanceFields = new InstanceFields( formatter );
    }

    public object? Deserialize()
    {
        int v = this._binaryReader.ReadCompressedInteger();

        if ( v is > SerializationProtocol.CurrentVersion or < SerializationProtocol.LastSupportedVersion )
        {
            throw new NotSupportedException(
                $"The assembly '{this._assemblyName}' was compiled with an incompatible version of Metalama (actual version: {v}, supported versions: {SerializationProtocol.CurrentVersion}-{SerializationProtocol.LastSupportedVersion} ). The '{this._assemblyName}' project must be recompiled or the package updated." );
        }

        var instanceId = 1;
        var rootObject = this.ReadObject( instanceId, true, null );

        // TODO: Consider refactoring. Should actually call read type and then decide whether to read object or call ReadValue.
        // But that's a lot of work. For now GetObjRef has a check to ignore instanceId for value types.

        for ( instanceId++; instanceId <= this._referenceTypeInstances.Count; instanceId++ )
        {
            this.InitializeObject( instanceId );
        }

        ICompileTimeSerializationCallback? callback;

        if ( (callback = rootObject as ICompileTimeSerializationCallback) != null )
        {
            callback.OnDeserialized();
        }

        foreach ( var obj in this._referenceTypeInstances.Values )
        {
            if ( (callback = obj.Value.AssertNotNull().Value as ICompileTimeSerializationCallback) != null && !ReferenceEquals( callback, rootObject ) )
            {
                callback.OnDeserialized();
            }
        }

        return rootObject;
    }

    private object? ReadObject( int instanceId, bool initializeObject, SerializationCause? cause )
    {
        if ( this._referenceTypeInstances.TryGetValue( instanceId, out var item ) )
        {
            return item.Value.AssertNotNull().Value;
        }

        var objRef = this.GetObjRef( instanceId, cause! );

        return this.ReadObjectInternal( objRef, instanceId, initializeObject );
    }

    private object? ReadObjectInternal( ObjRef objRef, int instanceId, bool initializeObject )
    {
        if ( objRef.Value == null )
        {
            return null;
        }

        // object can be ValueType so we need to check IsInitialized
        if ( !objRef.IsInitialized && initializeObject )
        {
            this.InitializeObject( instanceId );
        }

        return objRef.Value;
    }

    private void InitializeObject( int instanceId )
    {
        var item = this._referenceTypeInstances[instanceId];

        var objRef = item.Value.AssertNotNull();

        // object could be initialized in constructionData block
        if ( objRef.IsInitialized )
        {
            return;
        }

        objRef.IsInitialized = true;

        var type = objRef.Value!.GetType();

        if ( type.IsArray )
        {
            this.ReadArray( (Array) objRef.Value, item.Cause );
        }
        else
        {
            if ( objRef.IntrinsicType == SerializationIntrinsicType.Class )
            {
                var fields = this.ReadInstanceFields( type, false, item.Cause );

                if ( objRef.Serializer!.IsTwoPhase )
                {
                    TryDeserializeFields( objRef.Serializer, ref objRef.Value, fields, item.Cause );
                }
            }
            else
            {
                // We have a primitive type.
            }
        }
    }

    private static void TryDeserializeFields( ISerializer serializer, ref object value, InstanceFields fields, SerializationCause? cause )
    {
        try
        {
            serializer.DeserializeFields( ref value, fields );
        }
        catch ( CompileTimeSerializationException exception )
        {
            throw CompileTimeSerializationException.CreateWithCause( $"Deserialization of fields for type '{value.GetType()}' failed.", cause, exception );
        }
    }

    private InstanceFields ReadInstanceFields( Type type, bool initializeObjects, SerializationCause? cause )
    {
        int fieldCount = this._binaryReader.ReadCompressedInteger();

        if ( fieldCount == 0 )
        {
            return this._emptyInstanceFields;
        }

        var fields = new InstanceFields( type, this._formatter, fieldCount );

        for ( var i = 0; i < fieldCount; i++ )
        {
            string fieldName = this._binaryReader.ReadDottedString();

            var newCause = cause?.WithFieldAccess( fieldName );
            var value = this.ReadTypedValue( initializeObjects, newCause );

            fields.Values!.Add( fieldName, value );
        }

        return fields;
    }

    private void ReadType( out Type? type, out SerializationIntrinsicType intrinsicType )
    {
        intrinsicType = (SerializationIntrinsicType) this._binaryReader.ReadByte();

        switch ( intrinsicType )
        {
            case SerializationIntrinsicType.None:
                type = null;

                break;

            case SerializationIntrinsicType.Byte:
                type = typeof(byte);

                break;

            case SerializationIntrinsicType.SByte:
                type = typeof(sbyte);

                break;

            case SerializationIntrinsicType.Int16:
                type = typeof(short);

                break;

            case SerializationIntrinsicType.Int32:
                type = typeof(int);

                break;

            case SerializationIntrinsicType.Int64:
                type = typeof(long);

                break;

            case SerializationIntrinsicType.UInt16:
                type = typeof(ushort);

                break;

            case SerializationIntrinsicType.UInt32:
                type = typeof(uint);

                break;

            case SerializationIntrinsicType.UInt64:
                type = typeof(ulong);

                break;

            case SerializationIntrinsicType.Single:
                type = typeof(float);

                break;

            case SerializationIntrinsicType.Double:
                type = typeof(double);

                break;

            case SerializationIntrinsicType.String:
                type = typeof(string);

                break;

            case SerializationIntrinsicType.DottedString:
                type = typeof(DottedString);

                break;

            case SerializationIntrinsicType.Boolean:
                type = typeof(bool);

                break;

            case SerializationIntrinsicType.Enum:
                type = this.ReadNamedType();

                // IsEnum throws on CompileTimeType. Similarly for the checks below.
                if ( type is not CompileTimeType && !type.IsEnum )
                {
                    throw new CompileTimeSerializationException(
                        string.Format( CultureInfo.InvariantCulture, "Type '{0}' is expected to be an enum type.", type ) );
                }

                break;

            case SerializationIntrinsicType.Struct:
                type = this.ReadNamedType();

                break;

            case SerializationIntrinsicType.Class:
                type = this.ReadNamedType();

                break;

            case SerializationIntrinsicType.Array:
                int rank = this._binaryReader.ReadCompressedInteger();
                this.ReadType( out var elementType, out _ );

                if ( elementType is CompileTimeType compileTimeType )
                {
                    var compilation = this._formatter.Compilation.AssertNotNull();
                    var elementTypeSymbol = (INamedTypeSymbol) compileTimeType.Target.GetSymbol( compilation ).AssertNotNull();

                    var arrayTypeSymbol = compilation.CreateArrayTypeSymbol( elementTypeSymbol, rank );

                    type = this._compileTimeTypeFactory.AssertNotNull().Get( arrayTypeSymbol );
                }
                else
                {
                    type = rank == 1 ? elementType.AssertNotNull().MakeArrayType() : elementType.AssertNotNull().MakeArrayType( rank );
                }

                break;

            case SerializationIntrinsicType.Char:
                type = typeof(char);

                break;

            case SerializationIntrinsicType.ObjRef:
                type = typeof(object);

                break;

            case SerializationIntrinsicType.Type:
                type = typeof(Type);

                break;

            case SerializationIntrinsicType.GenericTypeParameter:
                type = this.ReadGenericTypeParameter();

                break;

            default:
                throw new CompileTimeSerializationException( $"Invalid type: {intrinsicType}." );
        }
    }

    private Type ReadNamedType()
    {
        var flags = (SerializationIntrinsicTypeFlags) this._binaryReader.ReadByte();

        switch ( flags )
        {
            case SerializationIntrinsicTypeFlags.Default:
                {
                    var typeName = this.ReadTypeName();

                    return this.GetType( typeName );
                }

            case SerializationIntrinsicTypeFlags.Generic:
                {
                    var typeName = this.ReadTypeName();
                    var genericType = this.GetType( typeName );
                    int arity = this._binaryReader.ReadCompressedInteger();

                    if ( arity > 0 )
                    {
                        var genericArguments = new Type[arity];

                        for ( var i = 0; i < arity; i++ )
                        {
                            // Assertion on nullability was added after the code import from PostSharp.
                            genericArguments[i] = this.ReadType().AssertNotNull();
                        }

                        if ( genericType is CompileTimeType || genericArguments.OfType<CompileTimeType>().Any() )
                        {
                            var compilation = this._formatter.Compilation.AssertNotNull();
                            var mapper = compilation.GetCompilationContext().ReflectionMapper;

                            var genericTypeSymbol = (INamedTypeSymbol) mapper.GetTypeSymbol( genericType );

                            var constructedTypeSymbol = genericTypeSymbol.Construct( genericArguments.SelectAsArray( mapper.GetTypeSymbol ) );

                            return this._compileTimeTypeFactory.AssertNotNull().Get( constructedTypeSymbol );
                        }

                        return genericType.MakeGenericType( genericArguments );
                    }
                    else
                    {
                        return genericType;
                    }
                }

            default:
                throw new CompileTimeSerializationException( "Cannot decode named type: invalid flag." );
        }
    }

    private Type? ReadType()
    {
        this.ReadType( out var type, out _ );

        return type;
    }

    private object? ReadTypedValue( bool initializeObjects, SerializationCause? cause )
    {
        this.ReadType( out var type, out var intrinsicType );

        if ( type == null )
        {
            return null;
        }

        var value = this.ReadValue( intrinsicType, type, initializeObjects, cause );

        return value;
    }

    private object? ReadValue( SerializationIntrinsicType intrinsicType, Type type, bool initializeObject, SerializationCause? cause )
    {
        if ( intrinsicType == SerializationIntrinsicType.None )
        {
            intrinsicType = type.GetIntrinsicType();
        }

        object? value;

        switch ( intrinsicType )
        {
            case SerializationIntrinsicType.Byte:
                value = this._binaryReader.ReadByte();

                break;

            case SerializationIntrinsicType.SByte:
                value = this._binaryReader.ReadSByte();

                break;

            case SerializationIntrinsicType.Int16:
                value = (short) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.Int32:
                value = (int) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.Int64:
                value = (long) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.UInt16:
                value = (ushort) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.UInt32:
                value = (uint) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.UInt64:
                value = (ulong) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.Single:
                value = this._binaryReader.ReadSingle();

                break;

            case SerializationIntrinsicType.Double:
                value = this._binaryReader.ReadDouble();

                break;

            case SerializationIntrinsicType.String:
                value = this._binaryReader.ReadString();

                break;

            case SerializationIntrinsicType.DottedString:
                value = this._binaryReader.ReadDottedString();

                break;

            case SerializationIntrinsicType.Boolean:
                value = this._binaryReader.ReadByte() != 0;

                break;

            case SerializationIntrinsicType.Struct:
                value = this.ReadStruct( type, cause );

                break;

            case SerializationIntrinsicType.ObjRef:
                value = this.ReadObjRef( initializeObject, cause );

                break;

            case SerializationIntrinsicType.Char:
                value = (char) this._binaryReader.ReadCompressedInteger();

                break;

            case SerializationIntrinsicType.Type:
                value = this.ReadType();

                break;

            case SerializationIntrinsicType.Enum:
                var enumValue = this._binaryReader.ReadCompressedInteger();

                // explicit cast is needed due to check in Enum.ToObject (it throws if type is not numeric type)
                value = enumValue.IsNegative ? Enum.ToObject( type, (long) enumValue ) : Enum.ToObject( type, (ulong) enumValue );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(intrinsicType) );
        }

        return value;
    }

    private Type ReadGenericTypeParameter()
    {
        // Assertion on nullability was added after the code import from PostSharp.
        var declaringType = this.ReadType().AssertNotNull();
        int position = this._binaryReader.ReadCompressedInteger();

        return declaringType.GetGenericArguments()[position];
    }

    private Type GetType( AssemblyTypeName typeName )
    {
        var result = this._formatter.Binder.BindToType( typeName.TypeName, typeName.AssemblyName );

        if ( result == null )
        {
            var assemblySymbol = this._formatter.Compilation.AssertNotNull().GetAssembly( typeName.AssemblyName )
                                 ?? throw new InvalidOperationException( $"Could not locate assembly {typeName.AssemblyName} in compilation." );

            var typeSymbol = assemblySymbol.GetTypeByMetadataName( typeName.TypeName )
                             ?? throw new InvalidOperationException(
                                 $"Could not locate type {typeName.TypeName} in assembly {typeName.AssemblyName} in compilation." );

            result = this._compileTimeTypeFactory.AssertNotNull().Get( typeSymbol );
        }

        return result;
    }

    private void ReadArray( Array array, SerializationCause? cause )
    {
        var indices = new int[array.Rank];

        this.ReadArrayElements( array, array.GetType().GetElementType()!, indices, 0, cause );
    }

    private void ReadArrayElements( Array array, Type elementType, int[] indices, int currentDimension, SerializationCause? cause )
    {
        var length = array.GetLength( currentDimension );
        var lowerBound = array.GetLowerBound( currentDimension );

        if ( currentDimension + 1 < indices.Length )
        {
            for ( var i = lowerBound; i < lowerBound + length; i++ )
            {
                indices[currentDimension] = i;
                this.ReadArrayElements( array, elementType, indices, currentDimension + 1, cause );
            }
        }
        else
        {
            var elementIntrinsicType = elementType.GetIntrinsicType( true );

            for ( var i = lowerBound; i < lowerBound + length; i++ )
            {
                indices[currentDimension] = i;

                var newCause = cause?.WithArrayAccess( indices );

                if ( elementIntrinsicType.IsPrimitiveIntrinsic() )
                {
                    array.SetValue( this.ReadValue( elementIntrinsicType, elementType, false, newCause ), indices );
                }
                else
                {
                    array.SetValue( this.ReadTypedValue( false, newCause ), indices );
                }
            }
        }
    }

    private object? ReadObjRef( bool initializeObject, SerializationCause? cause )
    {
        int instanceId = this._binaryReader.ReadCompressedInteger();

        return this.ReadObject( instanceId, initializeObject, cause );
    }

    private ObjRef GetObjRef( int instanceId, SerializationCause? cause )
    {
        if ( this._referenceTypeInstances.TryGetValue( instanceId, out var item ) )
        {
            return item.Value.AssertNotNull();
        }

        // Create an uninitialized instance for this type.
        this.ReadType( out var type, out var intrinsicType );

        if ( cause == null && this._shouldReportExceptionCause )
        {
            // This is the root.
            // Assertion on nullability was added after the code import from PostSharp.
            cause = SerializationCause.Root( type.AssertNotNull() );
        }

        if ( type == null )
        {
            return ObjRef.Empty;
        }

        object value;
        ISerializer? serializer;

        if ( intrinsicType == SerializationIntrinsicType.Array )
        {
            var lengths = new int[type.GetArrayRank()];
            var lowerBounds = new int[type.GetArrayRank()];

            for ( var i = 0; i < lengths.Length; i++ )
            {
                lengths[i] = this._binaryReader.ReadCompressedInteger();
                lowerBounds[i] = this._binaryReader.ReadCompressedInteger();
            }

            value = Array.CreateInstance( type.GetElementType()!, lengths, lowerBounds );

            serializer = null;
        }
        else if ( intrinsicType is SerializationIntrinsicType.Class or SerializationIntrinsicType.Struct )
        {
            var fields = this.ReadInstanceFields( type, true, cause );
            serializer = this._formatter.SerializerProvider.GetSerializer( type, cause );

            value = TryCreateInstance( serializer, type, fields, cause );
        }
        else
        {
            value = this.ReadValue( intrinsicType, type, true, cause ).AssertNotNull();
            serializer = null;
        }

        var objRef = new ObjRef( value, serializer, intrinsicType );

        if ( !type.IsValueType )
        {
            this._referenceTypeInstances.Add( instanceId, new SerializationQueueItem<ObjRef>( objRef, cause ) );
        }
        else
        {
            // ValueTypes are always initialized
            objRef.IsInitialized = true;
        }

        return objRef;
    }

    private static object TryCreateInstance( ISerializer serializer, Type type, InstanceFields fields, SerializationCause? cause )
    {
        try
        {
            return serializer.CreateInstance( type, fields );
        }
        catch ( CompileTimeSerializationException exception )
        {
            throw CompileTimeSerializationException.CreateWithCause( $"Deserialization of type '{type}' failed.", cause, exception );
        }
    }

    private object ReadStruct( Type type, SerializationCause? cause )
    {
        var fields = this.ReadInstanceFields( type, true, cause );

        var serializer = this._formatter.SerializerProvider.GetSerializer( type, cause );

        var value = TryCreateInstance( serializer, type, fields, cause );

        TryDeserializeFields( serializer, ref value, fields, cause );

        return value;
    }

    private AssemblyTypeName ReadTypeName()
    {
        // Assertion on nullability was added after the code import from PostSharp.
        var typeName = this._binaryReader.ReadDottedString();
        var assemblyName = this._binaryReader.ReadString().AssertNotNull();

        return new AssemblyTypeName( typeName, assemblyName );
    }

    private sealed class InstanceFields : IArgumentsReader, ISerializationContext
    {
        private readonly Type? _type;

        public Dictionary<string, object?>? Values { get; }

        private readonly CompileTimeSerializer _formatter;
        private Dictionary<string, object?>? _contextProperties;

        public InstanceFields( CompileTimeSerializer formatter )
        {
            this._type = null;
            this._formatter = formatter;
            this.Values = null;
        }

        public InstanceFields( Type type, CompileTimeSerializer formatter, int capacity )
        {
            this._type = type;
            this._formatter = formatter;
            this.Values = new Dictionary<string, object?>( capacity, StringComparer.Ordinal );
        }

        public bool TryGetValue<T>( string name, [MaybeNullWhen( false )] out T value, string? scope = null )
        {
            if ( this.Values == null )
            {
                value = default;

                return false;
            }

            if ( scope != null )
            {
                name = scope + "." + name;
            }

            if ( !this.Values.TryGetValue( name, out var valueObj ) )
            {
                value = default;

                return false;
            }

            if ( valueObj == null )
            {
                value = default!;

                return true;
            }

            ISerializer? serializer = null;

            if ( !typeof(T).HasElementType )
            {
                this._formatter.SerializerProvider.TryGetSerializer( typeof(T), out serializer );
            }

            try
            {
                if ( serializer != null )
                {
                    value = (T) serializer.Convert( valueObj, typeof(T) );
                }
                else
                {
                    value = (T) valueObj;
                }

                return true;
            }
            catch ( Exception e )
            {
#if LEGACY_REFLECTION_API
                    Type GetElementType(Type type)
                    {
                        if (type.HasElementType)
                        {
                            return type.GetElementType();
                        }
                        else if (type.GetTypeDefinition() == typeof(Nullable<>))
                        {
                            return type.GetGenericArguments()[0];
                        }
                        else
                        {
                            return type;
                        }
                    }
#endif

                static string FormatTypeName( Type type )
                {
#if LEGACY_REFLECTION_API
                        return type.AssemblyQualifiedName + " (" + GetElementType(type).Assembly.Location + ")";
#else
                    return type.AssemblyQualifiedName ?? type.ToString();
#endif
                }

                throw new CompileTimeSerializationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Error reading value of key '{0}' in type '{1}': cannot convert type '{2}' into '{3}': {4}",
                        name,
                        this._type,
                        FormatTypeName( valueObj.GetType() ),
                        FormatTypeName( typeof(T) ),
                        e.Message ),
                    e );
            }
        }

        public T? GetValue<T>( string name, string? scope = null )
        {
            this.TryGetValue( name, out T? value, scope );

            return value;
        }

        public CompilationContext CompilationContext => this._formatter.CompilationContext;

        public Dictionary<string, object?> ContextProperties => this._contextProperties ??= new Dictionary<string, object?>( StringComparer.Ordinal );
    }

    private sealed class ObjRef
    {
        public static readonly ObjRef Empty = new();

#pragma warning disable SA1401 // Fields should be private
        public object? Value;
#pragma warning restore SA1401 // Fields should be private

        public SerializationIntrinsicType IntrinsicType { get; }

        public ISerializer? Serializer { get; }

        public bool IsInitialized { get; set; }

        private ObjRef()
        {
            this.IntrinsicType = SerializationIntrinsicType.None;
        }

        public ObjRef( object value, ISerializer? serializer, SerializationIntrinsicType intrinsicType )
        {
            this.Value = value;
            this.Serializer = serializer;
            this.IntrinsicType = intrinsicType;
            this.IsInitialized = false;
        }
    }
}