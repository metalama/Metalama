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
                    "An error diagnostic is not yet registered in the current IDE session." );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserWarning
                = new(
                    "LAMA0302",
                    Warning,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "A warning diagnostic is not yet registered in the current IDE session.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserInfo
                = new(
                    "LAMA0303",
                    Info,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "An informational diagnostic is not yet registered in the current IDE session.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, string Message)>
            UserHidden
                = new(
                    "LAMA0304",
                    Hidden,
                    "{0}: {1} — Diagnostic '{0}' is new and could not be registered in the current session due to an IDE limitation."
                    + " Please restart your IDE to see it under its correct ID.",
                    "A hidden diagnostic is not yet registered in the current IDE session.",
                    _category );

        /// <summary>
        /// Reported when several syntax trees of the compilation have one path, which the project system can produce
        /// out of a valid project and which Metalama resolves by analyzing one of them. See issue #1742.
        /// </summary>
        /// <remarks>
        /// Design-time only. The command-line compiler deduplicates its source files itself, reporting <c>CS2002</c>, so
        /// the build is unaffected and this warning is the only notice the user receives.
        /// </remarks>
        internal static readonly DiagnosticDefinition<string>
            DuplicateSyntaxTreePath
                = new(
                    "LAMA0307",
                    Warning,
                    "Several source files of this project have the path '{0}'. Metalama analyzes only one of them, so a declaration "
                    + "of the others is not seen by aspects. Verify that the file is not included several times by the project file, "
                    + "for instance by a Compile item that repeats a glob, by Link metadata, or by a shared project.",
                    "Several source files of the project have the same path.",
                    _category );

        internal static readonly DiagnosticDefinition<(string Id, ISymbol Symbol)>
            UnregisteredSuppression
                = new(
                    "LAMA0306",
                    Warning,
                    "An aspect suppressed the diagnostic '{0}' on '{1}', but this suppression is not yet registered in the current IDE session due to " +
                    "an IDE limitation. Restart your IDE to apply the suppression.",
                    "A diagnostic suppression defined by an aspect is not yet registered in the IDE.",
                    _category );
    }
}