// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Factories;
using Metalama.Framework.Engine.ReflectionMocks;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Serialization;
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

    // Represents types that cannot be bound to a real, loadable Type in this process (e.g. a run-time type of the
    // writing process, incompatible across TFMs or assembly versions). Building a symbolic CompileTimeType from the
    // wire data directly, without a compilation, avoids resolving it to the wrong type.
    private readonly CompileTimeTypeFactory _symbolicTypeFactory = new();

    private readonly InstanceFields _emptyInstanceFields;

    internal SerializationReader(
        Stream stream,
        CompileTimeSerializer formatter,
        bool shouldReportExceptionCause,
        string assemblyName )
    {
        this._formatter = formatter;
        this._shouldReportExceptionCause = shouldReportExceptionCause;
        this._assemblyName = assemblyName;
        this._binaryReader = new SerializationBinaryReader( new BinaryReader( stream ) );
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
                    // The element type could not be bound to a real Type (see GetType), so neither can the array.
                    type = this.CreateSymbolicArrayType( compileTimeType, rank );
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
                            // The generic type definition, or one of its arguments, could not be bound to a real Type
                            // (see GetType), so neither can the constructed type.
                            return this.CreateSymbolicGenericType( genericType, genericArguments );
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
                // The value is itself a System.Type, which almost always captures a run-time type of the writing process.
                // Read it as a symbolic CompileTimeType instead of trying to resolve it to a real, loadable Type.
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
        // Binding is pure reflection and needs no compilation. The binder resolves the stored run-time assembly name
        // against *this* project's compile-time closure (CompileTimeProject.TryGetType), so it already yields the
        // consumer's own copy of a shared type, then falls back to the domain and to Type.GetType for system types.
        var result = this._formatter.Binder.BindToType( typeName.TypeName, typeName.AssemblyName );

        // The type is not in this project's closure and is not loadable here (e.g. a run-time type of the writing
        // process, incompatible across TFMs or assembly versions). Represent it symbolically. There is nothing a
        // compilation could add: resolving the name through one would only reach the same closure by a longer route,
        // and would otherwise yield a mock — which is what we build here directly.
        return result ?? this.CreateSymbolicType( typeName );
    }

    // Builds a CompileTimeType purely from the wire-encoded type signature (name, generic arguments, array rank),
    // without attempting to resolve a real Type and without needing any compilation.
    //
    // The SerializableTypeId is deliberately built WITHOUT the assembly name: it identifies a type *within a
    // compilation* by contract, and SerializableTypeIdResolver resolves it by parsing the part after the "Y:" prefix
    // as C# type syntax (`<id> M();`). Anything that is not valid C# type syntax -- an appended assembly name, a
    // reflection-style nested-type separator, a generic arity backtick -- makes the id unresolvable.
    private CompileTimeType CreateSymbolicType( AssemblyTypeName typeName )
    {
        var (ns, name) = SplitNamespaceAndName( typeName.TypeName );
        var metadata = new CompileTimeTypeMetadata( ns, name, typeName.TypeName, typeName.TypeName );

        return this._symbolicTypeFactory.Get( CreateTypeId( ToTypeSyntax( typeName.TypeName ) ), metadata );
    }

    private CompileTimeType CreateSymbolicArrayType( Type elementType, int rank )
    {
        var brackets = rank == 1 ? "[]" : "[" + new string( ',', rank - 1 ) + "]";
        var fullName = elementType.FullName + brackets;
        var metadata = new CompileTimeTypeMetadata( elementType.Namespace, elementType.Name + brackets, fullName, fullName );

        return this._symbolicTypeFactory.Get( CreateTypeId( ToTypeSyntax( elementType.FullName.AssertNotNull() ) + brackets ), metadata );
    }

    private CompileTimeType CreateSymbolicGenericType( Type genericTypeDefinition, Type[] genericArguments )
    {
        var fullName = genericTypeDefinition.FullName + "[" + string.Join( ",", genericArguments.SelectAsArray( a => a.FullName ) ) + "]";
        var (ns, name) = SplitNamespaceAndName( genericTypeDefinition.FullName.AssertNotNull() );
        var metadata = new CompileTimeTypeMetadata( ns, name, fullName, fullName );

        // C# syntax for a constructed generic type is `Ns.Foo<A, B>`, not the reflection form ``Ns.Foo`2[A,B]``. The arity
        // suffix is dropped here -- unlike in CreateSymbolicType -- because the type argument list already carries it.
        var syntax = StripArity( ToTypeSyntax( genericTypeDefinition.FullName.AssertNotNull() ) )
                     + "<" + string.Join( ", ", genericArguments.SelectAsArray( a => StripArity( ToTypeSyntax( a.FullName.AssertNotNull() ) ) ) ) + ">";

        return this._symbolicTypeFactory.Get( CreateTypeId( syntax ), metadata );
    }

    private static SerializableTypeId CreateTypeId( string typeSyntax ) => new( SerializableTypeId.Prefix + typeSyntax );

    /// <summary>
    /// Converts a reflection full name into C# type syntax, i.e. separates nested types by <c>.</c> rather than <c>+</c>.
    /// </summary>
    /// <remarks>
    /// The generic arity suffix (<c>`n</c>) is deliberately <em>kept</em>. It is not valid C# syntax, so an id retaining
    /// it cannot be resolved — but a symbolic type is by definition one we could not resolve anyway, and the arity is
    /// what distinguishes <c>Foo</c> from <c>Foo`1</c>. Dropping it would collide the two in the id-keyed
    /// <see cref="CompileTimeTypeFactory"/> cache, so one would be silently served for the other.
    /// </remarks>
    private static string ToTypeSyntax( string reflectionFullName ) => reflectionFullName.Replace( '+', '.' );

    /// <summary>
    /// Removes the generic arity suffix (<c>`n</c>) from each part of a name, e.g. <c>Ns.Foo`2.Bar`1</c> to
    /// <c>Ns.Foo.Bar</c>. Only valid where a type argument list supplies the arity instead.
    /// </summary>
    private static string StripArity( string name )
    {
        while ( true )
        {
            var backtick = name.IndexOfOrdinal( '`' );

            if ( backtick < 0 )
            {
                return name;
            }

            var end = backtick + 1;

            while ( end < name.Length && char.IsDigit( name[end] ) )
            {
                end++;
            }

            name = name.Remove( backtick, end - backtick );
        }
    }

    private static (string? Namespace, string Name) SplitNamespaceAndName( string reflectionFullName )
    {
        var firstNestedTypeSeparator = reflectionFullName.IndexOfOrdinal( '+' );
        var namespaceScope = firstNestedTypeSeparator >= 0 ? reflectionFullName[..firstNestedTypeSeparator] : reflectionFullName;
        var namespaceEnd = namespaceScope.LastIndexOf( '.' );
        var lastSeparator = reflectionFullName.LastIndexOfAny( ['.', '+'] );

        var ns = namespaceEnd >= 0 ? namespaceScope[..namespaceEnd] : null;
        var name = lastSeparator >= 0 ? reflectionFullName[(lastSeparator + 1)..] : reflectionFullName;

        return (ns, name);
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

        CompilationContext ISerializationContext.CompilationContext => throw new NotSupportedException();

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