// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Reflection;

namespace Metalama.Framework.RunTime
{
    /// <summary>
    /// Represents a reflection <see cref="FieldInfo"/> or a <see cref="PropertyInfo"/>.
    /// </summary>
    /// <seealso cref="FieldInfo"/>
    /// <seealso cref="PropertyInfo"/>
    [PublicAPI]
    public class FieldOrPropertyInfo : MemberInfo
    {
        private readonly MemberInfo? _underlyingMemberInfo;

        /// <summary>
        /// Gets the underlying <see cref="MemberInfo"/> (either a <see cref="FieldInfo"/> or <see cref="PropertyInfo"/>).
        /// </summary>
        public MemberInfo UnderlyingMemberInfo
            => this._underlyingMemberInfo ?? throw new InvalidOperationException( "This object cannot be accessed at compile time." );

        /// <summary>
        /// Gets the <see cref="FieldInfo"/> if this represents a field, otherwise returns null.
        /// </summary>
        public FieldInfo? AsField => (FieldInfo?) this.UnderlyingMemberInfo;

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> if this represents a property, otherwise returns null.
        /// </summary>
        public PropertyInfo? AsPropertyOrIndexer => (PropertyInfo?) this.UnderlyingMemberInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldOrPropertyInfo"/> class that represents a field.
        /// </summary>
        /// <param name="fieldInfo">The field.</param>
        public FieldOrPropertyInfo( FieldInfo fieldInfo )
        {
            this._underlyingMemberInfo = fieldInfo;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldOrPropertyInfo"/> class that represents a property.
        /// </summary>
        /// <param name="propertyInfo">The property.</param>
        public FieldOrPropertyInfo( PropertyInfo propertyInfo )
        {
            this._underlyingMemberInfo = propertyInfo;
        }

        // Compile-time constructor.
        private protected FieldOrPropertyInfo() { }

        public override object[] GetCustomAttributes( bool inherit ) => this.UnderlyingMemberInfo.GetCustomAttributes( inherit );

        public override object[] GetCustomAttributes( Type attributeType, bool inherit )
            => this.UnderlyingMemberInfo.GetCustomAttributes( attributeType, inherit );

        public override bool IsDefined( Type attributeType, bool inherit ) => this.UnderlyingMemberInfo.IsDefined( attributeType, inherit );

        public override Type DeclaringType => this.UnderlyingMemberInfo.DeclaringType!;

        public override MemberTypes MemberType => this.UnderlyingMemberInfo.MemberType;

        public override string Name => this.UnderlyingMemberInfo.Name;

        public override Type ReflectedType => this.UnderlyingMemberInfo.ReflectedType!;

        /// <summary>
        /// Gets the type of the field or property value.
        /// </summary>
        public Type ValueType
            => this.UnderlyingMemberInfo switch
            {
                FieldInfo @field => @field.FieldType,
                PropertyInfo property => property.PropertyType,
                _ => throw new InvalidOperationException()
            };

        /// <summary>
        /// Gets the value of the field or property from the specified object.
        /// </summary>
        /// <param name="obj">The object from which to get the value.</param>
        /// <returns>The value of the field or property.</returns>
        public object? GetValue( object? obj )
            => this.UnderlyingMemberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.GetValue( obj ),
                PropertyInfo propertyInfo => propertyInfo.GetValue( obj ),
                _ => throw new InvalidOperationException()
            };

        /// <summary>
        /// Sets the value of the field or property on the specified object.
        /// </summary>
        /// <param name="obj">The object on which to set the value.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue( object? obj, object? value )
        {
            switch ( this.UnderlyingMemberInfo )
            {
                case FieldInfo fieldInfo:
                    fieldInfo.SetValue( obj, value );

                    break;

                case PropertyInfo propertyInfo:
                    propertyInfo.SetValue( obj, value );

                    break;

                default:
                    throw new InvalidOperationException();
            }
        }
    }
}