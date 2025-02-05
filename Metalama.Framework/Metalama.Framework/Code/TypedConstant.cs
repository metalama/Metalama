// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code.SyntaxBuilders;
using Metalama.Framework.Code.Types;
using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Represents a typed value that can be defined, defined to null, or undefined. Used to represent default values,
    /// for instance <see cref="IParameter.DefaultValue"/>, or attribute arguments.
    /// </summary>
    [CompileTime]
    public readonly struct TypedConstant : IExpression, IEquatable<TypedConstant>
    {
        // ReSharper disable once UnassignedReadonlyField

        private readonly object? _value;
        private readonly IType? _type;

        private void CheckInitialized()
        {
            if ( this._type == null )
            {
                throw new InvalidOperationException( $"The {nameof(TypedConstant)} is unassigned." );
            }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="Value"/> has been specified (including when it is set to <c>null</c>).
        /// </summary>
        public bool IsInitialized => this._type != null;

        /// <summary>
        /// Gets the type of the value. This is important if the type is an enum, because in this case, if the enum type is not compile-time,
        /// <see cref="Value"/> is set to the underlying integer value.
        /// </summary>
        public IType Type
        {
            get
            {
                this.CheckInitialized();

                return this._type!;
            }
        }

        RefKind IHasType.RefKind => RefKind.None;

        /// <summary>
        /// Gets a value indicating whether the value is <c>null</c> or <c>default</c>. Not to be confused with <see cref="IsInitialized"/>.
        /// </summary>
        public bool IsNullOrDefault
        {
            get
            {
                this.CheckInitialized();

                return this.Value == null;
            }
        }

        bool IExpression.IsAssignable => false;

        ref dynamic? IExpression.Value => ref RefHelper.Wrap( SyntaxBuilder.CurrentImplementation.TypedConstant( this ) );

        /// <summary>
        /// Gets the raw value of the <see cref="TypedConstant"/>. If <see cref="IsArray"/> is <c>true</c>, this
        /// property returns an <c>ImmutableArray&lt;TypedConstant&gt;</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For enum values, <see cref="Value"/> represents the underlying integer value and <see cref="Type"/> the type of the enum.
        /// </para>
        /// </remarks>
        public object? RawValue
        {
            get
            {
                this.CheckInitialized();

                return this._value;
            }
        }

        public bool IsArray => this.Type is { TypeKind: TypeKind.Array };

        public ImmutableArray<TypedConstant> Values
        {
            get
            {
                var values = this._value as ImmutableArray<TypedConstant>? ?? default;

                if ( !values.IsDefault )
                {
                    return values;
                }
                else if ( !this.IsArray )
                {
                    throw new InvalidOperationException( "The TypedConstant does not represent an array." );
                }
                else
                {
                    // We have a null array.
                    return default;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedConstant"/> struct that represents the fact that the value
        /// was set to something, even <c>null</c>. To represent the fact that the default value was not set, use <c>default(TypedConstant)</c>.
        /// </summary>
        /// <param name="value">The value (even <c>null</c>).</param>
        /// <param name="type">The type of the value.</param>
        private TypedConstant( object? value, IType type ) : this()
        {
            if ( value != null )
            {
                var valueType = value.GetType();

                if ( valueType.IsEnum )
                {
                    this._value = Convert.ChangeType( value, valueType.GetEnumUnderlyingType(), CultureInfo.InvariantCulture );
                }
                else if ( valueType.IsArray )
                {
                    var array = (Array) value;
                    var arrayBuilder = ImmutableArray.CreateBuilder<TypedConstant>( array.Length );

                    foreach ( var item in array )
                    {
                        arrayBuilder.Add( Create( item ) );
                    }

                    this._value = arrayBuilder.ToImmutable();
                }
                else
                {
                    this._value = value;
                }
            }
            else
            {
                this._value = null;
            }

            this._type = type;
        }

        internal static bool CheckAcceptableType( IType expectedType, object? value, bool throwOnError, IDeclarationFactory declarationFactory )
        {
            if ( value == null )
            {
                // A null value is always acceptable because it means the default value in case of value types.
                return true;
            }

            bool TypeMismatch( object? actualExpectedType = null )
            {
                actualExpectedType ??= expectedType;

                if ( throwOnError )
                {
                    throw new ArgumentException(
                        nameof(value),
                        $"The value should be of type '{actualExpectedType}' but is of type '{value.GetType()}'." );
                }
                else
                {
                    return false;
                }
            }

            bool UnsupportedType()
            {
                if ( throwOnError )
                {
                    throw new ArgumentException(
                        nameof(value),
                        $"The type '{expectedType}' is not supported in a TypedConstant." );
                }
                else
                {
                    return false;
                }
            }

            if ( expectedType.SpecialType == SpecialType.Object )
            {
                return UnsupportedType();
            }
            else if ( expectedType is INamedType { FullName: "System.Nullable", TypeArguments: [var wrappedType] } )
            {
                if ( !CheckAcceptableType( wrappedType, value, throwOnError: false, declarationFactory ) )
                {
                    return TypeMismatch();
                }
            }
            else if ( expectedType.SpecialType is
                     SpecialType.SByte
                     or SpecialType.Int16
                     or SpecialType.Int32
                     or SpecialType.Int64
                     or SpecialType.Byte
                     or SpecialType.UInt16
                     or SpecialType.UInt32
                     or SpecialType.UInt64
                     or SpecialType.String
                     or SpecialType.Char
                     or SpecialType.Double
                     or SpecialType.Single
                     or SpecialType.Boolean
                     or SpecialType.Decimal )
            {
                if ( value.GetType().Namespace != nameof(System) || value.GetType().Name != ((INamedType) expectedType).Name )
                {
                    return TypeMismatch();
                }
            }
            else if ( expectedType is IArrayType arrayType )
            {
                if ( arrayType.Rank != 1 )
                {
                    return UnsupportedType();
                }

                if ( value is Array array )
                {
                    if ( !arrayType.ElementType.Equals( declarationFactory.GetTypeByReflectionType( array.GetType().GetElementType()! ) ) )
                    {
                        return TypeMismatch( $"ImmutableArray<TypedConstant>' or '{expectedType}" );
                    }
                }
                else if ( value is not ImmutableArray<TypedConstant> immutableArray )
                {
                    return TypeMismatch( $"ImmutableArray<TypedConstant>' or '{expectedType}" );
                }
                else if ( arrayType.ElementType.SpecialType != SpecialType.Object )
                {
                    foreach ( var arrayItem in immutableArray )
                    {
                        if ( !CheckAcceptableType( arrayType.ElementType, arrayItem._value, throwOnError, declarationFactory ) )
                        {
                            return false;
                        }
                    }
                }
            }
            else if ( expectedType.TypeKind == TypeKind.Enum )
            {
                if ( !expectedType.Equals( declarationFactory.GetTypeByReflectionType( value.GetType() ) ) &&
                     !CheckAcceptableType( ((INamedType) expectedType).UnderlyingType, value, throwOnError: false, declarationFactory ) )
                {
                    return TypeMismatch( $"{expectedType}' or '{((INamedType) expectedType).UnderlyingType}" );
                }
            }
            else if ( expectedType is INamedType { FullName: "System.Type" } )
            {
                if ( value is not IType )
                {
                    return TypeMismatch( $"{typeof(IType)}' or '{typeof(Type)}" );
                }
            }
            else
            {
                return UnsupportedType();
            }

            return true;
        }

        public override string ToString()
        {
            if ( !this.IsInitialized )
            {
                return "<uninitialized>";
            }
            else if ( this._value == null )
            {
                return $"({this._type}) null";
            }
            else if ( this._value is ImmutableArray<TypedConstant> array )
            {
                return $"({this._type}) [" + string.Join( ", ", array ) + "]";
            }
            else
            {
                return $"({this._type}) {this._value}";
            }
        }

        private static IType GetIType( Type type ) => TypeFactory.GetType( FixRuntimeType( type ) );

        private static Type FixRuntimeType( Type type ) => type is not ICompileTimeType && typeof(Type).IsAssignableFrom( type ) ? typeof(Type) : type;

        public static TypedConstant Default( IType type ) => new( null, type );

        public static TypedConstant Default( Type type ) => new( null, GetIType( type ) );

        private static Type GetValueType( object? value )
            => value switch
            {
                null => typeof(object),
                IType => typeof(Type),
                ImmutableArray<TypedConstant> => typeof(object[]),
                _ => value.GetType()
            };

        public static TypedConstant Create( object? value ) => Create( value, GetValueType( value ) );

        public static TypedConstant Create( object? value, Type? type )
        {
            if ( type == null )
            {
                return Create( value, GetValueType( value ) );
            }
            else
            {
                return Create( value, GetIType( type ) );
            }
        }

        public static TypedConstant Create( object? value, IType? type )
        {
            type ??= GetIType( GetValueType( value ) );

            if ( value is Type reflectionType )
            {
                value = ((ICompilationInternal) type.Compilation).Factory.GetTypeByReflectionType( reflectionType );
            }

            CheckAcceptableType( type, value, true, ((ICompilationInternal) type.Compilation).Factory );

            return new TypedConstant( value, type );
        }

        public static TypedConstant CreateUnchecked( object? value, IType type ) => new( value, type );

        internal static TypedConstant UnwrapOrCreate( object? value, IType type )
            => value is TypedConstant typedConstant ? typedConstant : new TypedConstant( value, type );

        /// <summary>
        /// Gets the <see cref="Value"/> for non-array types. For array types, get an array of a primitive type (e.g. <c>int[]</c>)
        /// instead of an array of <see cref="TypedConstant"/>.
        /// </summary>
        public object? Value
        {
            get
            {
                this.CheckInitialized();

                if ( this._value == null )
                {
                    return null;
                }

                if ( !this.IsArray )
                {
                    return this._value;
                }
                else
                {
                    var elementType = ((IArrayType) this.Type).ElementType;

                    if ( elementType is { TypeKind: TypeKind.Enum } )
                    {
                        elementType = ((INamedType) elementType).UnderlyingType;
                    }

                    var array = (ImmutableArray<TypedConstant>) this._value!;

                    return elementType.SpecialType switch
                    {
                        SpecialType.Boolean => GetTypedArray<bool>( array ),
                        SpecialType.Byte => GetTypedArray<byte>( array ),
                        SpecialType.SByte => GetTypedArray<sbyte>( array ),
                        SpecialType.Int16 => GetTypedArray<short>( array ),
                        SpecialType.UInt16 => GetTypedArray<ushort>( array ),
                        SpecialType.Int32 => GetTypedArray<int>( array ),
                        SpecialType.UInt32 => GetTypedArray<uint>( array ),
                        SpecialType.Int64 => GetTypedArray<long>( array ),
                        SpecialType.UInt64 => GetTypedArray<ulong>( array ),
                        SpecialType.String => GetTypedArray<string>( array ),
                        SpecialType.Double => GetTypedArray<double>( array ),
                        SpecialType.Single => GetTypedArray<float>( array ),
                        _ => GetTypedArray<object>( array )
                    };

                    static T[] GetTypedArray<T>( ImmutableArray<TypedConstant> values )
                    {
                        return values.Select( x => (T) x.RawValue! ).ToArray();
                    }
                }
            }
        }

        public TypedConstant ForCompilation( ICompilation compilation )
        {
            if ( !this.IsInitialized )
            {
                return default;
            }
            else if ( this.Type.Compilation == compilation )
            {
                return this;
            }
            else
            {
                return this._value switch
                {
                    IType type => Create( type.ForCompilation( compilation ), this.Type.ForCompilation( compilation ) ),
                    ImmutableArray<TypedConstant> array => Create(
                        array.Select( i => i.ForCompilation( compilation ) ).ToImmutableArray(),
                        this.Type.ForCompilation( compilation ) ),
                    _ => Create( this._value, this.Type.ForCompilation( compilation ) )
                };
            }
        }

        public bool Equals( TypedConstant other )
        {
            if ( !this.IsInitialized && !other.IsInitialized )
            {
                return true;
            }
            else if ( !this.IsInitialized || !other.IsInitialized )
            {
                return false;
            }

            if ( !this._type!.Equals( other._type ) )
            {
                return false;
            }

            if ( this._value == null && other._value == null )
            {
                return true;
            }

            if ( this._value == null || other._value == null )
            {
                return false;
            }

            switch ( this._value )
            {
                case ImmutableArray<TypedConstant> valueArray:
                    if ( other._value is ImmutableArray<TypedConstant> otherValueArray )
                    {
                        if ( valueArray.Length != otherValueArray.Length )
                        {
                            return false;
                        }

                        for ( var i = 0; i < valueArray.Length; i++ )
                        {
                            if ( !valueArray[i].Equals( otherValueArray[i] ) )
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }

                case IType type:
                    return other._value is IType otherType && type.Equals( otherType );

                default:
                    return this._value.Equals( other._value );
            }
        }

        public override bool Equals( object? obj ) => obj is TypedConstant other && this.Equals( other );

        public override int GetHashCode()
        {
            if ( !this.IsInitialized )
            {
                return 0;
            }

            var hashCode = HashCode.Combine( this._type );

            switch ( this._value )
            {
                case null:
                    return hashCode;

                case ImmutableArray<TypedConstant> valueArray:
                    foreach ( var value in valueArray )
                    {
                        hashCode = HashCode.Combine( hashCode, value.GetHashCode() );
                    }

                    return hashCode;

                case IType type:
                    return type.GetHashCode();

                default:
                    return this._value.GetHashCode();
            }
        }

        public static bool operator ==( TypedConstant left, TypedConstant right ) => left.Equals( right );

        public static bool operator !=( TypedConstant left, TypedConstant right ) => !left.Equals( right );
    }
}