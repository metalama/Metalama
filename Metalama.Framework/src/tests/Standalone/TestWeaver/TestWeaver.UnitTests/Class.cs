// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
