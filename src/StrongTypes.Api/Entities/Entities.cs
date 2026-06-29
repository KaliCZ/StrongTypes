using System.Net.Mail;

namespace StrongTypes.Api.Entities;

public sealed class NonEmptyStringEntity : EntityBase<NonEmptyStringEntity, NonEmptyString, NonEmptyString?>;

public sealed class EmailEntity : EntityBase<EmailEntity, MailAddress, MailAddress?>;

public sealed class PositiveIntEntity : EntityBase<PositiveIntEntity, Positive<int>, Positive<int>?>;
public sealed class NonNegativeIntEntity : EntityBase<NonNegativeIntEntity, NonNegative<int>, NonNegative<int>?>;
public sealed class NegativeIntEntity : EntityBase<NegativeIntEntity, Negative<int>, Negative<int>?>;
public sealed class NonPositiveIntEntity : EntityBase<NonPositiveIntEntity, NonPositive<int>, NonPositive<int>?>;

public sealed class PositiveLongEntity : EntityBase<PositiveLongEntity, Positive<long>, Positive<long>?>;
public sealed class NonNegativeLongEntity : EntityBase<NonNegativeLongEntity, NonNegative<long>, NonNegative<long>?>;
public sealed class NegativeLongEntity : EntityBase<NegativeLongEntity, Negative<long>, Negative<long>?>;
public sealed class NonPositiveLongEntity : EntityBase<NonPositiveLongEntity, NonPositive<long>, NonPositive<long>?>;

public sealed class PositiveShortEntity : EntityBase<PositiveShortEntity, Positive<short>, Positive<short>?>;
public sealed class NonNegativeShortEntity : EntityBase<NonNegativeShortEntity, NonNegative<short>, NonNegative<short>?>;
public sealed class NegativeShortEntity : EntityBase<NegativeShortEntity, Negative<short>, Negative<short>?>;
public sealed class NonPositiveShortEntity : EntityBase<NonPositiveShortEntity, NonPositive<short>, NonPositive<short>?>;

public sealed class PositiveDecimalEntity : EntityBase<PositiveDecimalEntity, Positive<decimal>, Positive<decimal>?>;
public sealed class NonNegativeDecimalEntity : EntityBase<NonNegativeDecimalEntity, NonNegative<decimal>, NonNegative<decimal>?>;
public sealed class NegativeDecimalEntity : EntityBase<NegativeDecimalEntity, Negative<decimal>, Negative<decimal>?>;
public sealed class NonPositiveDecimalEntity : EntityBase<NonPositiveDecimalEntity, NonPositive<decimal>, NonPositive<decimal>?>;

public sealed class PositiveFloatEntity : EntityBase<PositiveFloatEntity, Positive<float>, Positive<float>?>;
public sealed class NonNegativeFloatEntity : EntityBase<NonNegativeFloatEntity, NonNegative<float>, NonNegative<float>?>;
public sealed class NegativeFloatEntity : EntityBase<NegativeFloatEntity, Negative<float>, Negative<float>?>;
public sealed class NonPositiveFloatEntity : EntityBase<NonPositiveFloatEntity, NonPositive<float>, NonPositive<float>?>;

public sealed class PositiveDoubleEntity : EntityBase<PositiveDoubleEntity, Positive<double>, Positive<double>?>;
public sealed class NonNegativeDoubleEntity : EntityBase<NonNegativeDoubleEntity, NonNegative<double>, NonNegative<double>?>;
public sealed class NegativeDoubleEntity : EntityBase<NegativeDoubleEntity, Negative<double>, Negative<double>?>;
public sealed class NonPositiveDoubleEntity : EntityBase<NonPositiveDoubleEntity, NonPositive<double>, NonPositive<double>?>;

public sealed class ClosedIntervalEntity : EntityBase<ClosedIntervalEntity, ClosedInterval<int>, ClosedInterval<int>?>;
public sealed class IntervalEntity : EntityBase<IntervalEntity, Interval<int>, Interval<int>?>;
public sealed class IntervalFromEntity : EntityBase<IntervalFromEntity, IntervalFrom<int>, IntervalFrom<int>?>;
public sealed class IntervalUntilEntity : EntityBase<IntervalUntilEntity, IntervalUntil<int>, IntervalUntil<int>?>;

// Same four interval types, mapped to two scalar columns (Start/End) instead of
// a single JSON column — exercises the HasIntervalColumns persistence shape.
public sealed class ClosedIntervalColumnsEntity : EntityBase<ClosedIntervalColumnsEntity, ClosedInterval<int>, ClosedInterval<int>?>;
public sealed class IntervalColumnsEntity : EntityBase<IntervalColumnsEntity, Interval<int>, Interval<int>?>;
public sealed class IntervalFromColumnsEntity : EntityBase<IntervalFromColumnsEntity, IntervalFrom<int>, IntervalFrom<int>?>;
public sealed class IntervalUntilColumnsEntity : EntityBase<IntervalUntilColumnsEntity, IntervalUntil<int>, IntervalUntil<int>?>;
