// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using XForm.Commands.Transformers;
using XForm.Data;
using XForm.Extensions;
using XForm.Query;

namespace XForm.Commands
{
    internal class String8TransformCommandBuilder : IPipelineStageBuilder
    {
        public IEnumerable<string> Verbs => new string[] { "string8transform" };
        public string Usage => "'string8transform' <TransformName> <ColumnName> <ConvertedColumnName>";

        public IDataBatchEnumerator Build(IDataBatchEnumerator source, WorkflowContext context)
        {
            string transformName = context.Parser.NextString();

            IColumnValueTransformer transformer;
            if (transformName.Equals("cosmosnulltoempty", StringComparison.OrdinalIgnoreCase))
            {
                transformer = new CosmosNullToEmptyTransformer();
            }
            else
            {
                throw new InvalidOperationException("Unknown transformer: " + transformName);
            }

            string sourceColumnName = context.Parser.NextColumnName(source);
            string destinationColumnName = context.Parser.NextString();

            return new String8Transform(source, transformer, sourceColumnName, destinationColumnName);
        }
    }

    public class String8Transform : DataBatchEnumeratorWrapper
    {
        private int _sourceColumnIndex;
        private int _destinationColumnIndex;
        private List<ColumnDetails> _updatedColumnList;
        private IColumnValueTransformer _transformer;

        public override IReadOnlyList<ColumnDetails> Columns => _updatedColumnList;

        public String8Transform(IDataBatchEnumerator source, IColumnValueTransformer transformer, string sourceColumnName, string destinationColumnName) : base(source)
        {
            _sourceColumnIndex = source.Columns.IndexOfColumn(sourceColumnName);

            _transformer = transformer;
            _updatedColumnList = new List<ColumnDetails>(source.Columns);
            _updatedColumnList.Add(new ColumnDetails(destinationColumnName, source.Columns[_sourceColumnIndex].Type, true));
            _destinationColumnIndex = _updatedColumnList.IndexOfColumn(destinationColumnName);

            if (source.Columns[_sourceColumnIndex].Type != typeof(String8))
            {
                throw new InvalidOperationException($"Unexpected source column type. Expected String8. Actual {source.Columns[_sourceColumnIndex].Type.Name}");
            }
        }

        public override Func<DataBatch> ColumnGetter(int columnIndex)
        {
            // Pass through columns other than the one being converted
            if (columnIndex != _destinationColumnIndex) return _source.ColumnGetter(columnIndex);

            // Source column wasn't a string so there's no transform to perform
            if (_transformer == null) return _source.ColumnGetter(_sourceColumnIndex);

            // Cache the function to get the source data
            Func<DataBatch> sourceGetter = _source.ColumnGetter(_sourceColumnIndex);

            return () => _transformer.Transform(sourceGetter());
        }
    }
}
