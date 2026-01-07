// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable IDE0047 // Remove unnecessary parentheses

namespace Metalama.Framework.Tests.TemplateTests.Tests.Dynamic.DynamicWithTargetTypedExpressions;

[CompileTime]
internal class Aspect
{
    [TestTemplate]
    private dynamic? Template()
    {
        // All of these should produce compile-time errors
        dynamic? x1 = default;              // default literal
        dynamic? x2 = (default);            // parenthesized default
        dynamic? x3 = ((default));          // double parenthesized default
        dynamic? x4 = null;                 // null literal
        dynamic? x5 = (null);               // parenthesized null
        dynamic x6 = default;               // non-nullable dynamic with default
        dynamic x7 = null;                  // non-nullable dynamic with null

        return meta.Proceed();
    }
}

// <target>
internal class TargetCode
{
    private int Method( int a )
    {
        return a;
    }
}
