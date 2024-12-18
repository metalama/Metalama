// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Validation;
using System.Reflection;

namespace Metalama.Framework.Engine.Validation;

internal sealed class MethodBasedReferenceValidatorDriver : ValidatorDriver<ReferenceValidationContext>
{
    private readonly InvokeValidatorDelegate<ReferenceValidationContext> _validateMethod;
    private readonly MethodInfo _validateMethodInfo;

    public MethodBasedReferenceValidatorDriver(
        MethodInfo validateMethodInfoInfo,
        InvokeValidatorDelegate<ReferenceValidationContext> validateMethod )
    {
        this._validateMethodInfo = validateMethodInfoInfo;
        this._validateMethod = validateMethod;
    }

    public override void Validate(
        ValidatorImplementation implementation,
        in ReferenceValidationContext context,
        UserCodeInvoker invoker,
        UserCodeExecutionContext executionContext )
    {
        var invokePayload = new InvokePayload( implementation, context, this );
        invoker.TryInvoke( InvokePayload.Validate, ref invokePayload, executionContext, out _ );
    }

    // Intentionally not marking the struct as readonly to avoid defensive copies when passing by ref.
    private struct InvokePayload
    {
        private readonly ValidatorImplementation _implementation;
        private readonly ReferenceValidationContext _context;
        private readonly MethodBasedReferenceValidatorDriver _driver;

        public InvokePayload( ValidatorImplementation implementation, in ReferenceValidationContext context, MethodBasedReferenceValidatorDriver driver )
        {
            this._implementation = implementation;
            this._context = context;
            this._driver = driver;
        }

        public static bool Validate( ref InvokePayload payload )
        {
            payload._driver._validateMethod( payload._implementation.Implementation, payload._context );

            return true;
        }
    }

    internal override UserCodeDescription GetUserCodeMemberInfo( ValidatorInstance validatorInstance )
        => UserCodeDescription.Create( "executing {0}", validatorInstance );

    public override string MethodName => this._validateMethodInfo.Name;
}