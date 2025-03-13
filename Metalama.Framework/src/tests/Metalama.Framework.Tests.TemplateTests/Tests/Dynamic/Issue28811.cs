// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169, CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Dynamic.Issue28811
{
    [CompileTime]
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var field = meta.Target.Type.FieldsAndProperties.Single();

            var clone1 = meta.This;
            var clone2 = meta.This;
            var clone3 = meta.This;
            field.With( (IExpression)clone1 ).Value = clone1;
            field.With( (IExpression)clone2 ).Value = field.With( (IExpression)meta.This ).Value;
            field.With( (IExpression)clone3 ).Value = field.With( (IExpression)meta.This ).Value!.Clone();

            return default;
        }
    }

    // Placeholder implementation of a cache because the hosted try.postsharp.net does not allow for MemoryCache.

    // <target>
    internal class TargetCode
    {
        private int a;

        private void Method() { }
    }
}