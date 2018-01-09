using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Commands;
using XForm.Data;
using XForm.Types;

namespace XForm.Functions.String
{
    internal class SplitBuilder : IFunctionBuilder
    {
        public string Name => "Split";
        public string Usage => "Split(Column inputColumn, Character separator, Integer indexToReturn, String defaultValue = String.Empty)";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            int[] positionArray = null;

            IDataBatchColumn column = context.Parser.NextColumn(source, context);
            string separator = context.Parser.NextString();

            if (separator.Length != 1)
            {
                throw new ArgumentOutOfRangeException("The separator argument must be a single character.");
            }

            Char separatorChar = separator[0];
            int indexToReturn = context.Parser.NextInteger();

            if (indexToReturn < 0)
            {
                throw new ArgumentOutOfRangeException("The indexToReturn argument must be greater than or equal to zero.");
            }

            String8 defaultValue = String8.Empty;

            if (context.Parser.HasAnotherArgument)
            {
                string defaultString = context.Parser.NextString();
                defaultValue = String8.Convert(defaultString, new byte[String8.GetLength(defaultString)]);
            }

            return SimpleTransformFunction<String8, String8>.Build(Name, source, column, (text) => Split(text, separatorChar, indexToReturn, defaultValue, ref positionArray));
        }

        public static String8 Split(String8 text, char separatorChar, int indexToReturn, String8 defaultValue, ref int[] positionArray)
        {
            Allocator.AllocateToSize(ref positionArray, text.Length);
            String8Set parts = text.Split(separatorChar, positionArray);

            if (parts.Count >= indexToReturn + 1)
            {
                return parts[indexToReturn];
            }
            else
            {
                return defaultValue;
            }
        }
    }
}
