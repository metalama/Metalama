// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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