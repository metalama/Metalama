// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.RunTime;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.RunTime;

/// <summary>
/// Tests for <see cref="RunTimeAspectHelper.Buffer{T}(IEnumerator{T})"/> and related methods,
/// verifying that the buffered enumerator can be used multiple times (issue #695).
/// </summary>
public class RunTimeAspectHelperBufferTests
{
    private static IEnumerator<string> CreateTestEnumerator()
    {
        yield return "Hello";
        yield return "World";
    }

    private static IEnumerator CreateTestUntypedEnumerator()
    {
        yield return "Hello";
        yield return "World";
    }

    /// <summary>
    /// Verifies that <see cref="RunTimeAspectHelper.Buffer{T}(IEnumerator{T})"/> returns a <see cref="ResettableEnumerator{T}"/>.
    /// </summary>
    [Fact]
    public void BufferGenericEnumerator_ReturnsResettableEnumerator()
    {
        var buffered = RunTimeAspectHelper.Buffer( CreateTestEnumerator() );

        Assert.IsType<ResettableEnumerator<string>>( buffered );
    }

    /// <summary>
    /// Verifies that <see cref="RunTimeAspectHelper.Buffer(IEnumerator)"/> returns a <see cref="ResettableEnumerator"/>.
    /// </summary>
    [Fact]
    public void BufferUntypedEnumerator_ReturnsResettableEnumerator()
    {
        var buffered = RunTimeAspectHelper.Buffer( CreateTestUntypedEnumerator() );

        Assert.IsType<ResettableEnumerator>( buffered );
    }

    /// <summary>
    /// Verifies that a buffered generic enumerator can be iterated, reset, and iterated again.
    /// This is the core scenario required for return value contracts on IEnumerator-returning iterator methods.
    /// </summary>
    [Fact]
    public void BufferGenericEnumerator_CanBeResetAndReused()
    {
        var buffered = RunTimeAspectHelper.Buffer( CreateTestEnumerator() );

        // First iteration.
        var firstPass = new List<string>();

        while ( buffered.MoveNext() )
        {
            firstPass.Add( buffered.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, firstPass );

        // Reset and iterate again - this is what contracts need to do.
        buffered.Reset();

        var secondPass = new List<string>();

        while ( buffered.MoveNext() )
        {
            secondPass.Add( buffered.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, secondPass );
    }

    /// <summary>
    /// Verifies that a buffered non-generic enumerator can be iterated, reset, and iterated again.
    /// </summary>
    [Fact]
    public void BufferUntypedEnumerator_CanBeResetAndReused()
    {
        var buffered = RunTimeAspectHelper.Buffer( CreateTestUntypedEnumerator() );

        // First iteration.
        var firstPass = new List<object?>();

        while ( buffered.MoveNext() )
        {
            firstPass.Add( buffered.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, firstPass );

        // Reset and iterate again.
        buffered.Reset();

        var secondPass = new List<object?>();

        while ( buffered.MoveNext() )
        {
            secondPass.Add( buffered.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, secondPass );
    }

    /// <summary>
    /// Simulates the generated code pattern with multiple contracts on an IEnumerator{T} method.
    /// The new pattern uses Reset() before the final yield loop.
    /// </summary>
    [Fact]
    public void BufferGenericEnumerator_MultipleContractPatternWithReset()
    {
        var bufferedEnumerator = RunTimeAspectHelper.Buffer( CreateTestEnumerator() );
        var returnValue = bufferedEnumerator;

        // First contract iterates returnValue (same reference as bufferedEnumerator).
        var contract1Items = new List<string>();

        while ( returnValue.MoveNext() )
        {
            contract1Items.Add( returnValue.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, contract1Items );

        // Between contracts, re-assign (no-op for reference types) happens automatically.
        returnValue = bufferedEnumerator;

        // Reset before second contract.
        bufferedEnumerator.Reset();

        var contract2Items = new List<string>();

        while ( returnValue.MoveNext() )
        {
            contract2Items.Add( returnValue.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, contract2Items );

        // Reset before the final yield loop.
        bufferedEnumerator.Reset();

        var yieldItems = new List<string>();

        while ( bufferedEnumerator.MoveNext() )
        {
            yieldItems.Add( bufferedEnumerator.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, yieldItems );
    }

    /// <summary>
    /// Simulates the generated code pattern with multiple contracts on an IEnumerator method (non-generic).
    /// </summary>
    [Fact]
    public void BufferUntypedEnumerator_MultipleContractPatternWithReset()
    {
        var bufferedEnumerator = RunTimeAspectHelper.Buffer( CreateTestUntypedEnumerator() );
        var returnValue = bufferedEnumerator;

        // First contract iterates returnValue.
        var contract1Items = new List<object?>();

        while ( returnValue.MoveNext() )
        {
            contract1Items.Add( returnValue.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, contract1Items );

        // Reset before second contract.
        bufferedEnumerator.Reset();

        returnValue = bufferedEnumerator;
        var contract2Items = new List<object?>();

        while ( returnValue.MoveNext() )
        {
            contract2Items.Add( returnValue.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, contract2Items );

        // Reset before the final yield loop.
        bufferedEnumerator.Reset();

        var yieldItems = new List<object?>();

        while ( bufferedEnumerator.MoveNext() )
        {
            yieldItems.Add( bufferedEnumerator.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, yieldItems );
    }
}
