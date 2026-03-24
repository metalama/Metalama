// CompileTimeAspectPipeline.ExecuteAsync failed.
// Error LAMA0226 on `M`: `'C.M(IAspectBuilder<Foo>)' is invalid because 'C' is run-time but 'C.M(IAspectBuilder<Foo>)' is compile-time.`
// Error LAMA0236 on `t => t`: `Cannot reference 'lambda expression' in 'C.M(IAspectBuilder<Foo>)' because 'lambda expression' is run-time-only but 'C.M(IAspectBuilder<Foo>)' is Conflict.`
// Error LAMA0236 on `t`: `Cannot reference 'lambda expression/t' in 'C.M(IAspectBuilder<Foo>)' because 'lambda expression/t' is run-time-only but 'C.M(IAspectBuilder<Foo>)' is Conflict.`