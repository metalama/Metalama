// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Fabrics;
using Metalama.Framework.Services;

namespace Metalama.Framework.Tests.ExtensionPoints;

/// <summary>
/// The service that the verbs of <see cref="TestExtensionPointsExtensions"/> call. It is implemented by the test engine assembly.
/// </summary>
[CompileTime]
internal interface ITestExtensionPointsService : IProjectService
{
    void Register<T>( IAdviser<T> adviser, string tag, ITemplateProvider? expectedTemplateProvider )
        where T : class, IDeclaration;

    void Register<T>( IQuery<T> query, string tag )
        where T : class, IDeclaration;

    void RegisterThroughCreatedQuery<T>( IAdviser<T> adviser, string tag )
        where T : class, IDeclaration;

    void ReportReferences<T>( IAdviser<T> adviser, string methodName, bool restrictToTarget )
        where T : class, IDeclaration;

    void RedirectCalls<T>( IAdviser<T> adviser, string methodName, IMethod replacement, TestRedirectionOptions options )
        where T : class, IDeclaration;

    void RedirectCalls<T>( IQuery<T> query, string methodName, string replacementTypeName, string replacementMethodName, TestRedirectionOptions options )
        where T : class, IDeclaration;
}
