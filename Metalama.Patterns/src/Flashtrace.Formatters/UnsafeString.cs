// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Utilities;
using System.Globalization;

namespace Flashtrace.Formatters;

// TODO: It seems this whole class should be refactored. It does too many things, some of those are now redundant with Span.
/// <summary>
/// A generalized representation of a string that can be either backed by a system <see cref="string"/>,
/// a <c>char[]</c>, or an <see cref="UnsafeStringBuilder"/>. Conversions between these types happen transparently
/// and are cached.
/// </summary>
/// <remarks>
/// <para>Because an <see cref="UnsafeString"/> can be backed by a <see cref="UnsafeStringBuilder"/>, which is a mutable
/// type, and is generally pooled and reused for different purposes, 
/// it is generally not safe to evaluate an <see cref="UnsafeString"/> at a different moment than the one designed by
/// the API that exposes the <see cref="UnsafeString"/>. To make it safe to evaluate the <see cref="UnsafeString"/> at any
/// moment, call the <see cref="MakeImmutable"/> method, which unbinds the <see cref="UnsafeString"/> from
/// its parent <see cref="UnsafeStringBuilder"/>.</para>
/// </remarks>
internal sealed class UnsafeString
{
    private int _version;
#pragma warning disable IDE0032 // Use auto property
    private char[]? _array;
#pragma warning restore IDE0032 // Use auto property
    private string? _str;

    internal UnsafeString( UnsafeStringBuilder stringBuilder )
    {
        this.StringBuilder = stringBuilder;
        this._version = stringBuilder.Version;
        this.Length = stringBuilder.Length;
    }

    // TODO: Remove if proven unused after further porting.
    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeString"/> class backed by a <see cref="string"/>.
    /// </summary>
    /// <param name="str">A non-null <see cref="string"/>.</param>
    public UnsafeString( string str )
    {
        this._str = str ?? throw new ArgumentNullException( nameof(str) );
    }

    // TODO: Remove if proven unused after further porting.
    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeString"/> class backed by an array of <see cref="char"/>.
    /// </summary>
    /// <param name="array">A non-null array of <see cref="char"/>.</param>
    public UnsafeString( char[] array )
    {
        this._array = array ?? throw new ArgumentNullException( nameof(array) );
    }

    // Was [ExplicitCrossPackageInternal]
    internal UnsafeStringBuilder? StringBuilder { get; private set; }

    private void ThrowIfImmutable()
    {
        if ( this.IsImmutable )
        {
            throw new InvalidOperationException( "The current " + nameof(UnsafeString) + " is immutable." );
        }
    }

    // TODO: Remove if proven unused after further porting.
    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// Gets an unmanaged pointer to the string.
    /// </summary>
    /// <exception cref="InvalidOperationException">The current <see cref="UnsafeString"/> is immutable.</exception>
    public IntPtr Buffer
    {
        get
        {
            this.ThrowIfImmutable();

            return this.StringBuilder!.Buffer;
        }
    }

    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
#pragma warning disable IDE0032 // Use auto property
    internal char[]? CharArray => this._array;
#pragma warning restore IDE0032 // Use auto property

    // TODO: Make private if proven unused after further porting.
    // ReSharper disable once MemberCanBePrivate.Global
    /// <summary>
    /// Gets a value indicating whether the current <see cref="UnsafeString"/> is immutable.
    /// Call the <see cref="MakeImmutable"/> method to make the <see cref="UnsafeString"/> immutable.
    /// </summary>
    public bool IsImmutable => this.StringBuilder == null;

    /// <summary>
    /// Gets the number of characters in the string.
    /// </summary>
    public int Length { get; private set; }

    internal bool Recycle()
    {
        if ( this.IsImmutable )
        {
            // The current object can no longer be changed.
            return false;
        }

        if ( this._version != this.StringBuilder!.Version || this.Length != this.StringBuilder.Length )
        {
            // The current object is not shared but we need to reset the cached value.
            this._array = null;
            this._str = null;
            this.Length = this.StringBuilder.Length;
            this._version = this.StringBuilder.Version;
        }

        return true;
    }

    // TODO: Remove if proven unused after further porting.
    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// If the current <see cref="UnsafeString"/> is bound to its origin <see cref="UnsafeStringBuilder"/>,
    /// evaluates the <see cref="UnsafeStringBuilder"/> and breaks the binding, so that later changes in the 
    /// <see cref="UnsafeStringBuilder"/> do not cause changes in the current <see cref="UnsafeString"/>.
    /// This method also prevents the <see cref="UnsafeString"/> form being recycled.
    /// </summary>
    public unsafe void MakeImmutable()
    {
        if ( this.IsImmutable )
        {
            return;
        }

        this.CheckVersion();

        if ( this._array == null && this._str == null )
        {
            this._array = new char[this.Length];

            fixed ( char* pDestination = this._array )
            {
                BufferHelper.CopyMemory( pDestination, (void*) this.StringBuilder!.Buffer, this.Length * sizeof(char) );
            }
        }

        this.StringBuilder = null;
    }

    // TODO: Remove if proven unused after further porting.
    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// Gets an <see cref="ArraySegment{Char}"/> representing the current <see cref="UnsafeString"/>.
    /// </summary>
    /// <returns>An <see cref="ArraySegment{Char}"/> representing the current <see cref="UnsafeString"/>.</returns>
    public ArraySegment<char> ToCharArray()
    {
        if ( this.StringBuilder != null )
        {
            this.CheckVersion();

            if ( this.StringBuilder.CharArray != null )
            {
                return new ArraySegment<char>( this.StringBuilder.CharArray, 0, this.StringBuilder.Length );
            }
            else
            {
                // There is no backing managed array, so we need to allocate managed memory anyway.
                this._str = this.StringBuilder.ToStringImpl();
                this._array = this._str.ToCharArray();

                return new ArraySegment<char>( this._array );
            }
        }
        else if ( this._array != null )
        {
            return new ArraySegment<char>( this._array );
        }
        else if ( this._str != null )
        {
            this._array = this._str.ToCharArray();

            return new ArraySegment<char>( this._array );
        }
        else
        {
            return default;
        }
    }

    private void CheckVersion()
    {
        if ( this.StringBuilder != null && (this.StringBuilder.Version != this._version || this.StringBuilder.Length != this.Length) )
        {
            throw new InvalidOperationException( string.Format( CultureInfo.InvariantCulture, "The {0} has changed.", nameof(UnsafeStringBuilder) ) );
        }
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        // We need to take a copy of the buffer anyway, so we also create
        if ( this.StringBuilder != null )
        {
            this.CheckVersion();
            this._str = this.StringBuilder.ToStringImpl();
        }
        else if ( this._array != null )
        {
            this._str = new string( this._array );
        }

        return this._str;
    }
}