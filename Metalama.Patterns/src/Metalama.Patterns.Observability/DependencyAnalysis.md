# Dependency Analysis

## PostSharp behaviour

The PS implementation has several so-called 'validators'. These are invoked in order, where acceptance or rejection can be short-circuited.

### Common concepts

`TypeHelper` defines the following extensions taking `System.Type`:

- `IsIntrinsic`: true for `type.IsPrimitive` and `string`
- `IsImmutable`: true for primitives, `decimal`, `DateTime`, `string`, `enum` types and value types with namespace `System`.
- `IsIdempotentMethod`: true for methods of `Nullable<T>` or with `[Pure]` attribute.
- `IsFrameworkStaticMethod` matches static member methods from assemblies with public key token from a set of hard-coded known Microsoft tokens.

### Property Validators

#### `GenericMethodInfoPropertyValidator`

Looks at the basic characteristics exposed by `MethodInfo`. Accepts (final):

- Void methods with no ref or out parameters.
- `object.ToString()`
- `object.GetHashCode()`
- Has atrribute `[IgnoreAutoChangeNotification]`
- Is a method of `StringBuilder`
- Is a method of the metadata-hinting class `Depends`

#### `IdempotentMethodValidator`

1. Accepts (final) operator methods
2. Defines `allargumentsAreImmutable` which is true when all args are immutable (see `IsImmutable` above) or an array of immutable or `object` elements.
3. Accepts (final) `IsIdempotentMethod && allargumentsAreImmutable`.
4. Accepts (final) `IsFrameworkStaticMethod && allargumentsAreImmutable`.

#### `VirtualMethodCallValidator`

Rejects (final) virtual methods which don't have `[IgnoreUnobservableExpressionsAttribute]`.

#### `OuterScopeObjectMethodCallValidator`

_PS Specific:_ Abstains from methods that look like lambda method compiler implementation detail.

Rejects (final) calls to methods of a different class unless the method returns `void` and has no ref or out parameters, or is marked `[Pure]`, or the calling method is marked `[IgnoreUnobservableExpressionsAttribute]`.

### Field Validators

#### `DependsFieldValidator`

Accepts (final) fields of the `Depends` type.

#### `OuterScopeObjectFieldValidator`

1. Abstains for access to fields of `this`.
2. _PS Specific:_ Abstains for access to compiler-generated closure fields.
3. Abstains for access to `const` fields, and immutable `static readonly` fields.
4. Abstains for access to fields of `ValueTuple`.
5. Rejects (final) direct access to fields of another class unless the calling method is marked `[IgnoreUnobservableExpressionsAttribute]`.
6. Otherwise, accepts (final).