// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine
{
    [ExcludeFromCodeCoverage]
    public sealed class AssertionFailedException : Exception
    {
        public AssertionFailedException() { }

        public AssertionFailedException( string message ) : base( message ) { }

        internal AssertionFailedException( AssertionFailedInterpolatedStringHandler messageHandler ) : base( messageHandler.GetFormattedText() ) { }
    }
}