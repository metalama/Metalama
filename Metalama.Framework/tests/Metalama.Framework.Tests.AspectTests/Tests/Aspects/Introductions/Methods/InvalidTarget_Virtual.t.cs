// CompileTimeAspectPipeline.ExecuteAsync failed.
// Warning MANUAL_ASSERT on `TargetStruct`: `Manually assert that 3 errors are reported on this class.`
// Error LAMA0525 on `TargetStruct`: `The aspect 'ExplicitlyVirtualIntroduction' cannot introduce virtual member 'TargetStruct.Method_ExplicitlyVirtual()' into a type 'TargetStruct' because it is static, sealed or a struct.`
// Error LAMA0525 on `TargetStruct`: `The aspect 'ImplicitlyVirtualIntroduction' cannot introduce virtual member 'TargetStruct.Method_ImplicitlyVirtual()' into a type 'TargetStruct' because it is static, sealed or a struct.`
// Warning MANUAL_ASSERT on `SealedTargetClass`: `Manually assert that 3 errors are reported on this class.`
// Error LAMA0525 on `SealedTargetClass`: `The aspect 'ExplicitlyVirtualIntroduction' cannot introduce virtual member 'SealedTargetClass.Method_ExplicitlyVirtual()' into a type 'SealedTargetClass' because it is static, sealed or a struct.`
// Error LAMA0525 on `SealedTargetClass`: `The aspect 'ImplicitlyVirtualIntroduction' cannot introduce virtual member 'SealedTargetClass.Method_ImplicitlyVirtual()' into a type 'SealedTargetClass' because it is static, sealed or a struct.`
// Warning MANUAL_ASSERT on `StaticTargetClass`: `Manually assert that 3 errors are reported on this class.`
// Error LAMA0505 on `StaticTargetClass`: `The aspect 'ExplicitlyVirtualIntroduction' cannot introduce instance member 'StaticTargetClass.Method_ExplicitlyVirtual()' into a type 'StaticTargetClass' because it is static.`
// Error LAMA0505 on `StaticTargetClass`: `The aspect 'ImplicitlyVirtualIntroduction' cannot introduce instance member 'StaticTargetClass.Method_ImplicitlyVirtual()' into a type 'StaticTargetClass' because it is static.`
