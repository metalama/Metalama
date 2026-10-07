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
        => GetService( adviser.Compilation.Project ).Register( adviser, tag, expectedTemplateProvider );

    /// <summary>
    /// Registers a contribution through a query, for example from a project fabric or from <see cref="IAspectBuilder{TAspectTarget}.Outbound"/>.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="tag">A tag that identifies the registration in the diagnostic.</param>
    public static void TestRegisterQuery<T>( this IQuery<T> query, string tag )
        where T : class, IDeclaration
        => GetService( query.Project ).Register( query, tag );

    /// <summary>
    /// Registers a contribution through a query that the extension creates from the adviser, with <c>AdviserExtensionContext.CreateQuery</c>.
    /// The query selects the target of the adviser and is owned by the owner of the adviser.
    /// </summary>
    /// <param name="adviser">The adviser.</param>
    /// <param name="tag">A tag that identifies the registration in the diagnostic.</param>
    public static void TestRegisterThroughCreatedQuery<T>( this IAdviser<T> adviser, string tag )
        where T : class, IDeclaration
        => GetService( adviser.Compilation.Project ).RegisterThroughCreatedQuery( adviser, tag );

    /// <summary>
    /// Requests the references to the methods of a given name from the shared index of source references. The test extension reports a warning
    /// for each reference that it reads from the index.
    /// </summary>
    /// <param name="adviser">The adviser.</param>
    /// <param name="methodName">The name of the referenced methods.</param>
    /// <param name="restrictToTarget"><c>true</c> to give the target of the adviser as the only declaration root of the index.</param>
    public static void TestReportReferences<T>( this IAdviser<T> adviser, string methodName, bool restrictToTarget = false )
        where T : class, IDeclaration
        => GetService( adviser.Compilation.Project ).ReportReferences( adviser, methodName, restrictToTarget );

    /// <summary>
    /// Redirects the source calls to the methods of a given name, inside the target of the adviser, to another method. With
    /// <see cref="TestRedirectionOptions.MethodReferences"/>, it redirects the method groups instead of the calls.
    /// </summary>
    /// <param name="adviser">The adviser, whose target is the scope of the redirection.</param>
    /// <param name="methodName">The name of the source methods.</param>
    /// <param name="replacement">The method that replaces the source methods.</param>
    /// <param name="options">The options of the redirection, or <c>null</c> for the default options.</param>
    public static void TestRedirectCalls<T>( this IAdviser<T> adviser, string methodName, IMethod replacement, TestRedirectionOptions? options = null )
        where T : class, IDeclaration
        => GetService( adviser.Compilation.Project ).RedirectCalls( adviser, methodName, replacement, options ?? new TestRedirectionOptions() );

    /// <summary>
    /// Redirects the source calls to the methods of a given name, inside the declarations selected by a query, to another method. This verb is
    /// used by fabrics.
    /// </summary>
    /// <param name="query">The query, whose declarations are the scope of the redirection.</param>
    /// <param name="methodName">The name of the source methods.</param>
    /// <param name="replacementTypeName">The full name of the type that declares the replacement method.</param>
    /// <param name="replacementMethodName">The name of the replacement method, which must not be overloaded.</param>
    /// <param name="options">The options of the redirection, or <c>null</c> for the default options.</param>
    public static void TestRedirectCalls<T>(
        this IQuery<T> query,
        string methodName,
        string replacementTypeName,
        string replacementMethodName,
        TestRedirectionOptions? options = null )
        where T : class, IDeclaration
        => GetService( query.Project ).RedirectCalls( query, methodName, replacementTypeName, replacementMethodName, options ?? new TestRedirectionOptions() );

    /// <summary>
    /// Redirects the source calls to the methods of a given name, inside the target of the adviser, to methods that the extension declares and
    /// whose body is generated from a template of the aspect.
    /// </summary>
    /// <param name="adviser">The adviser, whose target is the scope of the redirection.</param>
    /// <param name="methodName">The name of the source methods.</param>
    /// <param name="template">The name of the template.</param>
    /// <param name="options">The options of the redirection, or <c>null</c> for the default options.</param>
    public static void TestRedirectCallsToTemplate<T>(
        this IAdviser<T> adviser,
        string methodName,
        string template,
        TestTemplateRedirectionOptions? options = null )
        where T : class, IDeclaration
        => GetService( adviser.Compilation.Project ).RedirectCallsToTemplate( adviser, methodName, template, options ?? new TestTemplateRedirectionOptions() );

    /// <summary>
    /// Gets the initializer of a field or property as an expression that compile-time code can inspect but that cannot be emitted in generated
    /// code.
    /// </summary>
    /// <param name="fieldOrProperty">A field or property with an initializer in source code.</param>
    public static IExpression TestGetInspectionOnlyInitializer( this IFieldOrProperty fieldOrProperty )
        => GetService( fieldOrProperty.Compilation.Project ).GetInspectionOnlyInitializer( fieldOrProperty );

    /// <summary>
    /// Returns the <see cref="ITestExtensionPointsService"/> of a project.
    /// </summary>
    /// <exception cref="InvalidOperationException">The test extension of the extension points is not loaded.</exception>
    private static ITestExtensionPointsService GetService( IProject project )
        => project.ServiceProvider.GetService<ITestExtensionPointsService>()
           ?? throw new InvalidOperationException( "The test extension of the extension points is not loaded." );
}
