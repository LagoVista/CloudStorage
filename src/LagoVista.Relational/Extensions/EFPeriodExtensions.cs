using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace LagoVista.Core.EF
{
    public static class EfPeriodExtensions
    {
        /// <summary>
        /// Filters the query to records whose UTC <see cref="DateTime"/> value falls within
        /// the specified inclusive calendar period.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="utcTimestampSelector">
        /// Expression selecting a UTC <see cref="DateTime"/> property.
        /// </param>
        /// <param name="start">
        /// The inclusive start boundary of the period.
        /// Records must satisfy: <c>timestamp &gt;= start at 00:00:00Z</c>.
        /// </param>
        /// <param name="end">
        /// The inclusive end boundary of the period.
        /// All timestamps occurring on the end date are included.
        /// Internally implemented as:
        /// <c>timestamp &lt; (end + 1 day) at 00:00:00Z</c>
        /// to ensure full-day inclusion.
        /// </param>
        /// <returns>
        /// A query containing only records within the specified calendar period.
        /// </returns>
        /// <remarks>
        /// Both <paramref name="start"/> and <paramref name="end"/> are required.
        /// This method assumes the underlying <see cref="DateTime"/> values represent UTC instants.
        /// A half-open interval is intentionally used for the upper bound to avoid
        /// precision and time-of-day ambiguity.
        /// </remarks>
        public static IQueryable<T> InPeriod<T>(
            this IQueryable<T> query,
            Expression<Func<T, DateOnly>> dateSelector,
            CalendarDate start,
            CalendarDate end)
        {
            var s = DateOnly.FromDateTime(start.ToDateTime());
            var e = DateOnly.FromDateTime(end.ToDateTime());

            return query.Where(BuildDateOnlyBetween(dateSelector, s, e));
        }

        /// <summary>
        /// Filters the query to records whose <see cref="DateOnly"/> value falls within
        /// the specified inclusive calendar period.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="dateSelector">Expression selecting a <see cref="DateOnly"/> property.</param>
        /// <param name="start">
        /// The inclusive start boundary of the period.
        /// Records must satisfy: <c>date &gt;= start</c>.
        /// </param>
        /// <param name="end">
        /// The inclusive end boundary of the period.
        /// Records must satisfy: <c>date &lt;= end</c>.
        /// </param>
        /// <returns>
        /// A query containing only records within the specified calendar period.
        /// </returns>
        /// <remarks>
        /// Both <paramref name="start"/> and <paramref name="end"/> are required.
        /// This method represents true calendar semantics with no time-of-day adjustments.
        /// </remarks>
        public static IQueryable<T> InPeriod<T>(
            this IQueryable<T> query,
            Expression<Func<T, DateOnly?>> dateSelector,
            CalendarDate start,
            CalendarDate end)
        {
            var s = DateOnly.FromDateTime(start.ToDateTime());
            var e = DateOnly.FromDateTime(end.ToDateTime());

            return query.Where(BuildNullableDateOnlyBetween(dateSelector, s, e));
        }

        // -----------------------------
        // DateTime UTC timestamp column vs CalendarDate period:
        // inclusive semantics via exclusive upper bound on next day.
        // -----------------------------
        public static IQueryable<T> InPeriodUtc<T>(
            this IQueryable<T> query,
            Expression<Func<T, DateTime>> utcTimestampSelector,
            CalendarDate start,
            CalendarDate end)
        {
            var sUtc = DateTime.SpecifyKind(start.ToDateTime().Date, DateTimeKind.Utc);
            var eExclusiveUtc = DateTime.SpecifyKind(end.ToDateTime().Date.AddDays(1), DateTimeKind.Utc);

            return query.Where(BuildDateTimeHalfOpen(utcTimestampSelector, sUtc, eExclusiveUtc));
        }

        public static IQueryable<T> InPeriodUtc<T>(
            this IQueryable<T> query,
            Expression<Func<T, DateTime?>> utcTimestampSelector,
            CalendarDate start,
            CalendarDate end)
        {
            var sUtc = DateTime.SpecifyKind(start.ToDateTime().Date, DateTimeKind.Utc);
            var eExclusiveUtc = DateTime.SpecifyKind(end.ToDateTime().Date.AddDays(1), DateTimeKind.Utc);

            return query.Where(BuildNullableDateTimeHalfOpen(utcTimestampSelector, sUtc, eExclusiveUtc));
        }

        // -----------------------------
        // Expression builders (EF translatable)
        // -----------------------------

        private static Expression<Func<T, bool>> BuildDateOnlyBetween<T>(
            Expression<Func<T, DateOnly>> selector,
            DateOnly start,
            DateOnly end)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;

            var ge = Expression.GreaterThanOrEqual(body, Expression.Constant(start));
            var le = Expression.LessThanOrEqual(body, Expression.Constant(end));

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(ge, le), p);
        }

        private static Expression<Func<T, bool>> BuildNullableDateOnlyBetween<T>(
            Expression<Func<T, DateOnly?>> selector,
            DateOnly start,
            DateOnly end)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;

            var hasValue = Expression.Property(body, "HasValue");
            var value = Expression.Property(body, "Value");

            var ge = Expression.GreaterThanOrEqual(value, Expression.Constant(start));
            var le = Expression.LessThanOrEqual(value, Expression.Constant(end));

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(hasValue, Expression.AndAlso(ge, le)), p);
        }

        private static Expression<Func<T, bool>> BuildDateTimeHalfOpen<T>(
            Expression<Func<T, DateTime>> selector,
            DateTime startUtc,
            DateTime endExclusiveUtc)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;

            var ge = Expression.GreaterThanOrEqual(body, Expression.Constant(startUtc));
            var lt = Expression.LessThan(body, Expression.Constant(endExclusiveUtc));

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(ge, lt), p);
        }

        private static Expression<Func<T, bool>> BuildNullableDateTimeHalfOpen<T>(
            Expression<Func<T, DateTime?>> selector,
            DateTime startUtc,
            DateTime endExclusiveUtc)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;

            var hasValue = Expression.Property(body, "HasValue");
            var value = Expression.Property(body, "Value");

            var ge = Expression.GreaterThanOrEqual(value, Expression.Constant(startUtc));
            var lt = Expression.LessThan(value, Expression.Constant(endExclusiveUtc));

            return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(hasValue, Expression.AndAlso(ge, lt)), p);
        }

        /// <summary>
        /// Applies an inclusive calendar period filter to the query using the provided
        /// <see cref="CalendarDate"/> bounds.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="dateSelector">Expression selecting a <see cref="DateOnly"/> property.</param>
        /// <param name="start">
        /// Optional start boundary (inclusive). If provided, records must satisfy:
        /// <c>date &gt;= start</c>.
        /// </param>
        /// <param name="end">
        /// Optional end boundary (inclusive). If provided, records must satisfy:
        /// <c>date &lt;= end</c>.
        /// </param>
        /// <returns>
        /// The original query with period constraints applied.
        /// If both <paramref name="start"/> and <paramref name="end"/> are null,
        /// the query is returned unchanged.
        /// </returns>
        /// <remarks>
        /// This method represents true calendar semantics.
        /// No time-of-day adjustments are performed.
        /// </remarks>
        public static IQueryable<T> ApplyPeriod<T>(
    this IQueryable<T> query,
    Expression<Func<T, DateOnly>> dateSelector,
    CalendarDate? start,
    CalendarDate? end)
        {
            if (start.HasValue)
            {
                var s = DateOnly.FromDateTime(start.Value.ToDateTime());
                query = query.Where(BuildDateOnlyStartInclusive(dateSelector, s));
            }

            if (end.HasValue)
            {
                var e = DateOnly.FromDateTime(end.Value.ToDateTime());
                query = query.Where(BuildDateOnlyEndInclusive(dateSelector, e));
            }

            return query;
        }

        private static Expression<Func<T, bool>> BuildDateOnlyStartInclusive<T>(
    Expression<Func<T, DateOnly>> selector,
    DateOnly start)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;

            var ge = Expression.GreaterThanOrEqual(body, Expression.Constant(start));
            return Expression.Lambda<Func<T, bool>>(ge, p);
        }

        private static Expression<Func<T, bool>> BuildDateOnlyEndInclusive<T>(
            Expression<Func<T, DateOnly>> selector,
            DateOnly end)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;

            var le = Expression.LessThanOrEqual(body, Expression.Constant(end));
            return Expression.Lambda<Func<T, bool>>(le, p);
        }

        /// <summary>
        /// Applies an inclusive calendar period filter to a UTC timestamp property.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The source query.</param>
        /// <param name="utcTimestampSelector">Expression selecting a UTC <see cref="DateTime"/> property.</param>
        /// <param name="start">
        /// Optional start boundary (inclusive). If provided, records must satisfy:
        /// <c>timestamp &gt;= start at 00:00:00Z</c>.
        /// </param>
        /// <param name="end">
        /// Optional end boundary (inclusive of the entire end day).
        /// Internally implemented as:
        /// <c>timestamp &lt; (end + 1 day) at 00:00:00Z</c>
        /// to ensure all times occurring on the end date are included.
        /// </param>
        /// <returns>
        /// The original query with period constraints applied.
        /// If both <paramref name="start"/> and <paramref name="end"/> are null,
        /// the query is returned unchanged.
        /// </returns>
        /// <remarks>
        /// This method assumes the underlying <see cref="DateTime"/> values represent UTC instants.
        /// It intentionally uses a half-open interval for the upper bound to avoid
        /// precision and time-of-day ambiguity.
        /// </remarks>
        public static IQueryable<T> ApplyPeriod<T>(this IQueryable<T> query, Expression<Func<T, DateTime>> utcTimestampSelector, CalendarDate? start, CalendarDate? end)
        {
            if (start.HasValue)
            {
                var sUtc = DateTime.SpecifyKind(start.Value.ToDateTime().Date, DateTimeKind.Utc);
                query = query.Where(BuildDateTimeStartInclusive(utcTimestampSelector, sUtc));
            }

            if (end.HasValue)
            {
                var eExclusiveUtc = DateTime.SpecifyKind(end.Value.ToDateTime().Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(BuildDateTimeEndExclusive(utcTimestampSelector, eExclusiveUtc));
            }

            return query;
        }

        private static Expression<Func<T, bool>> BuildDateTimeStartInclusive<T>(Expression<Func<T, DateTime>> selector, DateTime startUtc)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;
            var ge = Expression.GreaterThanOrEqual(body, Expression.Constant(startUtc));
            return Expression.Lambda<Func<T, bool>>(ge, p);
        }

        private static Expression<Func<T, bool>> BuildDateTimeEndExclusive<T>(
            Expression<Func<T, DateTime>> selector,
            DateTime endExclusiveUtc)
        {
            var p = selector.Parameters[0];
            var body = selector.Body;
            var lt = Expression.LessThan(body, Expression.Constant(endExclusiveUtc));
            return Expression.Lambda<Func<T, bool>>(lt, p);
        }
    }
}