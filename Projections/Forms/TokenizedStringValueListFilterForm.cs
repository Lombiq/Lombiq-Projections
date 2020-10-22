using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using System;

namespace Lombiq.Projections.Projections.Forms
{
    public class TokenizedStringValueListFilterForm : TokenizedValueListFilterFormBase
    {
        private readonly dynamic _shapeFactory;


        public TokenizedStringValueListFilterForm(IShapeFactory shapeFactory) : base(shapeFactory)
        {
            _shapeFactory = shapeFactory;
        }


        public override void Describe(DescribeContext context) =>
            context.Form(nameof(TokenizedStringValueListFilterForm), shape =>
                _shapeFactory.Form(
                    Id: nameof(TokenizedStringValueListFilterForm),
                    _MatchOrNoMatch: GetMatchRadioFieldSet(),
                    _StringOperator: _shapeFactory.FieldSet(
                        _Equals: _shapeFactory.Radio(
                            Id: "equals", Name: nameof(TokenizedStringValueListFilterFormElements.StringOperatorString),
                            Title: T("Value(s) equal(s)"), Value: StringOperator.Equals, Checked: true),
                        _ContainedIn: _shapeFactory.Radio(
                            Id: "containedIn", Name: nameof(TokenizedStringValueListFilterFormElements.StringOperatorString),
                            Title: T("Value(s) is/are contained in"), Value: StringOperator.ContainedIn)),
                    _Value: GetValueTextbox(),
                    _Relationship: GetFilterRelationshipTextbox()));
    }


    public enum StringOperator
    {
        Equals,
        ContainedIn
    }


    public class TokenizedStringValueListFilterFormElements : TokenizedValueListFilterFormElementsBase
    {
        public string StringOperatorString { get; }
        public StringOperator StringOperator { get; }


        public TokenizedStringValueListFilterFormElements(object formState) : base(formState)
        {
            StringOperatorString = ((dynamic)formState)[nameof(StringOperatorString)];
            StringOperator = Enum.TryParse(StringOperatorString, out StringOperator stringOperator) ?
                stringOperator : StringOperator.Equals;
        }


        public override void GetFilterExpression(IHqlExpressionFactory expression, string property, string value = "")
        {
            switch (StringOperator)
            {
                case StringOperator.ContainedIn:
                    if (Matches) expression.Like(property, Convert.ToString(value), HqlMatchMode.Anywhere);
                    else expression.Not(inner => inner.Like(property, Convert.ToString(value), HqlMatchMode.Anywhere));

                    break;
                case StringOperator.Equals:
                default:
                    if (Matches) expression.Eq(property, value);
                    else expression.Not(inner => inner.Eq(property, value));

                    break;
            }
        }
    }
}