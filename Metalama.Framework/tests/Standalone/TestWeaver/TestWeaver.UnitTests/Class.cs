// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

#pragma warning disable CA1822 // Mark members as static
#pragma warning disable IDE0051 // Remove unused private members
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace Metalama.Open.Virtuosity.TestApp
{
    [Virtualize]
    internal class C
    {
        // Not transformed.
        private void ImplicitPrivate() { }

        // Not transformed.
        private void ExplicitPrivate() { }

        // Transformed.
        public void Public() { }

        // Not transformed (already virtual).
        public virtual void PublicVirtual() { }

        // Transformed.
        protected async void Protected() { }

        // Transformed.
        private protected void PrivateProtected() { }

        // Transformed (should not be sealed).
        public sealed override string ToString()
        {
            return null;
        }

        // Not transformed.
        public override int GetHashCode()
        {
            return 0;
        }

        // Not transformed.
        public static void PublicStatic() { }

        public int Property { get; }
    }

    internal sealed partial class SC
    {
        public void M() { }
    }

}
