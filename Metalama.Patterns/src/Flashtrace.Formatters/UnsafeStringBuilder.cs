// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters.Utilities;
using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Flashtrace.Formatters;

/// <summary>
/// A class similar to <see cref="System.Text.StringBuilder"/>, but implemented using unsafe C#.
/// </summary>
[PublicAPI]
[SuppressMessage(
    "Style",
    "IDE0047:Remove unnecessary parentheses",
    Justification = "If the parentheses are removed, these warnings are replaced with 'Arithmetic expressions should declare precedence' warnings instead." )]
#pragma warning disable IDE0079 // Remove unnecessary suppression
[SuppressMessage(
    "ReSharper",
    "ArrangeRedundantParentheses",
    Justification = "If the parentheses are removed, these warnings are replaced with 'Arithmetic expressions should declare precedence' warnings instead." )]
#pragma warning restore IDE0079 // Remove unnecessary suppression
public sealed unsafe class UnsafeStringBuilder : IDisposable
{
#pragma warning disable IDE0032
    private char[]? _array;
#pragma warning restore IDE0032
    private char* _cursor;
    private char* _start;
    private char* _end;
    private GCHandle _gcHandle;
    private UnsafeString _unsafeString;

    /// <summary>
    /// Gets a value indicating whether  an <see cref="OverflowException"/> should be thrown when
    /// the buffer capacity is insufficient. If <c>false</c>, the <c>Append</c> method should return <c>false</c> without exception.
    /// </summary>
    public bool ThrowOnOverflow { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeStringBuilder"/> class and allocates a new buffer.
    /// </summary>
    /// <param name="capacity">The capacity of the new <see cref="UnsafeStringBuilder"/>.</param>
    /// <param name="throwOnOverflow"><c>true</c> if an <see cref="OverflowException"/> should be thrown when
    /// the buffer capacity is insufficient, <c>false</c> if the <c>Append</c> method should return <c>false</c> without exception.</param>
    public UnsafeStringBuilder( int capacity = 2048, bool throwOnOverflow = true )
    {
        this.ThrowOnOverflow = throwOnOverflow;
        this._array = new char[capacity];
        this._gcHandle = GCHandle.Alloc( this.CharArray, GCHandleType.Pinned );
        this._cursor = this._start = (char*) this._gcHandle.AddrOfPinnedObject();
        this._end = this._start + capacity;
        this._unsafeString = new UnsafeString( this );
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnsafeStringBuilder"/> class with a pre-allocated buffer.
    /// </summary>
    /// <param name="buffer">Pointer to the buffer.</param>
    /// <param name="size">Number of <c>char</c> in the buffer.</param>
    /// <param name="throwOnOverflow"><c>true</c> if an <see cref="OverflowException"/> should be thrown when
    /// the buffer capacity is insufficient, <c>false</c> if the <c>Append</c> method should return <c>false</c> without exception.</param>
    public UnsafeStringBuilder( char* buffer, int size, bool throwOnOverflow = true )
    {
        this.ThrowOnOverflow = throwOnOverflow;
        this._array = null;
        this._start = buffer;
        this._cursor = buffer;
        this._end = this._start + size;
        this._unsafeString = new UnsafeString( this );
        GC.SuppressFinalize( this );
    }

    /// <summary>
    /// Gets the capacity (number of <c>char</c>) of the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    public int Capacity => (int) (this._end - this._start);

    [MethodImpl( MethodImplOptions.NoInlining )]
    private bool OnOverflow()
    {
        if ( this.ThrowOnOverflow )
        {
            throw new InvalidOperationException( "Cannot append to the UnsafeStringBuilder because it has insufficient capacity." );
        }

        return false;
    }

    /// <summary>
    /// Appends one <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c">A <c>char</c>.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char c )
    {
        if ( this._cursor + 1 > this._end )
        {
            return this.OnOverflow();
        }

        *this._cursor = c;
        this._cursor++;

        return true;
    }

    /// <summary>
    /// Appends two <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c1">The first <c>char</c> to append.</param>
    /// <param name="c2">The second <c>char</c> to append.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char c1, char c2 )
    {
        if ( this._cursor + 2 > this._end )
        {
            return this.OnOverflow();
        }

        *this._cursor = c1;
        *(this._cursor + 1) = c2;
        this._cursor += 2;

        return true;
    }

    /// <summary>
    /// Appends three <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c1">The first <c>char</c> to append.</param>
    /// <param name="c2">The second <c>char</c> to append.</param>
    /// <param name="c3">The third <c>char</c> to append.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char c1, char c2, char c3 )
    {
        if ( this._cursor + 3 > this._end )
        {
            return this.OnOverflow();
        }

        *this._cursor = c1;
        *(this._cursor + 1) = c2;
        *(this._cursor + 2) = c3;
        this._cursor += 3;

        return true;
    }

    /// <summary>
    /// Appends four <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c1">The first <c>char</c> to append.</param>
    /// <param name="c2">The second <c>char</c> to append.</param>
    /// <param name="c3">The third <c>char</c> to append.</param>
    /// <param name="c4">The fourth <c>char</c> to append.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char c1, char c2, char c3, char c4 )
    {
        if ( this._cursor + 4 > this._end )
        {
            return this.OnOverflow();
        }

        *this._cursor = c1;
        *(this._cursor + 1) = c2;
        *(this._cursor + 2) = c3;
        *(this._cursor + 3) = c4;
        this._cursor += 4;

        return true;
    }

    /// <summary>
    /// Appends five <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c1">The first <c>char</c> to append.</param>
    /// <param name="c2">The second <c>char</c> to append.</param>
    /// <param name="c3">The third <c>char</c> to append.</param>
    /// <param name="c4">The fourth <c>char</c> to append.</param>
    /// <param name="c5">The fifth <c>char</c> to append.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char c1, char c2, char c3, char c4, char c5 )
    {
        if ( this._cursor + 5 > this._end )
        {
            return this.OnOverflow();
        }

        *this._cursor = c1;
        *(this._cursor + 1) = c2;
        *(this._cursor + 2) = c3;
        *(this._cursor + 3) = c4;
        *(this._cursor + 4) = c5;
        this._cursor += 5;

        return true;
    }

    /// <summary>
    /// Appends an array segment of <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c">A non-null array of <c>char</c>.</param>
    /// <param name="offset">Index of the first <c>char</c> to be appended.</param>
    /// <param name="count">Number of <c>char</c> to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char[]? c, int offset, int count )
    {
        if ( c == null || c.Length == 0 )
        {
            return true;
        }

        if ( offset + count > c.Length )
        {
            throw new ArgumentOutOfRangeException( nameof(c) );
        }

        if ( this._cursor + count > this._end )
        {
            return this.OnOverflow();
        }

        fixed ( char* theirArray = c )
        {
            BufferHelper.CopyMemory( this._cursor, theirArray + offset, count * sizeof(char) );
        }

        this._cursor += count;

        return true;
    }

    /// <summary>
    /// Appends an unmanaged array of <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c">A non-null pointer to an unmanaged array of <c>char</c>.</param>
    /// <param name="count">Number of <c>char</c> to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char* c, int count )
    {
        if ( c == null || count == 0 )
        {
            return true;
        }

        if ( this._cursor + count > this._end )
        {
            return this.OnOverflow();
        }

        BufferHelper.CopyMemory( this._cursor, c, count * sizeof(char) );
        this._cursor += count;

        return true;
    }

    /// <summary>
    /// Appends several times the same <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c">A <c>char</c>.</param>
    /// <param name="count">The number of times <paramref name="c"/> has to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char c, int count )
    {
        if ( this._cursor + count > this._end )
        {
            return this.OnOverflow();
        }

        for ( var i = 0; i < count / 4; i++ )
        {
            *(this._cursor + 0) = c;
            *(this._cursor + 1) = c;
            *(this._cursor + 2) = c;
            *(this._cursor + 3) = c;
            this._cursor += 4;
        }

        switch ( count % 4 )
        {
            case 0:
                break;

            case 1:
                *(this._cursor + 0) = c;
                this._cursor += 1;

                break;

            case 2:
                *(this._cursor + 0) = c;
                *(this._cursor + 1) = c;
                this._cursor += 2;

                break;

            case 3:
                *(this._cursor + 0) = c;
                *(this._cursor + 1) = c;
                *(this._cursor + 2) = c;
                this._cursor += 3;

                break;
        }

        return true;
    }

    /// <summary>
    /// Appends an array of <c>char</c> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="c">An array of <c>char</c>.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( char[]? c )
    {
        if ( c == null || c.Length == 0 )
        {
            return true;
        }

        var count = c.Length;

        if ( this._cursor + count > this._end )
        {
            return this.OnOverflow();
        }

        fixed ( char* theirArray = c )
        {
            BufferHelper.CopyMemory( this._cursor, theirArray, count * sizeof(char) );
        }

        this._cursor += count;

        return true;
    }

    public bool Append( ReadOnlySpan<char> c )
    {
        if ( c.Length == 0 )
        {
            return true;
        }

        var count = c.Length;

        if ( this._cursor + count > this._end )
        {
            return this.OnOverflow();
        }

        fixed ( char* theirArray = c )
        {
            BufferHelper.CopyMemory( this._cursor, theirArray, count * sizeof(char) );
        }

        this._cursor += count;

        return true;
    }

    /// <summary>
    /// Appends a <see cref="string"/> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="str">A <see cref="string"/>.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( string? str )
    {
        if ( str == null )
        {
            return true;
        }

        return this.Append( str, 0, str.Length );
    }

    /// <summary>
    /// Appends a part of a <see cref="string"/> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="str">A <see cref="string"/>.</param>
    /// <param name="startIndex">The index of the first character of the string to append.</param>
    /// <param name="length">The number of characters to append.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( string? str, int startIndex, int length )
    {
        if ( str == null || length == 0 )
        {
            return true;
        }

        if ( this._cursor + length > this._end )
        {
            return this.OnOverflow();
        }

        fixed ( char* theirArray = str )
        {
            BufferHelper.CopyMemory( this._cursor, theirArray + startIndex, length * sizeof(char) );
        }

        this._cursor += length;

        return true;
    }

    /// <summary>
    /// Appends an <see cref="UnsafeString"/> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="s">An <see cref="UnsafeString"/>.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    internal bool Append( UnsafeString? s )
    {
        if ( s == null || s.Length == 0 )
        {
            return true;
        }

        if ( s.StringBuilder != null )
        {
            return this.Append( s.StringBuilder );
        }
        else
        {
            var count = s.Length;

            if ( this._cursor + count > this._end )
            {
                return this.OnOverflow();
            }

            if ( s.CharArray != null )
            {
                fixed ( char* source = s.CharArray )
                {
                    BufferHelper.CopyMemory( this._cursor, source, count * sizeof(char) );
                }
            }
            else
            {
                fixed ( char* source = s.ToString() )
                {
                    BufferHelper.CopyMemory( this._cursor, source, count * sizeof(char) );
                }
            }

            this._cursor += count;
        }

        return true;
    }

    /// <summary>
    /// Appends the current value of a <see cref="UnsafeStringBuilder"/> to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">An <see cref="UnsafeStringBuilder"/>.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( UnsafeStringBuilder? stringBuilder )
    {
        if ( stringBuilder == null || stringBuilder.Length == 0 )
        {
            return true;
        }

        var count = stringBuilder.Length;

        if ( this._cursor + count > this._end )
        {
            return this.OnOverflow();
        }

        BufferHelper.CopyMemory( this._cursor, stringBuilder._start, count * sizeof(char) );

        this._cursor += count;

        return true;
    }

    /// <summary>
    /// Appends a <see cref="byte"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( byte value )
    {
        if ( this._cursor + 3 > this._end )
        {
            return this.OnOverflow();
        }

        this.AppendByte( value );

        return true;
    }

    /// <summary>
    /// Appends an <see cref="sbyte"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( sbyte value )
    {
        if ( this._cursor + 4 > this._end )
        {
            return this.OnOverflow();
        }

        unchecked
        {
            if ( value >= 0 )
            {
                this.AppendByte( (byte) value );
            }
            else
            {
                *this._cursor = '-';
                this._cursor++;
                this.AppendByte( (byte) -value );
            }
        }

        return true;
    }

    private void AppendByte( byte value )
    {
        unchecked
        {
            if ( value >= 100 )
            {
                *(this._cursor + 0) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 1) % 10) + '0');

                this._cursor += 3;
            }
            else if ( value >= 10 )
            {
                *(this._cursor + 0) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 1) % 10) + '0');

                this._cursor += 2;
            }
            else
            {
                *(this._cursor + 0) = (char) (((value / 1) % 10) + '0');

                this._cursor += 1;
            }
        }
    }

    /// <summary>
    /// Appends a <see cref="ushort"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( ushort value )
    {
        if ( this._cursor + 5 > this._end )
        {
            return this.OnOverflow();
        }

        this.AppendUInt16( value );

        return true;
    }

    /// <summary>
    /// Appends a <see cref="short"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( short value )
    {
        if ( this._cursor + 6 > this._end )
        {
            return this.OnOverflow();
        }

        unchecked
        {
            if ( value >= 0 )
            {
                this.AppendUInt16( (ushort) value );
            }
            else
            {
                *this._cursor = '-';
                this._cursor++;
                this.AppendUInt16( (ushort) -value );
            }
        }

        return true;
    }

    private void AppendUInt16( ushort value )
    {
        unchecked
        {
            if ( value >= 10000 )
            {
                *(this._cursor + 0) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 1) % 10) + '0');

                this._cursor += 5;
            }
            else if ( value >= 1000 )
            {
                *(this._cursor + 0) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 1) % 10) + '0');

                this._cursor += 4;
            }
            else if ( value >= 100 )
            {
                *(this._cursor + 0) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 1) % 10) + '0');

                this._cursor += 3;
            }
            else if ( value >= 10 )
            {
                *(this._cursor + 0) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 1) % 10) + '0');

                this._cursor += 2;
            }
            else
            {
                *(this._cursor + 0) = (char) (((value / 1) % 10) + '0');

                this._cursor += 1;
            }
        }
    }

    /// <summary>
    /// Appends a <see cref="uint"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( uint value )
    {
        if ( this._cursor + 10 > this._end )
        {
            return this.OnOverflow();
        }

        this.AppendUInt32( value );

        return true;
    }

    /// <summary>
    /// Appends a <see cref="int"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( int value )
    {
        if ( this._cursor + 11 > this._end )
        {
            return this.OnOverflow();
        }

        unchecked
        {
            if ( value >= 0 )
            {
                this.AppendUInt32( (uint) value );
            }
            else
            {
                *this._cursor = '-';
                this._cursor++;
                this.AppendUInt32( (uint) -value );
            }
        }

        return true;
    }

    private void AppendUInt32( uint value )
    {
        unchecked
        {
            if ( value >= 1000000000 )
            {
                *(this._cursor + 0) = (char) (((value / 1000000000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 100000000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 10000000) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 1000000) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 100000) % 10) + '0');
                *(this._cursor + 5) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 6) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 7) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 8) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 9) = (char) (((value / 1) % 10) + '0');

                this._cursor += 10;
            }
            else if ( value >= 100000000 )
            {
                *(this._cursor + 0) = (char) (((value / 100000000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 10000000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 1000000) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 100000) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 5) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 6) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 7) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 8) = (char) (((value / 1) % 10) + '0');

                this._cursor += 9;
            }
            else if ( value >= 10000000 )
            {
                *(this._cursor + 0) = (char) (((value / 10000000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 1000000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 100000) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 5) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 6) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 7) = (char) (((value / 1) % 10) + '0');

                this._cursor += 8;
            }
            else if ( value >= 1000000 )
            {
                *(this._cursor + 0) = (char) (((value / 1000000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 100000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 5) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 6) = (char) (((value / 1) % 10) + '0');

                this._cursor += 7;
            }
            else if ( value >= 100000 )
            {
                *(this._cursor + 0) = (char) (((value / 100000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 5) = (char) (((value / 1) % 10) + '0');

                this._cursor += 6;
            }
            else if ( value >= 10000 )
            {
                *(this._cursor + 0) = (char) (((value / 10000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 4) = (char) (((value / 1) % 10) + '0');

                this._cursor += 5;
            }
            else if ( value >= 1000 )
            {
                *(this._cursor + 0) = (char) (((value / 1000) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 3) = (char) (((value / 1) % 10) + '0');

                this._cursor += 4;
            }
            else if ( value >= 100 )
            {
                *(this._cursor + 0) = (char) (((value / 100) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 2) = (char) (((value / 1) % 10) + '0');

                this._cursor += 3;
            }
            else if ( value >= 10 )
            {
                *(this._cursor + 0) = (char) (((value / 10) % 10) + '0');
                *(this._cursor + 1) = (char) (((value / 1) % 10) + '0');

                this._cursor += 2;
            }
            else
            {
                *(this._cursor + 0) = (char) (((value / 1) % 10) + '0');

                this._cursor += 1;
            }
        }
    }

    private void AppendPaddedUInt32( uint value )
    {
        if ( ((value / 1000000000) % 10) != 0 )
        {
            throw new FormattersAssertionFailedException();
        }

        unchecked
        {
            *(this._cursor + 0) = (char) (((value / 100000000) % 10) + '0');
            *(this._cursor + 1) = (char) (((value / 10000000) % 10) + '0');
            *(this._cursor + 2) = (char) (((value / 1000000) % 10) + '0');
            *(this._cursor + 3) = (char) (((value / 100000) % 10) + '0');
            *(this._cursor + 4) = (char) (((value / 10000) % 10) + '0');
            *(this._cursor + 5) = (char) (((value / 1000) % 10) + '0');
            *(this._cursor + 6) = (char) (((value / 100) % 10) + '0');
            *(this._cursor + 7) = (char) (((value / 10) % 10) + '0');
            *(this._cursor + 8) = (char) (((value / 1) % 10) + '0');

            this._cursor += 9;
        }
    }

    /// <summary>
    /// Appends a <see cref="ulong"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( ulong value )
    {
        if ( this._cursor + 20 > this._end )
        {
            return this.OnOverflow();
        }

        this.AppendUInt64( value );

        return true;
    }

    /// <summary>
    /// Appends a <see cref="long"/> (with decimal formatting) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( long value )
    {
        if ( this._cursor + 22 > this._end )
        {
            return this.OnOverflow();
        }

        unchecked
        {
            if ( value >= 0 )
            {
                this.AppendUInt64( (ulong) value );
            }
            else
            {
                *this._cursor = '-';
                this._cursor++;
                this.AppendUInt64( (ulong) -value );
            }
        }

        return true;
    }

    private void AppendUInt64( ulong value )
    {
        unchecked
        {
            const uint uintMaxValueDecimal = 1000000000; // 10^9

            // 64-bit arithmetic is significantly slower
            // so we're going to use 32-bit arithmetic as much as possible even for 64-bit numbers

            // value max: 18 446 744 073 709 551 616
            if ( value <= uint.MaxValue )
            {
                this.AppendUInt32( (uint) value );

                return;
            }

            // max: 18 446 744 073
            var valueMedHigh = value / uintMaxValueDecimal;

            var valueLow = (uint) (value - (uintMaxValueDecimal * valueMedHigh));

            if ( valueMedHigh <= uint.MaxValue )
            {
                this.AppendUInt32( (uint) valueMedHigh );
            }
            else
            {
                // max: 18
                var valueHigh = (byte) (valueMedHigh / uintMaxValueDecimal);

                var valueMed = (uint) (valueMedHigh - (uintMaxValueDecimal * valueHigh));

                this.AppendByte( valueHigh );
                this.AppendPaddedUInt32( valueMed );
            }

            this.AppendPaddedUInt32( valueLow );
        }
    }

    /// <summary>
    /// Appends a <see cref="bool"/> (<c>true</c> or <c>false</c>, literally) to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="value">The value to be appended.</param>
    /// <returns><c>true</c> in case of success, <c>false</c> in case of buffer overflow.</returns>
    public bool Append( bool value )
    {
        if ( value )
        {
            return this.Append( 't', 'r', 'u', 'e' );
        }
        else
        {
            return this.Append( 'f', 'a', 'l', 's', 'e' );
        }
    }

#if NET6_0_OR_GREATER
    public void Append<T>( T formattable, ReadOnlySpan<char> format = default, IFormatProvider? formatProvider = null )
        where T : ISpanFormattable
    {
        var span = new Span<char>( this._cursor, (int) (this._end - this._cursor) );

        if ( !formattable.TryFormat( span, out var charsWritten, format, formatProvider ) )
        {
            this.OnOverflow();
        }
        else
        {
            this._cursor += charsWritten;
        }
    }
#endif

    /// <summary>
    /// Clears the current <see cref="UnsafeStringBuilder"/> so it can be reused to build a new string.
    /// </summary>
    public void Clear()
    {
        this.Version++;
        this._cursor = this._start;
    }

    /// <inheritdoc />
    public override string? ToString()
    {
        // Cache through an UnsafeString.
        return this.ToUnsafeString().ToString();
    }

    /// <summary>
    /// Returns the substring starting at a given index and ending at the end of the current string.
    /// </summary>
    /// <param name="startIndex">Index of the first character of the substring.</param>
    /// <returns>The substring starting from <paramref name="startIndex"/>.</returns>
    public string Substring( int startIndex )
    {
        return this.Substring( startIndex, this.Length - startIndex );
    }

    /// <summary>
    /// Returns the substring starting at a given index and having a specified length.
    /// </summary>
    /// <param name="startIndex">Index of the first character of the substring.</param>
    /// <param name="length">Number of characters to return.</param>
    /// <returns>The substring starting from <paramref name="startIndex"/> having <paramref name="length"/> characters.</returns>
    public string Substring( int startIndex, int length )
    {
        // Exception condition added when porting code to satisfy nullable rules.
        var a = this.CharArray ?? throw new InvalidOperationException( "CharArray is unexpectedly null." );

        return new string( a, startIndex, length );
    }

    internal string ToStringImpl()
    {
        var len = (int) (this._cursor - this._start);

        return new string( this._start, 0, len );
    }

    /// <summary>
    /// Gets the version of the current <see cref="UnsafeStringBuilder"/>. This property is incremented every time
    /// the current object is reused, more specifically, when the <see cref="Clear"/> method is called.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the current <see cref="UnsafeStringBuilder"/> has been disposed.
    /// </summary>
    public bool IsDisposed => this._cursor == null;

    /// <summary>
    /// Gets the current number of characters in the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    public int Length
    {
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        get => (int) (this._cursor - this._start);
    }

    /// <summary>
    /// Truncates the string to a maximum length.
    /// </summary>
    /// <param name="length">The wished length of the string after truncation.</param>
    public void Truncate( int length )
    {
        if ( this.Length > length )
        {
            this._cursor = this._start + length;
        }
    }

    /// <summary>
    /// Gets a pointer to the unmanaged buffer of the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    public IntPtr Buffer => (IntPtr) this._start;

    // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
#pragma warning disable IDE0032
    internal char[]? CharArray => this._array;
#pragma warning restore IDE0032

    /// <summary>
    /// Gets an <see cref="UnsafeString"/> that provides read-only access to the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <returns>An <see cref="UnsafeString"/> that provides read-only access to the current <see cref="UnsafeStringBuilder"/>.</returns>
    internal UnsafeString ToUnsafeString()
    {
        if ( !this._unsafeString.Recycle() )
        {
            this._unsafeString = new UnsafeString( this );
        }

        return this._unsafeString;
    }

    /// <summary>
    /// Appends a null character at the end of the current string, without affecting the string length.
    /// In case of overflow, if <see cref="ThrowOnOverflow"/> is <c>false</c>, the last character of the string is removed
    /// and the string length is decreased by 1.
    /// </summary>
    /// <returns><c>true</c> if the string was null-terminated without affecting its length, or <c>false</c> if the last character of the string was replaced by the null character
    /// and the string length was decreased by 1.</returns>
    public bool SetNullTermination()
    {
        if ( this._cursor + 1 > this._end )
        {
            if ( this.OnOverflow() )
            {
                // If we're to overflow, we suppress the last character.
                this._cursor--;
                *this._cursor = '\0';

                return false;
            }
        }

        *this._cursor = '\0';

        return true;
    }

    /// <summary>
    /// Gets the <c>char</c> at a given position in the current <see cref="UnsafeStringBuilder"/>.
    /// </summary>
    /// <param name="index">Index of the <c>char</c> in the buffer.</param>
    /// <returns>The <c>char</c> at position <paramref name="index"/>.</returns>
    public char this[ int index ]
    {
        get => *(this._start + index);
        set => *(this._start + index) = value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if ( this._start != null )
        {
            this._gcHandle.Free();
            this._cursor = this._start = this._end = null;
            this._array = null;
            GC.SuppressFinalize( this );
        }
    }

    ~UnsafeStringBuilder()
    {
        this._gcHandle.Free();
    }
}