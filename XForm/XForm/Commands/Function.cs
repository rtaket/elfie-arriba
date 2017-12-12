// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Reflection;
using XForm.Commands.Transformers;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Commands
{
    /// <summary>
    /// Runs a function over a set of input columns to produce a result column
    /// </summary>
    internal class FunctionCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "function", "func" };
        public string Usage => "'function' <FunctionName> <ResultColumnName> <InputColumnName> [, InputColumnName] [, ...]";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            string functionName = context.Parser.NextString();
            MethodInfo methodInfo;

            if (!this.TryGetMethodInfo(functionName, out methodInfo))
            {
                throw new InvalidOperationException("Unknown function name: " + functionName);
            }

            string destinationColumnName = context.Parser.NextString();

            List<string> inputColumnNames = new List<string>();
            do
            {
                inputColumnNames.Add(context.Parser.NextColumnName(source));
            } while (context.Parser.HasAnotherPart);

            return new Function(source, methodInfo, destinationColumnName, inputColumnNames);
        }

        private bool TryGetMethodInfo(string methodFullName, out MethodInfo methodInfo)
        {
            methodInfo = default(MethodInfo);
            int periodIndex = methodFullName.LastIndexOf('.');

            if (periodIndex > 0)
            {
                string fullyQualifiedTypeName = methodFullName.Substring(0, periodIndex);
                string methodName = methodFullName.Substring(periodIndex + 1);

                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    Type type = assembly.GetType(fullyQualifiedTypeName);

                    if (type != null)
                    {
                        methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                    }
                }
            }

            return methodInfo != default(MethodInfo);
        }
    }

    public class Function : DataBatchEnumeratorWrapper
    {
        private List<int> _sourceColumnIndexes;
        private int _destinationColumnIndex;
        private List<ColumnDetails> _updatedColumnList;
        private MethodInfo _methodInfo;

        public override IReadOnlyList<ColumnDetails> Columns => _updatedColumnList;

        public Function(IDataBatchEnumerator source, MethodInfo methodInfo, string destinationColumnName, IEnumerable<string> inputColumnNames) : base(source)
        {
            _sourceColumnIndexes = new List<int>();
            foreach (string inputColumnName in inputColumnNames)
            {
                _sourceColumnIndexes.Add(source.Columns.IndexOfColumn(inputColumnName));
            }

            _methodInfo = methodInfo;
            _updatedColumnList = new List<ColumnDetails>(source.Columns);
            _updatedColumnList.Add(new ColumnDetails(destinationColumnName, typeof(String8), true));
            _destinationColumnIndex = _updatedColumnList.IndexOfColumn(destinationColumnName);
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _destinationColumnIndex) return _source.ColumnGetter(columnIndex);

            return () => (DataBatch)_methodInfo.Invoke(null, new object[] { _source, _sourceColumnIndexes.ToArray() });
        }
    }
}
