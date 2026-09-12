// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Advising;

/// <summary>
/// Refuses an advice that would replace the primary constructor of a record that an aspect introduced.
/// </summary>
/// <remarks>
/// <para>
/// An initializer that runs before an instance constructor, and an override of the constructor itself, both replace
/// the primary constructor of a record by an explicit one. The linker performs that replacement by rewriting the
/// declaration of the record as it is written in source, and a record that an aspect introduces has no such
/// declaration, so the advice is refused with a diagnostic rather than left to fail inside the linker.
/// </para>
/// </remarks>
internal static class IntroducedPrimaryConstructorValidator
{
    /// <summary>
    /// Returns a value indicating whether the constructor is the primary constructor of a record that an aspect
    /// introduced.
    /// </summary>
    public static bool IsIntroducedPrimaryConstructor( IConstructor constructor )
        => constructor is { IsPrimary: true, Origin.Kind: DeclarationOriginKind.Aspect };

    /// <summary>
    /// Creates the diagnostic that refuses the advice.
    /// </summary>
    public static Diagnostic CreateRefusalDiagnostic(
        IConstructor constructor,
        AdviceKind adviceKind,
        string aspectType,
        IDiagnosticSource diagnosticSource )
    {
        var declaringType = constructor.DeclaringType;

        return AdviceDiagnosticDescriptors.CannotReplaceIntroducedPrimaryConstructor.CreateRoslynDiagnostic(
            declaringType.GetDiagnosticLocation(),
            (aspectType, adviceKind, declaringType),
            diagnosticSource );
    }
}
