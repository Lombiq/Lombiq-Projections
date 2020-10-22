using Orchard.DisplayManagement;
using Orchard.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedBooleanValueListFilterForm : TokenizedValueListFilterFormBase
    {
        public TokenizedBooleanValueListFilterForm(IShapeFactory shapeFactory) : base(shapeFactory) { }
    }


    public class TokenizedBooleanValueListFilterFormElements : TokenizedValueListFilterFormElementsBase
    {
        public TokenizedBooleanValueListFilterFormElements(object formState) : base(formState) { }


        public override string[] GetValuesFromJsonString(IJsonConverter jsonConverter = null) =>
            base.GetValuesFromJsonString(jsonConverter)
                .Select(value => value.ToNullableBoolean())
                .Where(value => value != null)
                .Select(value => value == true ? "1" : "0")
                .Distinct()
                .ToArray();
    }
}