// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Project;
using System;

namespace Metalama.Framework.Tests.ExtensionPoints;

/// <summary>
/// The verbs of the proof of concept. Each verb registers a contribution that the test pipeline extension processes in the transforming hook.
/// </summary>
[CompileTime]
public static class TestExtensionPointsExtensions
{
    /// <summary>
    /// Registers a contribution through an adviser. The test extension reports a warning that describes the origin of the contribution.
    /// </summary>
    /// <param name="adviser">The adviser.</param>
    /// <param name="tag">A tag that identifies the registration in the diagnostic.</param>
    /// <param name="expectedTemplateProvider">The template provider that the adviser is expected to use, or <c>null</c> to skip this check.</param>
    public static void TestRegister<T>( this IAdviser<T> adviser, string tag, ITemplateProvider? expectedTemplateProvider = null )
        where T : class, IDeclaration
        => GetService( adviser.Target.Compilation.Project ).Register( adviser, tag, expectedTemplateProvider );

    /// <summary>
    /// Registers a contribution through a query, for example from a project fabric or from <see cref="IAspectBuilder{TAspectTarget}.Outbound"/>.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="tag">A tag that identifies the registration in the diagnostic.</param>
    public static void TestRegisterQuery<T>( this IQuery<T> query, string tag )
        where T : class, IDeclaration
        => GetService( query.Project ).Register( query, tag );

    private static ITestRegistrationService GetService( IProject project )
        => project.ServiceProvider.GetService<ITestRegistrationService>()
           ?? throw new InvalidOperationException( "The test extension of the extension points is not loaded." );
}
