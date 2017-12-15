﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using XForm.Data;

namespace XForm.Extensions
{
    public struct RunResult
    {
        public long RowCount { get; set; }
        public bool IsComplete { get; set; }
        public TimeSpan Timeout { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    public static class DataBatchEnumeratorExtensions
    {
        public const int DefaultBatchSize = 10240;

        public static long Run(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            long rowsWritten = 0;
            while (true)
            {
                int batchCount = pipeline.Next(batchSize);
                if (batchCount == 0) break;
                rowsWritten += batchCount;
            }
            return rowsWritten;
        }

        public static long RunAndDispose(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            using (pipeline)
            {
                return Run(pipeline, batchSize);
            }
        }

        public static RunResult RunUntilTimeout(this IDataBatchEnumerator pipeline, TimeSpan timeout, int batchSize = DefaultBatchSize)
        {
            RunResult result = new RunResult();
            result.Timeout = timeout;

            Stopwatch w = Stopwatch.StartNew();
            while(true)
            {
                int batch = pipeline.Next(batchSize);
                result.RowCount += batch;

                if (batch == 0)
                {
                    result.IsComplete = true;
                    break;
                }

                if (w.Elapsed > timeout) break;
            }

            result.Elapsed = w.Elapsed;
            return result;
        }

        public static T RunAndGetSingleValue<T>(this IDataBatchEnumerator pipeline, int batchSize = DefaultBatchSize)
        {
            Func<DataBatch> getter = pipeline.ColumnGetter(0);
            using (pipeline)
            {
                pipeline.Next(batchSize);

                DataBatch batch = getter();
                T[] array = (T[])(getter().Array);
                return array[batch.Index(0)];
            }
        }

        public static List<T> ToList<T>(this IDataBatchEnumerator pipeline, string columnName)
        {
            List<T> result = new List<T>();

            using (pipeline)
            {
                Func<DataBatch> getter = pipeline.ColumnGetter(pipeline.Columns.IndexOfColumn(columnName));

                while (pipeline.Next(DefaultBatchSize) != 0)
                {
                    DataBatch batch = getter();
                    T[] array = (T[])batch.Array;
                    for (int i = 0; i < batch.Count; ++i)
                    {
                        result.Add(array[batch.Index(i)]);
                    }
                }
            }

            return result;
        }
    }
}
