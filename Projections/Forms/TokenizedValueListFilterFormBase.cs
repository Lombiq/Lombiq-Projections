﻿using Orchard.ContentManagement;
using Orchard.DisplayManagement;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.Services;
using System;
using System.Linq;

namespace Lombiq.Projections.Projections.Forms
{
    public abstract class TokenizedValueListFilterFormBase : IFormProvider
    {
        private readonly dynamic _shapeFactory;

        public Localizer T { get; set; }


        public TokenizedValueListFilterFormBase(IShapeFactory shapeFactory)
        {
            _shapeFactory = shapeFactory;

            T = NullLocalizer.Instance;
        }


        // This won't do anything, just here for demonstation.
        public virtual void Describe(DescribeContext context) =>
            context.Form(nameof(TokenizedValueListFilterFormBase), shape =>
                _shapeFactory.Form(
                    Id: nameof(TokenizedValueListFilterFormBase),
                    _MatchOrNoMatch: GetMatchRadioFieldSet(),
                    _Value: GetValueTextbox(),
                    _Relationship: GetFilterRelationshipTextbox()));


        public virtual dynamic GetMatchRadioFieldSet() => _shapeFactory.FieldSet(
            _Match: _shapeFactory.Radio(
                Id: "match", Name: nameof(TokenizedValueListFilterFormElementsBase.Matches),
                Title: T("Value(s) match(es)"), Value: "true", Checked: true),
            _NoMatch: _shapeFactory.Radio(
                Id: "noMatch", Name: nameof(TokenizedValueListFilterFormElementsBase.Matches),
                Title: T("Value(s) do(es)n't match"), Value: "false"));

        public virtual dynamic GetValueTextbox() => _shapeFactory.Textbox(
            Id: "valueString", Name: nameof(TokenizedValueListFilterFormElementsBase.ValueString),
            Classes: new[] { "text", "medium", "tokenized" },
            Title: T("Value(s)"),
            Description: T("The optionally tokenized comma-separated list of values."));

        public virtual dynamic GetFilterRelationshipTextbox() => _shapeFactory.Textbox(
            Id: "filterRelationshipString", Name: nameof(TokenizedValueListFilterFormElementsBase.FilterRelationshipString),
            Classes: new[] { "text", "medium", "tokenized" },
            Title: T("Filter relationship"),
            Description: T("Defines the operator between the filters of individual values. Accepted values: {0}. Default value: \"{1}\".",
                string.Join(", ", Enum.GetNames(typeof(ValueFilterRelationship))),
                ValueFilterRelationship.Or.ToString()));
    }


    public enum ValueFilterRelationship
    {
        Or,
        And
    }


    public abstract class TokenizedValueListFilterFormElementsBase
    {
        public dynamic FormState { get; set; }

        public bool Matches { get; set; }
        public string FilterRelationshipString { get; set; }
        public ValueFilterRelationship FilterRelationship { get; set; }
        public string ValueString { get; set; }
        public string[] Values { get; set; }


        public TokenizedValueListFilterFormElementsBase(dynamic formState)
        {
            FormState = formState;

            Matches = FormState[nameof(Matches)] ?? true;
            FilterRelationshipString = FormState[nameof(FilterRelationshipString)];
            FilterRelationship = Enum.TryParse(FilterRelationshipString, out ValueFilterRelationship filterRelationship) ?
                filterRelationship : ValueFilterRelationship.Or;
            ValueString = FormState[nameof(ValueString)];
            Values = string.IsNullOrEmpty(ValueString) ?
                new string[] { } :
                ValueString.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Distinct().Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();
        }


        public virtual string[] GetValuesFromJsonString(IJsonConverter jsonConverter = null) =>
            string.IsNullOrEmpty(ValueString) ?
                new string[] { } :
                // If the value string is not a JSON array, then it's probably a single value or a comma-separated list.
                ValueString.StartsWith("[") && ValueString.EndsWith("]") &&
                    jsonConverter != null && jsonConverter.TryDeserialize<string[]>(ValueString, out var values) ?
                        values.Distinct().Where(v => !string.IsNullOrWhiteSpace(v)).ToArray() : Values;

        public virtual Action<IHqlExpressionFactory> GetFilterExpression(string property, string value = "")
        {
            if (Matches) return expression => expression.Eq(property, value);
            else return expression => expression.Not(inner => inner.Eq(property, value));
        }
    }
}