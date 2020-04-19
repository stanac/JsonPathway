using JsonPathway.Internal;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway
{
    public abstract class Expression
    {
        
    }

    public class PropertyAccessExpression: Expression
    {
        public List<string> Properties { get; }
        public bool ChildProperties { get; }
        public bool RecursiveProperties { get; }

        public PropertyAccessExpression(PropertyToken token)
        {
            Properties = new List<string>
            {
                token.StringValue
            };
        }

        public PropertyAccessExpression(MultiplePropertiesToken token)
        {
            Properties = token.Properties.ToList();
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        public PropertyAccessExpression(RecursivePropertiesToken token)
        {
            RecursiveProperties = true;
        }

        public PropertyAccessExpression(ChildPropertiesToken token)
        {
            ChildProperties = true;
        }
    }

    public class ArrayElementsExpression : Expression
    {
        public int? SliceStart { get; }
        public int? SliceEnd { get; }
        public int? SliceStep { get; }

        public int[] Indexes { get; }

        public bool AllElements { get; }

        public ArrayElementsExpression(AllArrayElementsToken token)
        {
            AllElements = true;
        }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.

        public ArrayElementsExpression(ArrayElementsToken token)
        {
            SliceStart = token.SliceStart;
            SliceEnd = token.SliceEnd;
            SliceStep = token.SliceStep;

            Indexes = token.ExactElementsAccess;
        }
    }

    public class FilterExpression : Expression
    {
        public FilterExpression(FilterToken token)
        {
            throw new NotImplementedException();
        }
    }
}
