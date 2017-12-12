﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace XForm.IO
{
    public static class DirectoryIO
    {
        public static void DeleteAllContents(string directoryPath)
        {
            if (!Directory.Exists(directoryPath)) return;

            foreach (string filePath in Directory.GetFiles(directoryPath))
            {
                File.Delete(filePath);
            }

            foreach (string subdirectoryPath in Directory.GetDirectories(directoryPath))
            {
                Directory.Delete(subdirectoryPath, true);
            }
        }

        public static void Copy(string directoryPath, string destination)
        {
            if (!Directory.Exists(directoryPath)) throw new IOException($"{directoryPath} did not exist to copy from.");
            Directory.CreateDirectory(destination);

            foreach (string filePath in Directory.GetFiles(directoryPath))
            {
                File.Copy(filePath, Path.Combine(destination, Path.GetFileName(filePath)));
            }

            foreach (string subdirectoryPath in Directory.GetDirectories(directoryPath))
            {
                Copy(subdirectoryPath, Path.Combine(destination, Path.GetFileName(subdirectoryPath)));
            }
        }
    }
}
