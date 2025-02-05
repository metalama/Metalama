// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Commands;

public sealed class CommandException : Exception
{
    public int ReturnCode { get; }

    public CommandException( string message, int returnCode = 1 ) : base( message )
    {
        this.ReturnCode = returnCode;
    }
}