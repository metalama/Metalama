// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using Xunit.Sdk;
using Xunit.v3;

namespace Metalama.Testing.AspectTesting.XunitFramework;

/// <summary>
/// Creates the messages that the <see cref="TestExecutor"/> sends to xunit. Each message identifies the test object
/// that it describes, and its parents, by their unique identifiers.
/// </summary>
internal static class TestMessages
{
    private static readonly IReadOnlyDictionary<string, TestAttachment> _noAttachments = new Dictionary<string, TestAttachment>( StringComparer.Ordinal );

    public static TestAssemblyStarting AssemblyStarting( TestAssembly assembly, string? targetFramework )
        => new()
        {
            AssemblyName = assembly.AssemblyName,
            AssemblyPath = assembly.AssemblyPath,
            AssemblyUniqueID = assembly.UniqueID,
            ConfigFilePath = assembly.ConfigFilePath,
            Seed = null,
            StartTime = DateTimeOffset.Now,
            TargetFramework = targetFramework,
            TestEnvironment = $"{IntPtr.Size * 8}-bit .NET {Environment.Version}",
            TestFrameworkDisplayName = AspectTestFramework.DisplayName,
            Traits = assembly.Traits
        };

    public static TestAssemblyFinished AssemblyFinished( TestAssembly assembly, Metrics metrics )
        => new()
        {
            AssemblyUniqueID = assembly.UniqueID,
            ExecutionTime = metrics.ExecutionTime,
            FinishTime = DateTimeOffset.Now,
            TestsFailed = metrics.TestFailed,
            TestsNotRun = 0,
            TestsSkipped = metrics.TestSkipped,
            TestsTotal = metrics.TestsTotal
        };

    public static TestCollectionStarting CollectionStarting( ITestCollection collection )
        => new()
        {
            AssemblyUniqueID = collection.TestAssembly.UniqueID,
            StartTime = DateTimeOffset.Now,
            TestCollectionClassName = collection.TestCollectionClassName,
            TestCollectionDisplayName = collection.TestCollectionDisplayName,
            TestCollectionUniqueID = collection.UniqueID,
            Traits = collection.Traits
        };

    public static TestCollectionFinished CollectionFinished( ITestCollection collection, Metrics metrics )
        => new()
        {
            AssemblyUniqueID = collection.TestAssembly.UniqueID,
            ExecutionTime = metrics.ExecutionTime,
            FinishTime = DateTimeOffset.Now,
            TestCollectionUniqueID = collection.UniqueID,
            TestsFailed = metrics.TestFailed,
            TestsNotRun = 0,
            TestsSkipped = metrics.TestSkipped,
            TestsTotal = metrics.TestsTotal
        };

    public static TestClassStarting ClassStarting( ITestClass testClass )
        => new()
        {
            AssemblyUniqueID = testClass.TestCollection.TestAssembly.UniqueID,
            StartTime = DateTimeOffset.Now,
            TestClassName = testClass.TestClassName,
            TestClassNamespace = testClass.TestClassNamespace,
            TestClassSimpleName = testClass.TestClassSimpleName,
            TestClassUniqueID = testClass.UniqueID,
            TestCollectionUniqueID = testClass.TestCollection.UniqueID,
            Traits = testClass.Traits
        };

    public static TestClassFinished ClassFinished( ITestClass testClass, Metrics metrics )
        => new()
        {
            AssemblyUniqueID = testClass.TestCollection.TestAssembly.UniqueID,
            ExecutionTime = metrics.ExecutionTime,
            FinishTime = DateTimeOffset.Now,
            TestClassUniqueID = testClass.UniqueID,
            TestCollectionUniqueID = testClass.TestCollection.UniqueID,
            TestsFailed = metrics.TestFailed,
            TestsNotRun = 0,
            TestsSkipped = metrics.TestSkipped,
            TestsTotal = metrics.TestsTotal
        };

    public static TestMethodStarting MethodStarting( ITestMethod testMethod )
        => new()
        {
            AssemblyUniqueID = testMethod.TestClass.TestCollection.TestAssembly.UniqueID,
            MethodArity = testMethod.MethodArity,
            MethodName = testMethod.MethodName,
            StartTime = DateTimeOffset.Now,
            TestClassUniqueID = testMethod.TestClass.UniqueID,
            TestCollectionUniqueID = testMethod.TestClass.TestCollection.UniqueID,
            TestMethodUniqueID = testMethod.UniqueID,
            Traits = testMethod.Traits
        };

    public static TestMethodFinished MethodFinished( ITestMethod testMethod, Metrics metrics )
        => new()
        {
            AssemblyUniqueID = testMethod.TestClass.TestCollection.TestAssembly.UniqueID,
            ExecutionTime = metrics.ExecutionTime,
            FinishTime = DateTimeOffset.Now,
            TestClassUniqueID = testMethod.TestClass.UniqueID,
            TestCollectionUniqueID = testMethod.TestClass.TestCollection.UniqueID,
            TestMethodUniqueID = testMethod.UniqueID,
            TestsFailed = metrics.TestFailed,
            TestsNotRun = 0,
            TestsSkipped = metrics.TestSkipped,
            TestsTotal = metrics.TestsTotal
        };

    public static TestCaseStarting CaseStarting( TestCase testCase )
        => new()
        {
            AssemblyUniqueID = testCase.TestCollection.TestAssembly.UniqueID,
            Explicit = testCase.Explicit,
            SkipReason = testCase.SkipReason,
            SourceFilePath = testCase.SourceFilePath,
            SourceLineNumber = testCase.SourceLineNumber,
            StartTime = DateTimeOffset.Now,
            TestCaseDisplayName = testCase.TestCaseDisplayName,
            TestCaseUniqueID = testCase.UniqueID,
            TestClassMetadataToken = testCase.TestClassMetadataToken,
            TestClassName = testCase.TestClassName,
            TestClassNamespace = testCase.TestClassNamespace,
            TestClassSimpleName = testCase.TestClassSimpleName,
            TestClassUniqueID = testCase.TestClass.UniqueID,
            TestCollectionUniqueID = testCase.TestCollection.UniqueID,
            TestMethodArity = testCase.TestMethodArity,
            TestMethodMetadataToken = testCase.TestMethodMetadataToken,
            TestMethodName = testCase.TestMethodName,
            TestMethodParameterTypesVSTest = testCase.TestMethodParameterTypesVSTest,
            TestMethodReturnTypeVSTest = testCase.TestMethodReturnTypeVSTest,
            TestMethodUniqueID = testCase.TestMethod.UniqueID,
            Traits = testCase.Traits
        };

    public static TestCaseFinished CaseFinished( TestCase testCase, Metrics metrics )
        => new()
        {
            AssemblyUniqueID = testCase.TestCollection.TestAssembly.UniqueID,
            ExecutionTime = metrics.ExecutionTime,
            FinishTime = DateTimeOffset.Now,
            TestCaseUniqueID = testCase.UniqueID,
            TestClassUniqueID = testCase.TestClass.UniqueID,
            TestCollectionUniqueID = testCase.TestCollection.UniqueID,
            TestMethodUniqueID = testCase.TestMethod.UniqueID,
            TestsFailed = metrics.TestFailed,
            TestsNotRun = 0,
            TestsSkipped = metrics.TestSkipped,
            TestsTotal = metrics.TestsTotal
        };

    public static TestStarting TestStarting( Test test )
        => new()
        {
            AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
            Explicit = test.TestCase.Explicit,
            StartTime = DateTimeOffset.Now,
            TestCaseUniqueID = test.TestCase.UniqueID,
            TestClassUniqueID = test.TestCase.TestClass.UniqueID,
            TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
            TestDisplayName = test.TestDisplayName,
            TestLabel = test.TestLabel,
            TestMethodUniqueID = test.TestCase.TestMethod.UniqueID,
            TestUniqueID = test.UniqueID,
            Timeout = 0,
            Traits = test.Traits
        };

    public static TestFinished TestFinished( Test test, decimal executionTime, string output )
        => new()
        {
            AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
            Attachments = _noAttachments,
            ExecutionTime = executionTime,
            FinishTime = DateTimeOffset.Now,
            Output = output,
            TestCaseUniqueID = test.TestCase.UniqueID,
            TestClassUniqueID = test.TestCase.TestClass.UniqueID,
            TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
            TestMethodUniqueID = test.TestCase.TestMethod.UniqueID,
            TestUniqueID = test.UniqueID,
            Warnings = null
        };

    public static TestPassed TestPassed( Test test, decimal executionTime, string output )
        => new()
        {
            AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
            ExecutionTime = executionTime,
            FinishTime = DateTimeOffset.Now,
            Output = output,
            TestCaseUniqueID = test.TestCase.UniqueID,
            TestClassUniqueID = test.TestCase.TestClass.UniqueID,
            TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
            TestMethodUniqueID = test.TestCase.TestMethod.UniqueID,
            TestUniqueID = test.UniqueID,
            Warnings = null
        };

    public static TestSkipped TestSkipped( Test test, string reason, string output )
        => new()
        {
            AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
            ExecutionTime = 0,
            FinishTime = DateTimeOffset.Now,
            Output = output,
            Reason = reason,
            TestCaseUniqueID = test.TestCase.UniqueID,
            TestClassUniqueID = test.TestCase.TestClass.UniqueID,
            TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
            TestMethodUniqueID = test.TestCase.TestMethod.UniqueID,
            TestUniqueID = test.UniqueID,
            Warnings = null
        };

    public static ITestFailed TestFailed( Test test, Exception exception, decimal executionTime, string output )
        => Xunit.v3.TestFailed.FromException(
            exception,
            test.TestCase.TestCollection.TestAssembly.UniqueID,
            test.TestCase.TestCollection.UniqueID,
            test.TestCase.TestClass.UniqueID,
            test.TestCase.TestMethod.UniqueID,
            test.TestCase.UniqueID,
            test.UniqueID,
            executionTime,
            output,
            null,
            null );

    public static TestOutput Output( Test test, string output )
        => new()
        {
            AssemblyUniqueID = test.TestCase.TestCollection.TestAssembly.UniqueID,
            Output = output,
            TestCaseUniqueID = test.TestCase.UniqueID,
            TestClassUniqueID = test.TestCase.TestClass.UniqueID,
            TestCollectionUniqueID = test.TestCase.TestCollection.UniqueID,
            TestMethodUniqueID = test.TestCase.TestMethod.UniqueID,
            TestUniqueID = test.UniqueID
        };

    public static DiagnosticMessage Diagnostic( string message ) => new() { Message = message };
}
