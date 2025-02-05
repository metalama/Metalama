using System;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.PropertyAccessorOverride;

#pragma warning disable CS0162

[assembly: AspectOrder(AspectOrderDirection.RunTime, typeof(Aspect1), typeof(Aspect2))]

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Formatting.PropertyAccessorOverride
{
    public class Aspect1 : PropertyAspect
    {
        public override void BuildAspect(IAspectBuilder<IProperty> builder)
        {
            builder.Advice.OverrideAccessors(builder.Target, nameof(Override), nameof(Override));
        }

        [Template]
        public dynamic? Override()
        {
            meta.InsertComment("Comment before Aspect1.");
            Console.WriteLine(nameof(Aspect1));
            meta.InsertComment("Comment mid Aspect1.");

            return meta.Proceed();

            meta.InsertComment("Comment after Aspect1.");
        }
    }

    public class Aspect2 : PropertyAspect
    {
        public override void BuildAspect(IAspectBuilder<IProperty> builder)
        {
            builder.Advice.OverrideAccessors(builder.Target, nameof(Override), nameof(Override));
        }

        [Template]
        public dynamic? Override()
        {
            meta.InsertComment("Comment before Aspect2.");
            Console.WriteLine(nameof(Aspect2));
            meta.InsertComment("Comment mid Aspect2.");

            return meta.Proceed();

            meta.InsertComment("Comment after Aspect2.");
        }
    }

    // <target>
    public class Target
    {
        // Before Foo.
        [Aspect1]
        [Aspect2]
        public int Foo // After Foo name.
        { // After Foo opening brace.
            // Before Foo.get.
            get // After Foo.get keyword.
            // Before Foo.get opening brace.
            { // After Foo.get opening brace.
                Console.WriteLine("Foo.get");
                return 42;
                // Before Foo.get closing brace.
            } // After Foo.get closing brace.
            // After Foo.get and before Foo.set.
            set // After Foo.set keyword.
            // Before Foo.set opening brace.
            { // After Foo.set opening brace.
                Console.WriteLine("Foo.set");
                // Before Foo.set closing brace.
            } // After Foo.set closing brace.
            // Before Foo closing brace.
        } // After Foo closing brace.
        // After Foo/before Bar.
        [Aspect1]
        [Aspect2]
        public int Bar // After Bar name.
        { // After Bar opening brace.
            // Before Bar.get.
            get // After Bar.get keyword.
            // Before Bar.get semicolon
            ; // After Bar.get semicolon.
            // After Bar.get and before Bar.set.
            set // After Bar.set keyword.
            // Before Bar.set semicolon
            ; // After Bar.set semicolon.
            // Before Bar closing brace.
        }// After Bar closing brace.
        // After Bar/before Baz.
        [Aspect1]
        [Aspect2]
        public int Baz // After Baz name.
        { // After Baz opening brace.
            // Before Baz.get.
            get // After Baz.get keyword.
            // Before Baz.get arrow.
                => // After Baz.get arrow.
                   // Before Baz.get expression.
                    42 // After Baz.get expression.
            // Before Baz.get semicolon.
            ; // After Baz.get semicolon.
            // Before Baz.set.
            set // After Baz.set keyword.
            // Before Baz.set arrow.
                => // After Baz.set arrow.
                   // Before Baz.set expression.
                Console.WriteLine("Foo.set") // After Baz.set expression.
            // Before Baz.set semicolon.
            ; // After Baz.set semicolon.
            // Before Baz closing brace.
        }// After Baz closing brace.
        // After Baz/before Qux.
        [Aspect1]
        [Aspect2]
        public int Qux // After Qux name
                       // Before Qux.get arrow.
            => // After Qux.get arrow.
               // Before Qux.get expression.
                42 // After Qux.get expression.
        // Before Qux.get semicolon.
        ; // After Qux.get semicolon.
        // After Qux.
    }
}
