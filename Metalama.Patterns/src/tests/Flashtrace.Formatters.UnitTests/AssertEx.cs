// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;

namespace Flashtrace.Formatters.UnitTests;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global
internal static class AssertEx
{
    // NOTE: Xunit doesn't support assert messages by design (except for True and False methods).

    public static void NotNull( object expected, string message ) => Assert.NotNull( expected );

    public static void Null( object expectedNull, string message ) => Assert.Null( expectedNull );

    public static void NotEqual( object notExpected, object actual, string message ) => Assert.NotEqual( notExpected, actual );

    public static void Equal( object expected, object actual, string message ) => Assert.Equal( expected, actual );

    public static void Equal( long expected, long actual, string message ) => Assert.Equal( expected, actual );

    public static void Equal( int expected, int actual, string message ) => Assert.Equal( expected, actual );

    public static void NotSame( object notExpected, object actual, string message ) => Assert.NotSame( notExpected, actual );

    public static void Inconclusive()
        =>

            // TODO: implement a replacement for Inconclusive, see https://github.com/AArnott/Xunit.SkippableFact
            // Xunit doesn't support inconclusive tests
            Assert.True( true, "Inconclusive" );

    public static void Inconclusive( string message )
        =>

            // TODO: implement a replacement for Inconclusive, see https://github.com/AArnott/Xunit.SkippableFact
            // Xunit doesn't support inconclusive tests
            Assert.True( true, message );

    public static void Fail( string message ) => Assert.Fail( message );

    public static void Fail() => Assert.True( false );

    public static void EqualSet<T>( IEnumerable<T> expected, IEnumerable<T> actual ) => Assert.Equal( new HashSet<T>( expected ), new HashSet<T>( actual ) );

    public static void ContainsAll<T>( IEnumerable<T> expected, ICollection<T> actual )
    {
        foreach ( var item in expected )
        {
            Assert.Contains( item, actual );
        }
    }

    public static void Throws<T>( Action task )
        where T : Exception
        => Assert.Throws<T>( task );

    public static Task ThrowsAsync<T>( Func<Task> task )
        where T : Exception
        => Assert.ThrowsAsync<T>( task );

    public static void Throws<T>( string expectedMessage, Action task )
        where T : Exception
    {
        try
        {
            task();
        }
        catch ( Exception ex )
        {
            AssertExceptionType<T>( ex );
            AssertExceptionMessage( ex, expectedMessage );

            return;
        }

        Assert.Fail( $"Expected exception of type {typeof(T)} but no exception was thrown." );
    }

    public static async Task ThrowsAsync<T>( string expectedMessage, Func<Task> task )
        where T : Exception
    {
        try
        {
            await task();
        }
        catch ( Exception ex )
        {
            AssertExceptionType<T>( ex );
            AssertExceptionMessage( ex, expectedMessage );

            return;
        }

        Assert.Fail( $"Expected exception of type {typeof(T)} but no exception was thrown." );
    }

    private static void AssertExceptionType<T>( Exception ex ) => Assert.True( ex.GetType() == typeof(T), "Expected exception type failed." );

    private static void AssertExceptionMessage( Exception ex, string expectedMessage )
        => Assert.True(
            string.Equals(
                expectedMessage.ToLowerInvariant(),
                ex.Message.ToLowerInvariant(),
                StringComparison.Ordinal ),
            "Expected exception message failed." );

    public static void ContainsMultiple( string expectedText, int expectedCount, string actualText )
    {
        var nextStart = 0;
        var actualCount = 0;

        while ( true )
        {
            var index = actualText.IndexOf( expectedText, nextStart, StringComparison.Ordinal );

            if ( index != -1 )
            {
                nextStart = index + 1;
                actualCount++;
            }
            else
            {
                break;
            }
        }

        Assert.True(
            actualCount == expectedCount,
            $"Expected string \"{expectedText}\" to be present {expectedCount} times but it was present {actualCount} times." );
    }
}