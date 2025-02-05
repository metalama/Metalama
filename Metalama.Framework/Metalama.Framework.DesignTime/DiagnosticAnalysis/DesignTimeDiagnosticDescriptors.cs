// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
                    "{0}: {1} The diagnostic {0} was not defined in the user profile and has been replaced by a generic diagnostic ID.",
                    "A Metalama user error." );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserWarning
                = new(
                    "LAMA0302",
                    Warning,
                    "{0}: {1} The diagnostic {0} was not defined in the user profile and has been replaced by a generic diagnostic ID. "
                    + "Please restart your IDE.",
                    "A Metalama user warning.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserInfo
                = new(
                    "LAMA0303",
                    Info,
                    "{0}: {1} The diagnostic {0} was not defined in the user profile and has been replaced by a generic diagnostic ID. "
                    + " Please restart your IDE.",
                    "A Metalama user info.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserHidden
                = new(
                    "LAMA0304",
                    Hidden,
                    "{0}: {1} The diagnostic {0} was not defined in the user profile and has been replaced by a generic diagnostic ID."
                    + " Please restart your IDE.",
                    "A Metalama user hidden message.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, ISymbol Symbol)>
            UnregisteredSuppression
                = new(
                    "LAMA0306",
                    Warning,
                    "An aspect tried to suppress the diagnostic {0} on '{1}', but this diagnostic ID has not been configured for "
                    + "suppression in the user profile. Please restart your IDE.",
                    "An aspect tried to suppress an unregistered diagnostic.",
                    _category );
    }
}