using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedBooleanValueListFilterForm : TokenizedValueListFilterFormBase
    {
        private readonly dynamic _shapeFactory;


        public TokenizedBooleanValueListFilterForm(IShapeFactory shapeFactory) : base(shapeFactory)
        {
            _shapeFactory = shapeFactory;
        }


        public override void Describe(DescribeContext context) =>
            context.Form(nameof(TokenizedBooleanValueListFilterForm), shape =>
                _shapeFactory.Form(
                    Id: nameof(TokenizedBooleanValueListFilterForm),
                    _MatchOrNoMatch: GetMatchRadioFieldSet(),
                    _Value: GetValueTextbox(),
                    _Relationship: GetFilterRelationshipTextbox()));
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