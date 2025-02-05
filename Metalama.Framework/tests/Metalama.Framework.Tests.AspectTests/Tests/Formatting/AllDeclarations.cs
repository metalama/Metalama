using System;
using Metalama.Framework.Aspects;

#pragma warning disable CS0067

namespace Metalama.Framework.Tests.AspectTests.Tests.Formatting.AllDeclarations
{
    // We need at least an aspect otherwise the template annotator does not run.
    class Aspect : OverrideMethodAspect
    {
        public override dynamic? OverrideMethod() => null;
    }
    
    [CompileTime]
    struct CompileTimeStruct
    {
        public event EventHandler FieldEvent;
        public event EventHandler ManualEvent
        {
            add => throw new Exception();
            remove => throw new Exception();
        }
        
        public string Property { get; set; }
    }

    struct RunTimeStruct
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
    record CompileTimeRecord( int f );

    record RunTimeRecord( int f );

    [CompileTime]
    delegate void CompileTimeDelegate();

    delegate void RunTimeDelegate();



}