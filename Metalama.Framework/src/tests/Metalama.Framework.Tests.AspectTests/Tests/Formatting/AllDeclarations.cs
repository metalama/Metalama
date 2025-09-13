// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.AllDeclarations
{
    // We need at least an aspect otherwise the template annotator does not run.
    internal class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod() => null;
    }
    
    [CompileTime]
    internal struct CompileTimeStruct
    {
        public event EventHandler FieldEvent;
        public event EventHandler ManualEvent
        {
            add => throw new Exception();
            remove => throw new Exception();
        }
        
        public string Property { get; set; }
    }

    internal struct RunTimeStruct
    {
        public event EventHandler FieldEvent;
        public event EventHandler ManualEvent
        {
            add => throw new Exception();
            remove => throw new Exception();
        }
        
        public string Property { get; set; }
    }

    [CompileTime]
    internal record CompileTimeRecord( int f );

    internal record RunTimeRecord( int f );

    [CompileTime]
    internal delegate void CompileTimeDelegate();

    internal delegate void RunTimeDelegate();



}