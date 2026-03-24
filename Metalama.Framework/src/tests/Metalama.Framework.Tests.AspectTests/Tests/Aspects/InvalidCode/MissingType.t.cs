// CompileTimeAspectPipeline.ExecuteAsync failed.
// Error LAMA0292 on `M`: `Execution scope mismatch: the member 'C.M(IAspectBuilder<Foo>)' is compile-time, but the declaring type 'C' is run-time.`
// Error LAMA0236 on `t`: `Cannot reference 'lambda expression/t' in 'C.M(IAspectBuilder<Foo>)' because 'lambda expression/t' is run-time-only but 'C.M(IAspectBuilder<Foo>)' is compile-time.`