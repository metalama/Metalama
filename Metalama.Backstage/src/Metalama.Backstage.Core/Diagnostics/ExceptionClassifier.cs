// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security;

namespace Metalama.Backstage.Diagnostics;

public static class ExceptionClassifier
{
    public static ClassifiedException Classify( Exception e ) => new( ShouldReportException( e ), e );

    private static bool ShouldReportException( Exception exception )
    {
        switch ( exception )
        {
            case IOException _:
            case SecurityException _:
            case UnauthorizedAccessException _:
            case WebException _:
            case OperationCanceledException _:
                return false;
        }

        if ( exception.InnerException != null && !ShouldReportException( exception.InnerException ) )
        {
            return false;
        }

        if ( exception is AggregateException aggregateException && aggregateException.InnerExceptions.Any( e => !ShouldReportException( e ) ) )
        {
            return false;
        }

        return true;
    }
}