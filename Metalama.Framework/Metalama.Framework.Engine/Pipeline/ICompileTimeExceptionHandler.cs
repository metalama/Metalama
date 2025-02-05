// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;

namespace Metalama.Framework.Engine.Pipeline
{
    [PublicAPI] // Implemented by Metalama.Try.
    public interface ICompileTimeExceptionHandler : IGlobalService
    {
        void ReportException( Exception exception, Action<Diagnostic> reportDiagnostic, bool canIgnoreException, out bool isHandled );
    }
}