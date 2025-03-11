// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.TestFramework;

internal sealed class TestMessageSink : LongLivedMarshalByRefObject, IMessageSink
{
    public List<IMessageSinkMessage> Messages { get; } = new();

    public bool OnMessage( IMessageSinkMessage message )
    {
        lock ( this.Messages )
        {
            this.Messages.Add( message );
        }

        return true;
    }
}