// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @WriteCompiledTemplate
#endif

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Templating;

#pragma warning disable CS0162 // Unreachable code detected

namespace Metalama.Framework.Tests.TemplateTests.Return.PropertyPatternAfterReturn
{
    [CompileTime]
    internal class Person
    {
        public string Name { get; set; } = "";
        public int Age { get; set; }
    }

    [CompileTime]
    internal class Aspect
    {
        // Test property pattern matching: if (obj is { Property: var value })
        [TestTemplate]
        private dynamic? Template()
        {
            if ( meta.Target.Method.Parameters.Count == 0 )
            {
                return null;
            }

            var person = GetPerson();

            // Property pattern matching
            if ( person is { Name: var name, Age: var age } )
            {
                return $"{name} is {age} years old";
            }

            return meta.Proceed();
        }

        [CompileTime]
        private static Person GetPerson()
        {
            return new Person { Name = "John", Age = 30 };
        }
    }

    internal class TargetCode
    {
        // <target>
        private object? Method()
        {
            return null;
        }
    }
}
