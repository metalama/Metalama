// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Diagnostics;
using Microsoft.CodeAnalysis;
using static Metalama.Framework.Diagnostics.Severity;

#pragma warning disable SA1118

namespace Metalama.Framework.DesignTime.DiagnosticAnalysis
{
    internal static class DesignTimeDiagnosticDescriptors
    {
        // Reserved range 300-319

        private const string _category = "Metalama.DesignTime";

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserError
                = new(
                    "LAMA0301",
                    Error,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "A Metalama user error." );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserWarning
                = new(
                    "LAMA0302",
                    Warning,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "A Metalama user warning.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserInfo
                = new(
                    "LAMA0303",
                    Info,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "A Metalama user info.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserHidden
                = new(
                    "LAMA0304",
                    Hidden,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "A Metalama user hidden message.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, ISymbol Symbol)>
            UnregisteredSuppression
                = new(
                    "LAMA0306",
                    Warning,
                    "An aspect tried to suppress diagnostic '{0}' on '{1}', but '{0}' was not registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to apply the suppression.",
                    "An aspect tried to suppress an unregistered diagnostic.",
                    _category );
    }
}