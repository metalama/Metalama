// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Enhancements.Inherited
{
    [Layers( "1", "2" )]
    [Inheritable]
    internal class TestAttribute : TypeAspect
    {
        [Introduce( Layer = "1" )]
        private void Foo1()
        {
            var target = meta.Target.Method.DeclaringType;

            meta.InsertComment(
                $"Current='{target}'" +
                $"HasAspect={( target.Enhancements().HasAspect( typeof(TestAttribute) ) )}" );

            meta.InsertComment(
                $"BaseType='{target.BaseType}', BelToCurProj={target.BaseType?.BelongsToCurrentProject}, " +
                $"HasAspect={( target.BaseType?.BelongsToCurrentProject == true ? target.BaseType?.Enhancements().HasAspect( typeof(TestAttribute) ) : "n/a" )}" );

            foreach (var pred in meta.AspectInstance.Predecessors)
            {
                meta.InsertComment( $"BasePred = {pred.Instance.TargetDeclaration?.ToString()}" );
            }
        }

        [Introduce( Layer = "2" )]
        private void Foo2()
        {
            var target = meta.Target.Method.DeclaringType;

            meta.InsertComment(
                $"BaseType='{target.BaseType}', BelToCurProj={target.BaseType?.BelongsToCurrentProject}, " +
                $"HasAspect={( target.BaseType?.BelongsToCurrentProject == true ? target.BaseType?.Enhancements().HasAspect( typeof(TestAttribute) ) : "n/a" )}" );

            foreach (var pred in meta.AspectInstance.Predecessors)
            {
                meta.InsertComment( $"BasePred = {pred.Instance.TargetDeclaration?.ToString()}" );
            }
        }
    }

// <target>
    namespace Targets
    {
        [Test]
        internal class A { }

        internal class B : A { }

        internal class C : B { }

        internal class D : C { }
    }
}