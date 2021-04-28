using Orchard.ContentManagement;
using Orchard.Projections.FilterEditors.Forms;
using Orchard.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    // This filter doesn't actually need its own form, but the file is named the way it is for consistency.
    public class TokenizedYearSpanRangeValueListFilterFormElements : TokenizedValueListFilterFormElementsBase
    {
        private readonly IClock _clock;


        public TokenizedYearSpanRangeValueListFilterFormElements(object formState, IClock clock) : base(formState)
        {
            _clock = clock;
        }


        public override Action<IHqlExpressionFactory> GetFilterExpression(string property, string value = "")
        {
            var now = _clock.UtcNow.Date;
            var range = value.Split(new char[] { '-' }, StringSplitOptions.RemoveEmptyEntries)
                // Putting an artificial limit on the earliest birth year (1800) for database-compatibility.
                .Select(segment => int.TryParse(segment, out int number) ? Math.Min(number, now.Year - 1800) : -1)
                .Where(number => number >= 0)
                .ToArray();

            if (range.Count() != 2) return null;


            var yearSpanRange = new YearSpanRange
            {
                Min = Math.Min(range[0], range[1]),
                Max = Math.Max(range[0], range[1])
            };

            FormState.Operator = DateTimeOperator.Between;
            FormState.ValueType = 0;
            FormState.Min = now.AddYears(-yearSpanRange.Max).ToIsoDateString();
            FormState.Max = now.AddYears(-yearSpanRange.Min).AddDays(1).ToIsoDateString();

            return DateTimeFilterForm.GetFilterPredicate(FormState, property, _clock.UtcNow, false);
        }
    }

    public class YearSpanRange
    {
        public int Min { get; set; }
        public int Max { get; set; }
    }
}