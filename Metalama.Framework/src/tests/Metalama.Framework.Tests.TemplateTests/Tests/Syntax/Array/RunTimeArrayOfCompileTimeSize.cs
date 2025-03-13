// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0169, CS8618

namespace Metalama.Framework.Tests.AspectTests.Tests.Templating.Syntax.Array.RunTimeArrayOfCompileTimeSize
{
    internal class Aspect
    {
        [TestTemplate]
        private dynamic? Template()
        {
            var fields = meta.Target.Type.FieldsAndProperties.Where( f => !f.IsStatic & !f.IsImplicitlyDeclared ).ToReadOnlyList();
            var values = meta.RunTime( new object[fields.Count] );

            foreach (var i in meta.CompileTime( Enumerable.Range( 0, fields.Count ) ))
            {
                values[i] = i;
            }

            return default;
        }
    }

    internal class TargetCode
    {
        private int x;

        public string Y { get; set; }

        private int Method( int a )
        {
            return a;
        }
    }
}