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
    /// Verifies that a buffered generic enumerator can be iterated, reset via the interface, and iterated again.
    /// This is the core scenario required for return value contracts on IEnumerator-returning iterator methods.
    /// </summary>
    [Fact]
    public void BufferGenericEnumerator_CanBeResetAndReused()
    {
        IEnumerator<string> buffered = RunTimeAspectHelper.Buffer( CreateTestEnumerator() );

        // First iteration.
        var firstPass = new List<string>();

        while ( buffered.MoveNext() )
        {
            firstPass.Add( buffered.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, firstPass );

        // Reset via the interface and iterate again - this is what contracts need to do.
        buffered.Reset();

        var secondPass = new List<string>();

        while ( buffered.MoveNext() )
        {
            secondPass.Add( buffered.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, secondPass );
    }

    /// <summary>
    /// Verifies that a buffered non-generic enumerator can be iterated, reset via the interface, and iterated again.
    /// </summary>
    [Fact]
    public void BufferUntypedEnumerator_CanBeResetAndReused()
    {
        IEnumerator buffered = RunTimeAspectHelper.Buffer( CreateTestUntypedEnumerator() );

        // First iteration.
        var firstPass = new List<object?>();

        while ( buffered.MoveNext() )
        {
            firstPass.Add( buffered.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, firstPass );

        // Reset via the interface and iterate again.
        buffered.Reset();

        var secondPass = new List<object?>();

        while ( buffered.MoveNext() )
        {
            secondPass.Add( buffered.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, secondPass );
    }

    /// <summary>
    /// Simulates the exact generated code pattern with multiple contracts on an IEnumerator{T} method.
    /// When boxed (assigned to the interface), re-assigning from the original variable does not produce
    /// a fresh enumerator because both variables point to the same boxed instance.
    /// </summary>
    [Fact]
    public void BufferGenericEnumerator_MultipleContractPattern_Boxed()
    {
        // Simulate the generated code pattern with interface-typed variables (boxed).
        IEnumerator<string> bufferedEnumerator = RunTimeAspectHelper.Buffer( CreateTestEnumerator() );
        IEnumerator<string> returnValue = bufferedEnumerator;

        // First contract iterates returnValue.
        var contract1Items = new List<string>();

        while ( returnValue.MoveNext() )
        {
            contract1Items.Add( returnValue.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, contract1Items );

        // Second contract re-assigns and iterates.
        // With boxing, both variables point to the same exhausted enumerator object.
        returnValue = bufferedEnumerator;
        var contract2Items = new List<string>();

        while ( returnValue.MoveNext() )
        {
            contract2Items.Add( returnValue.Current );
        }

        // With current implementation, contract2Items is empty because the boxed enumerator is exhausted.
        Assert.Equal( new[] { "Hello", "World" }, contract2Items );

        // Final yield loop should also produce all items.
        var yieldItems = new List<string>();

        while ( bufferedEnumerator.MoveNext() )
        {
            yieldItems.Add( bufferedEnumerator.Current );
        }

        Assert.Equal( new[] { "Hello", "World" }, yieldItems );
    }

    /// <summary>
    /// Simulates the exact generated code pattern with multiple contracts on an IEnumerator method (non-generic).
    /// </summary>
    [Fact]
    public void BufferUntypedEnumerator_MultipleContractPattern_Boxed()
    {
        IEnumerator bufferedEnumerator = RunTimeAspectHelper.Buffer( CreateTestUntypedEnumerator() );
        IEnumerator returnValue = bufferedEnumerator;

        // First contract iterates returnValue.
        var contract1Items = new List<object?>();

        while ( returnValue.MoveNext() )
        {
            contract1Items.Add( returnValue.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, contract1Items );

        // Second contract re-assigns and iterates.
        returnValue = bufferedEnumerator;
        var contract2Items = new List<object?>();

        while ( returnValue.MoveNext() )
        {
            contract2Items.Add( returnValue.Current );
        }

        Assert.Equal( new object[] { "Hello", "World" }, contract2Items );
    }
}
