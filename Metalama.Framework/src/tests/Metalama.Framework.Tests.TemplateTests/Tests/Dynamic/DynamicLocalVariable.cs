// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

namespace Metalama.Framework.Tests.TemplateTests.Tests.Dynamic.DynamicLocalVariable;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        // No initializer.
        var local1 = meta.DefineLocalVariable( "myLocal", typeof(int) );
        local1.Value = 1;

        // Dynamic initializer.
        meta.DefineLocalVariable( "myLocal", typeof(int), 2 );
        
              // Expression initializer.
              meta.DefineLocalVariable( "myLocal", typeof(int), meta.Target.Parameters[0] );


              // Var, dynamic initializer.
              meta.DefineLocalVariable( "myLocal", 3 );

              // Var, expression initializer.
              meta.DefineLocalVariable( "myLocal", meta.Target.Parameters[0] );
      

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    private int Method( int myLocal )
    {
        return myLocal;
    }
}