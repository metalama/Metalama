using System;

namespace Metalama.Framework.IntegrationTests.Aspects.Overrides.Operators.CrossAssembly
{
    // <target>
    [Override]
    [Introduction]
    internal class TargetClass
    {
        public static TargetClass operator +(TargetClass x, int y)
        {
            Console.WriteLine("Original.");
            return x;
        }

        public static TargetClass operator +(TargetClass x)
        {
            Console.WriteLine("Original.");
            return x;
        }

        public static implicit operator TargetClass(int y)
        {
            Console.WriteLine("Original.");
            return new TargetClass();
        }
    }
}