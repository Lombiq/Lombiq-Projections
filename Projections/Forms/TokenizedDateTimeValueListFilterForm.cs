using Orchard.ContentManagement;
using Orchard.Projections.FilterEditors.Forms;
using Orchard.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    // This is just a placeholder for now, use DateTimeFilterForm from Orchard.Projections instead.
    //public class TokenizedDateTimeValueListFilterForm : TokenizedValueListFilterFormBase
    //{
    //    private readonly dynamic _shapeFactory;


    //    public TokenizedDateTimeValueListFilterForm(IShapeFactory shapeFactory) : base(shapeFactory)
    //    {
    //        _shapeFactory = shapeFactory;
    //    }
    //}


    public class TokenizedDateTimeValueListFilterFormElements : TokenizedValueListFilterFormElementsBase
    {
        private readonly IClock _clock;


        public TokenizedDateTimeValueListFilterFormElements(object formState, IClock clock) : base(formState)
        {
            _clock = clock;

            ValueString = FormState["Value"];
            Values = string.IsNullOrEmpty(ValueString) ?
                new string[] { } :
                ValueString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
        }


        public override Action<IHqlExpressionFactory> GetFilterExpression(string property, string value = "") =>
            DateTimeFilterForm.GetFilterPredicate(FormState, property, _clock.UtcNow, false);
    }
}