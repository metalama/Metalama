// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Testing;

public class TestException : Exception
{
    public override string StackTrace { get; }

    public TestException( string message, string stackTrace, Exception? innerException = null ) : base( message, innerException )
    {
        this.StackTrace = stackTrace;
    }
}