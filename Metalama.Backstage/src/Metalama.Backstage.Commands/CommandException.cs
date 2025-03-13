// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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