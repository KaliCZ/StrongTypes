namespace StrongTypes.Api.Entities;

public sealed class NonEmptyStringEntity : EntityBase<NonEmptyStringEntity, NonEmptyString, NonEmptyString?>;

public sealed class PositiveIntEntity : EntityBase<PositiveIntEntity, Positive<int>, Positive<int>?>;

public sealed class NonNegativeIntEntity : EntityBase<NonNegativeIntEntity, NonNegative<int>, NonNegative<int>?>;

public sealed class NegativeIntEntity : EntityBase<NegativeIntEntity, Negative<int>, Negative<int>?>;

public sealed class NonPositiveIntEntity : EntityBase<NonPositiveIntEntity, NonPositive<int>, NonPositive<int>?>;
