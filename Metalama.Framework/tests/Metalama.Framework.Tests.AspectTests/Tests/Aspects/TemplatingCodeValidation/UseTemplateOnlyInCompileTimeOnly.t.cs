// CompileTimeAspectPipeline.ExecuteAsync failed.
// Error LAMA0233 on `Proceed`: `Cannot use 'meta.Proceed()' in 'C.M(IMethodInvoker)' because it is only allowed inside a template.`
// Error LAMA0233 on `ProceedAsync`: `Cannot use 'meta.ProceedAsync()' in 'C.M(IMethodInvoker)' because it is only allowed inside a template.`
// Error LAMA0233 on `InsertStatement`: `Cannot use 'meta.InsertStatement(IExpression)' in 'C.M(IMethodInvoker)' because it is only allowed inside a template.`
// Error LAMA0233 on `Capture`: `Cannot use 'ExpressionFactory.Capture(dynamic?)' in 'C.M(IMethodInvoker)' because it is only allowed inside a template.`
// Error LAMA0233 on `With`: `Cannot use 'IMethodInvoker.With(dynamic?, InvokerOptions)' in 'C.M(IMethodInvoker)' because it is only allowed inside a template.`
// Error LAMA0233 on `This`: `Cannot use 'meta.This' in 'C.GetLoggingExpression(IParameter)' because it is only allowed inside a template. Use ExpressionFactory.This() instead of meta.This.`
// Error LAMA0233 on `AppendExpression`: `Cannot use 'SyntaxBuilder.AppendExpression(dynamic?)' in 'C.GetLoggingExpression(IParameter)' because it is only allowed inside a template. Use 'AppendLiteral(parameter.Name)' instead of 'AppendExpression(parameter.Name)'.`
// Error LAMA0233 on `Value`: `Cannot use 'IExpression.Value' in 'C.GetLoggingExpression(IParameter)' because it is only allowed inside a template. Use 'parameter' directly instead of accessing 'parameter.Value'.`