// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Xunit.Sdk;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

/// <summary>
/// An <see cref="IMessageSink"/> that records the messages it receives.
/// </summary>
internal sealed class TestMessageSink : IMessageSink
{
    private readonly Func<IMessageSinkMessage, bool>? _shouldContinue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestMessageSink"/> class.
    /// </summary>
    /// <param name="shouldContinue">A function that returns the value of <see cref="OnMessage"/> for a message, or
    /// <c>null</c> to always return <c>true</c>. The executor cancels the run when the value is <c>false</c>.</param>
    public TestMessageSink( Func<IMessageSinkMessage, bool>? shouldContinue = null )
    {
        this._shouldContinue = shouldContinue;
    }

    public List<IMessageSinkMessage> Messages { get; } = new();

    public bool OnMessage( IMessageSinkMessage message )
    {
        lock ( this.Messages )
        {
            this.Messages.Add( message );
        }

        return this._shouldContinue?.Invoke( message ) ?? true;
    }
}
