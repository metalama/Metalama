// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Observability.AspectTests.NotObservableVirtualProperty
{
    // <target>
    [Observable]
    public class TestClass
    {
        [NotObservable]
        public virtual int Foo { get; set; }
    }
}