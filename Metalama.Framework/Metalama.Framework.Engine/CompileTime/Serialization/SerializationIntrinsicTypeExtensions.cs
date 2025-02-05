// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.CompileTime.Serialization
{
    internal static class SerializationIntrinsicTypeExtensions
    {
        public static bool IsPrimitiveIntrinsic( this SerializationIntrinsicType intrinsicType )
        {
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
                case SerializationIntrinsicType.DottedString:
                case SerializationIntrinsicType.Char:
                case SerializationIntrinsicType.SByte:
                case SerializationIntrinsicType.Struct:
                case SerializationIntrinsicType.Enum:
                case SerializationIntrinsicType.Boolean:
                    return true;

                case SerializationIntrinsicType.String:
                case SerializationIntrinsicType.Class:
                case SerializationIntrinsicType.Array:
                case SerializationIntrinsicType.ObjRef:
                case SerializationIntrinsicType.Type:
                case SerializationIntrinsicType.GenericTypeParameter:
                    return false;

                default:
                    throw new ArgumentOutOfRangeException( nameof(intrinsicType) );
            }
        }

        public static SerializationIntrinsicType GetIntrinsicType( this Type? type, bool useObjRef = false )
        {
            if ( type == null )
            {
                return SerializationIntrinsicType.None;
            }
            else if ( type.IsEnum )
            {
                return SerializationIntrinsicType.Enum;
            }

            switch ( Type.GetTypeCode( type ) )
            {
                case TypeCode.Boolean:
                    return SerializationIntrinsicType.Boolean;

                case TypeCode.Char:
                    return SerializationIntrinsicType.Char;

                case TypeCode.SByte:
                    return SerializationIntrinsicType.SByte;

                case TypeCode.Byte:
                    return SerializationIntrinsicType.Byte;

                case TypeCode.Int16:
                    return SerializationIntrinsicType.Int16;

                case TypeCode.UInt16:
                    return SerializationIntrinsicType.UInt16;

                case TypeCode.Int32:
                    return SerializationIntrinsicType.Int32;

                case TypeCode.UInt32:
                    return SerializationIntrinsicType.UInt32;

                case TypeCode.Int64:
                    return SerializationIntrinsicType.Int64;

                case TypeCode.UInt64:
                    return SerializationIntrinsicType.UInt64;

                case TypeCode.Single:
                    return SerializationIntrinsicType.Single;

                case TypeCode.Double:
                    return SerializationIntrinsicType.Double;

                case TypeCode.Decimal:
                    return SerializationIntrinsicType.Struct;

                case TypeCode.DateTime:
                    return SerializationIntrinsicType.Struct;

                case TypeCode.String:
                    return SerializationIntrinsicType.String;

                case TypeCode.Object:
                    if ( type.IsGenericParameter )
                    {
                        if ( type.DeclaringMethod != null )
                        {
                            // Not supported.
                            throw new AssertionFailedException( $"Cannot map {type} because it is a generic parameter of a method ({type.DeclaringMethod})." );
                        }
                        else
                        {
                            return SerializationIntrinsicType.GenericTypeParameter;
                        }
                    }
                    else if ( type.IsValueType )
                    {
                        if ( type == typeof(DottedString) )
                        {
                            return SerializationIntrinsicType.DottedString;
                        }
                        else
                        {
                            return SerializationIntrinsicType.Struct;
                        }
                    }
                    else
                    {
                        if ( typeof(Type).IsAssignableFrom( type ) )
                        {
                            return SerializationIntrinsicType.Type;
                        }
                        else
                        {
                            if ( useObjRef )
                            {
                                return SerializationIntrinsicType.ObjRef;
                            }
                            else if ( type.IsArray )
                            {
                                return SerializationIntrinsicType.Array;
                            }
                            else
                            {
                                return SerializationIntrinsicType.Class;
                            }
                        }
                    }

                case TypeCode.DBNull:
                    return SerializationIntrinsicType.Struct;

                default:
                    throw new ArgumentOutOfRangeException( nameof(type) );
            }
        }

        public static SerializationIntrinsicType GetIntrinsicType( this ITypeSymbol? typeSymbol, bool useObjRef = false )
        {
            if ( typeSymbol == null )
            {
                return SerializationIntrinsicType.None;
            }
            else if ( typeSymbol.TypeKind == TypeKind.Enum )
            {
                return SerializationIntrinsicType.Enum;
            }

            switch ( typeSymbol.SpecialType )
            {
                case SpecialType.System_Boolean:
                    return SerializationIntrinsicType.Boolean;

                case SpecialType.System_Char:
                    return SerializationIntrinsicType.Char;

                case SpecialType.System_SByte:
                    return SerializationIntrinsicType.SByte;

                case SpecialType.System_Byte:
                    return SerializationIntrinsicType.Byte;

                case SpecialType.System_Int16:
                    return SerializationIntrinsicType.Int16;

                case SpecialType.System_UInt16:
                    return SerializationIntrinsicType.UInt16;

                case SpecialType.System_Int32:
                    return SerializationIntrinsicType.Int32;

                case SpecialType.System_UInt32:
                    return SerializationIntrinsicType.UInt32;

                case SpecialType.System_Int64:
                    return SerializationIntrinsicType.Int64;

                case SpecialType.System_UInt64:
                    return SerializationIntrinsicType.UInt64;

                case SpecialType.System_Single:
                    return SerializationIntrinsicType.Single;

                case SpecialType.System_Double:
                    return SerializationIntrinsicType.Double;

                case SpecialType.System_String:
                    return SerializationIntrinsicType.String;

                default:
                    if ( typeSymbol.TypeKind == TypeKind.TypeParameter )
                    {
                        if ( typeSymbol.ContainingSymbol is IMethodSymbol )
                        {
                            // Not supported.
                            throw new AssertionFailedException(
                                $"Cannot map {typeSymbol} because it is a generic parameter of a method ({typeSymbol.ContainingSymbol})." );
                        }
                        else
                        {
                            return SerializationIntrinsicType.GenericTypeParameter;
                        }
                    }
                    else if ( typeSymbol.IsValueType )
                    {
                        if ( typeSymbol.GetReflectionName() == typeof(DottedString).FullName )
                        {
                            return SerializationIntrinsicType.DottedString;
                        }
                        else
                        {
                            return SerializationIntrinsicType.Struct;
                        }
                    }
                    else
                    {
                        if ( typeSymbol.GetReflectionName() == typeof(Type).FullName )
                        {
                            return SerializationIntrinsicType.Type;
                        }
                        else
                        {
                            if ( useObjRef )
                            {
                                return SerializationIntrinsicType.ObjRef;
                            }
                            else if ( typeSymbol.TypeKind == TypeKind.Array )
                            {
                                return SerializationIntrinsicType.Array;
                            }
                            else
                            {
                                return SerializationIntrinsicType.Class;
                            }
                        }
                    }
            }
        }
    }
}