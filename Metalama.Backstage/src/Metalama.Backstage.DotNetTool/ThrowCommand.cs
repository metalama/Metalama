// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Commands;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.DotNetTool;

/// <summary>
/// Throws an exception so that the exception-reporting pipeline can be exercised end to end: the report is captured, the
/// review notification is shown, and the review page offers to send it, to report all future ones automatically, or
/// never to report this particular error.
/// </summary>
/// <remarks>
/// The exception is reported through the tooling policy, which requires the current directory to be inside a git
/// repository. Use <c>--variant</c> to produce a distinct signature, e.g. to check that discarding one error does not
/// silence the others. Since the same variant is only prompted once per hour, use <c>metalama telemetry reset-dedup</c>
/// to be prompted again immediately.
/// </remarks>
[UsedImplicitly( ImplicitUseTargetFlags.WithMembers )]
internal class ThrowCommand : BaseCommand<ThrowCommandSettings>
{
    // The point of this command is to exercise exception reporting end to end, and the review notification is part of
    // it. Other commands only add the user interface on demand (the hidden '--with-ui' flag); without it there is no
    // IToastNotificationService, the report is captured silently and there is nothing to review. See #1751.
    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options )
        => options with { AddUserInterface = true };

    protected override void Execute( ExtendedCommandContext context, ThrowCommandSettings settings )
    {
        // The signature of an issue is built from the exception type and its stack frames, so each variant must throw
        // from a distinct method to be treated as a distinct issue.
        switch ( settings.Variant )
        {
            case ThrowCommandVariant.B:
                ThrowVariantB();

                break;

            case ThrowCommandVariant.C:
                ThrowVariantC();

                break;

            default:
                ThrowVariantA();

                break;
        }
    }

    private static void ThrowVariantA() => throw new InvalidOperationException( "This exception is intentional (variant A)." );

    private static void ThrowVariantB() => throw new InvalidOperationException( "This exception is intentional (variant B)." );

    private static void ThrowVariantC() => throw new InvalidOperationException( "This exception is intentional (variant C)." );
}
