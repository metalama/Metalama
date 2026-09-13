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
/// the primary constructor of a record with an explicit one. The linker performs that replacement for a record read
/// from source, as the aspect test <c>Initialization/BeforeInstanceConstructor_Record_Primary</c> shows, and the
/// obstacle for an introduced record is one step of it:
/// <c>LinkerInjectionStep.AuxiliaryMemberFactory.GetAuxiliarySourceConstructor</c> reads the positional parameter
/// list from the declaring syntax of the constructor, and the declaration of an introduced record is produced by the
/// same injection step rather than read from source, so that syntax carries no parameter list. The advice is refused
/// with a diagnostic rather than left to fail there with an assertion. Serving it would mean taking the parameter
/// list from the builder data instead, which is issue #2020.
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
