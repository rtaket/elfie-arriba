using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using XForm.Commands;
using XForm.Data;
using XForm.Types;

namespace XForm.Functions.String
{
    internal class IndexOfBuilder : IFunctionBuilder
    {
        public string Name => "IndexOf";
        public string Usage => "IndexOf(Column inputColumn, String value, Integer startIndex = 0)";

        public IDataBatchColumn Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            IDataBatchColumn column = context.Parser.NextColumn(source, context);
            string value = context.Parser.NextString();
            String8 value8 = String8.Convert(value, new byte[String8.GetLength(value)]);
            int startIndex = 0;

            if (context.Parser.HasAnotherArgument)
            {
                startIndex = context.Parser.NextInteger();
            }

            return SimpleTransformFunction<String8, int>.Build(Name, source, column, (text) =>
            {
                return text.IndexOf(value8, startIndex);
            });
        }
    }
}
