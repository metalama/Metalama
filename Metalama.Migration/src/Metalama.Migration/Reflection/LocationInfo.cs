// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.RunTime;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace PostSharp.Reflection
{
    /// <summary>
    /// In Metalama, use <see cref="FieldOrPropertyInfo"/>.
    /// </summary>
    [PublicAPI]
    public sealed class LocationInfo : IEquatable<LocationInfo>
                                     , ISerializable

    {
        public LocationInfo( FieldInfo fieldInfo )
        {
            throw new NotImplementedException();
        }

        public LocationInfo( PropertyInfo propertyInfo )
        {
            throw new NotImplementedException();
        }

        public LocationInfo( ParameterInfo parameterInfo )
        {
            throw new NotImplementedException();
        }

        public static LocationInfo ToLocationInfo( object reflectionInfo )
        {
            throw new NotImplementedException();
        }

        public static LocationInfo[] ToLocationInfoArray( ICollection<FieldInfo> fields )
        {
            throw new NotImplementedException();
        }

        public static LocationInfo[] ToLocationInfoArray( ICollection<PropertyInfo> properties )
        {
            throw new NotImplementedException();
        }

        public static LocationInfo[] ToLocationInfoArray( ICollection<ParameterInfo> parameters )
        {
            throw new NotImplementedException();
        }

        public Type LocationType { get; }

        public LocationKind LocationKind { get; }

        public PropertyInfo PropertyInfo { get; }

        public FieldInfo FieldInfo { get; }

        public ParameterInfo ParameterInfo { get; }

        public MemberInfo MemberInfo { get; }

        public Type DeclaringType => throw new NotImplementedException();

        public string Name => throw new NotImplementedException();

        public bool IsStatic => throw new NotImplementedException();

        public object GetValue( object instance )
        {
            throw new NotImplementedException();
        }

        public object GetValue( object instance, object[] index )
        {
            throw new NotImplementedException();
        }

        public void SetValue( object instance, object value )
        {
            throw new NotImplementedException();
        }

        public void SetValue( object instance, object value, object[] index )
        {
            throw new NotImplementedException();
        }

        public bool Equals( LocationInfo other )
        {
            throw new NotImplementedException();
        }

        public static bool operator ==( LocationInfo left, LocationInfo right )
        {
            throw new NotImplementedException();
        }

        public static bool operator !=( LocationInfo left, LocationInfo right )
        {
            throw new NotImplementedException();
        }

        public void GetObjectData( SerializationInfo info, StreamingContext context )
        {
            throw new NotImplementedException();
        }
    }
}