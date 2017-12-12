using Microsoft.CodeAnalysis.Elfie.Model.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XForm.Commands.Functions;
using XForm.Data;

namespace XForm.Commands.Transformers
{
    public class EmptyToDefaultTransformer : IColumnValueTransformer
    {
        String8[] _transformedArray;

        public DataBatch Transform(DataBatch batch, string[] arguments)
        {
            if (arguments.Length != 1)
            {
                throw new ArgumentOutOfRangeException($"EmptyToDefault tranform requires one additional argument to specify the default value to substitute for empty. Expected 1. Actual {arguments.Length}.");
            }

            byte[] defaultStringAsBytes = UTF8Encoding.UTF8.GetBytes(arguments[0]);
            String8 defaultValue = new String8(defaultStringAsBytes, 0, defaultStringAsBytes.Length);
            Allocator.AllocateToSize(ref _transformedArray, batch.Count);

            String8[] sourceArray = (String8[])batch.Array;
            for (int i = 0; i < batch.Count; ++i)
            {
                String8 originalValue = sourceArray[batch.Index(i)];

                if (originalValue == null || originalValue.IsEmpty())
                {
                    _transformedArray[i] = defaultValue;
                }
                else
                {
                    _transformedArray[i] = originalValue;
                }
            }

            return DataBatch.All(_transformedArray, batch.Count);
        }
    }
}
