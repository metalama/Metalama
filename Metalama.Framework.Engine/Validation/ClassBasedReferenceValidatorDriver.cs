// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Validation;

namespace Metalama.Framework.Engine.Validation;

internal sealed class ClassBasedReferenceValidatorDriver : ValidatorDriver<ReferenceValidationContext>
{
    public static ClassBasedReferenceValidatorDriver Instance { get; } = new();

    private ClassBasedReferenceValidatorDriver() { }

    public override void Validate(
        ValidatorImplementation implementation,
        in ReferenceValidationContext context,
        UserCodeInvoker invoker,
        UserCodeExecutionContext executionContext )
    {
        var invokePayload = new InvokePayload( implementation, context );
        invoker.TryInvoke( InvokePayload.Validate, ref invokePayload, executionContext, out _ );
    }

    internal override UserCodeDescription GetUserCodeMemberInfo( ValidatorInstance validatorInstance )
        => UserCodeDescription.Create( "executing the Validate for", (ReferenceValidatorInstance) validatorInstance );

    public override string? MethodName => null;

    private struct InvokePayload
    {
        private readonly ValidatorImplementation _implementation;
        private readonly ReferenceValidationContext _context;

        public InvokePayload( ValidatorImplementation implementation, ReferenceValidationContext context )
        {
            this._implementation = implementation;
            this._context = context;
        }

        public static bool Validate( ref InvokePayload payload )
        {
            ((InboundReferenceValidator) payload._implementation.Implementation).ValidateReferences( payload._context );

            return true;
        }
    }
}