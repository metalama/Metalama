// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace PostSharp.Aspects.Dependencies
{
    /// <summary>
    /// Aspect roles are not supported in Metalama.
    /// </summary>
    public static class StandardRoles
    {
        public const string Validation = "Validation";

        public const string Tracing = "Tracing";

        public const string PerformanceInstrumentation = "PerformanceInstrumentation";

        public const string Security = "Security";

        public const string Caching = "Caching";

        public const string TransactionHandling = "Transaction";

        public const string ExceptionHandling = "ExceptionHandling";

        public const string DataBinding = "DataBinding";

        public const string Persistence = "Persistence";

        public const string EventBroker = "Event Broker";

        public const string Threading = "Threading";
    }
}