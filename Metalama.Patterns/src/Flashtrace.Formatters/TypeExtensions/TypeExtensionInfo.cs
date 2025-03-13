// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Flashtrace.Formatters.TypeExtensions;

public readonly struct TypeExtensionInfo<T> : IEquatable<TypeExtensionInfo<T>>
    where T : class
{
    internal TypeExtensionInfo( T? extension, Type objectType, bool isGeneric )
    {
        this.Extension = extension;
        this.ObjectType = objectType ?? throw new ArgumentNullException( nameof(objectType) );
        this.IsGeneric = isGeneric;
    }

    public T? Extension { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public Type ObjectType { get; }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsGeneric { get; }

    internal bool ShouldOverwrite( TypeExtensionInfo<T> typeExtension )
        => CovariantTypeExtensionFactory<T>.ShouldOverwrite( this.ObjectType, this.IsGeneric, typeExtension.ObjectType, this.IsGeneric );

    public bool Equals( TypeExtensionInfo<T> other ) => EqualityComparer<T?>.Default.Equals( this.Extension, other.Extension ) && this.ObjectType == other.ObjectType && this.IsGeneric == other.IsGeneric;

    public override bool Equals( object? obj ) => obj is TypeExtensionInfo<T> other && this.Equals( other );

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = this.Extension == null ? 0 : EqualityComparer<T>.Default.GetHashCode( this.Extension );
            hashCode = (hashCode * 397) ^ this.ObjectType.GetHashCode();
            hashCode = (hashCode * 397) ^ this.IsGeneric.GetHashCode();

            return hashCode;
        }
    }
}