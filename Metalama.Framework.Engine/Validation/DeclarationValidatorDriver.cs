// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Utilities.UserCode;
using Metalama.Framework.Validation;

namespace Metalama.Framework.Engine.Validation;

internal sealed class DeclarationValidatorDriver : ValidatorDriver<DeclarationValidationContext>
{
    private readonly ValidatorDelegate<DeclarationValidationContext> _validator;

    public DeclarationValidatorDriver( ValidatorDelegate<DeclarationValidationContext> validator )
    {
        this._validator = validator;
    }

    public override void Validate(
        ValidatorImplementation implementation,
        in DeclarationValidationContext context,
        UserCodeInvoker invoker,
        UserCodeExecutionContext executionContext )
    {
        var invokePayload = new InvokePayload( context, this );
        invoker.TryInvoke( InvokePayload.Validate, ref invokePayload, executionContext, out _ );
    }

    // Intentionally not marking the struct as readonly to avoid defensive copies when passing by ref.
    private struct InvokePayload
    {
        private readonly DeclarationValidationContext _context;
        private readonly DeclarationValidatorDriver _driver;

        public InvokePayload( in DeclarationValidationContext context, DeclarationValidatorDriver driver )
        {
            this._context = context;
            this._driver = driver;
        }

        public static bool Validate( ref InvokePayload payload )
        {
            payload._driver._validator.Invoke( payload._context );

            return true;
        }
    }

    internal override UserCodeDescription GetUserCodeMemberInfo( ValidatorInstance validatorInstance )
        => UserCodeDescription.Create( "executing the validator method for ", validatorInstance );

    public override string MethodName => this._validator.Method.Name;
}