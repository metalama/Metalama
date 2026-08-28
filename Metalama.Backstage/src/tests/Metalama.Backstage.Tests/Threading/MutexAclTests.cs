// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Threading;
using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Metalama.Backstage.Tests.Threading;

/// <summary>
/// Tests the translation of a Win32 error code into an exception performed by <see cref="MutexAcl"/>.
/// </summary>
/// <remarks>
/// The translation is worth a test of its own because getting it wrong is silent. An error code that is not
/// converted into an <c>HRESULT</c> reads as a success code, which makes
/// <see cref="Marshal.GetExceptionForHR(int)"/> return <see langword="null"/> and the caller throw a bare
/// <see cref="System.ComponentModel.Win32Exception"/>. The type of the exception is what
/// <c>NamedLockService.TryOpenOrCreateMutex</c> classifies on, so an exception of the wrong type does not merely
/// carry a worse message: it disables the retry that absorbs the race in which another process creates the mutex
/// between the attempt to open it and the attempt to create it.
/// </remarks>
public sealed class MutexAclTests
{
    /// <summary>
    /// The Win32 code for a denied access, which is what the operating system returns when the mutex was created
    /// by a peer process with a security descriptor that does not grant this process the right to create it again.
    /// </summary>
    private const int _errorAccessDenied = 5;

    /// <summary>
    /// Verifies that a Win32 error code is converted into an <c>HRESULT</c> that the runtime maps to the intended
    /// exception.
    /// </summary>
    [Fact]
    public void AWin32ErrorCodeIsConvertedIntoTheIntendedException()
    {
        var hresult = MutexAcl.GetHResultForWin32Error( _errorAccessDenied );

        Assert.Equal( unchecked((int) 0x80070005), hresult );
        Assert.IsType<UnauthorizedAccessException>( Marshal.GetExceptionForHR( hresult ) );
    }

    /// <summary>
    /// Verifies that the raw Win32 error code, which the implementation used to pass through unchanged, yields no
    /// exception at all.
    /// </summary>
    /// <remarks>
    /// This records the defect rather than the fix: it is what made the conversion necessary, and it is the reason
    /// a caller that classified on the exception type never saw the type it was waiting for.
    /// </remarks>
    [Fact]
    public void ARawWin32ErrorCodeYieldsNoException()
    {
        Assert.Null( Marshal.GetExceptionForHR( _errorAccessDenied ) );
    }

    /// <summary>
    /// Verifies that a value that is already an <c>HRESULT</c>, or a success code, is passed through unchanged.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    [Theory]
    [InlineData( 0 )]
    [InlineData( unchecked((int) 0x80070005) )]
    [InlineData( -1 )]
    public void AValueThatIsNotAPositiveWin32ErrorCodeIsUnchanged( int value )
    {
        Assert.Equal( value, MutexAcl.GetHResultForWin32Error( value ) );
    }
}
