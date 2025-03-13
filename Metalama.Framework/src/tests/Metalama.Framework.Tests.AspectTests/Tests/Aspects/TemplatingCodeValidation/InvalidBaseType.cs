// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.TemplatingCodeValidation.InvalidBaseType;

[CompileTime]
public class MyCompileTimeType : IDisposable, IRunTimeOnlyInterface, ICompileTimeOnlyInterface
{
    public void Dispose() { }
}

// No error expected here.
[RunTimeOrCompileTime]
public class MyRunTimeOrCompileTimeType : IDisposable, IRunTimeOnlyInterface, ICompileTimeOnlyInterface
{
    public void Dispose() { }
}

public class TypeWithConflicts : IDisposable, IRunTimeOnlyInterface, ICompileTimeOnlyInterface
{
    public void Dispose() { }
}

public interface IRunTimeOnlyInterface { }

[CompileTime]
public interface ICompileTimeOnlyInterface { }