// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Aspects;

[CompileTime]
public sealed class InvalidAdviceParametersException : Exception
{
    internal InvalidAdviceParametersException( string message ) : base( message ) { }
}