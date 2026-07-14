// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Threading.Tasks;
using Issue1722.Aspects;

namespace Issue1722.Consumer;

internal class Target
{
    // The aspect's eligibility runs against these methods during the EvaluateAspectSources pipeline step.
    // Evaluating eligibility invokes SetContextAttribute.ProduceValidationFailureMessage -> the cross-package
    // extension method AspectUtilities.IsResultTask, which is where issue #1722 reports a FileNotFoundException
    // for 'ml!...Primitives...'. This solution builds green, proving the cross-package extension method resolves
    // correctly in the eligibility pipeline stage.
    [SetContext]
    private static Task<int> DoTheThing3( int a, int b ) => Task.FromResult( a + b );
}
