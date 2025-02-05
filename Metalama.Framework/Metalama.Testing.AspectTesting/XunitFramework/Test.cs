// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Xunit;
using Xunit.Abstractions;

namespace Metalama.Testing.AspectTesting.XunitFramework
{
    internal sealed class Test : LongLivedMarshalByRefObject, ITest
    {
        public string DisplayName => this.TestCase.DisplayName;

        public ITestCase TestCase { get; }

        public Test( ITestCase testCase )
        {
            this.TestCase = testCase;
        }
    }
}