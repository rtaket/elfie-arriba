﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Arriba;
using System;
using System.Collections;
using System.Collections.Generic;

namespace V5
{
    // TODO:
    //  - Can I take LowestWealth tracking out of insert loop?
    //  - Remove 'WealthVariance'

    /// <summary>
    ///  HashSet5 is a HashSet using Robin Hood hashing to provide good insert and search performance
    ///  with much lower memory use than .NET HashSet.
    ///  
    ///  HashSet5 adds one byte of overhead and stays >= 75% full for large sizes;
    ///  HashSet has 8 bytes overhead [cached hashcode and next node] and resizes more each time.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HashSet5<T> : IEnumerable<T> where T : IEquatable<T>
    {
        public int Count { get; private set; }
        private T[] Values;
        private byte[] Wealth;
        private int LowestWealth;

        public HashSet5(int capacity = 16)
        {
            Reset(capacity + (capacity >> 3) + 1);
        }

        private void Reset(int size)
        {
            this.Values = new T[size];
            this.Wealth = new byte[size];

            this.Count = 0;
            this.LowestWealth = 255;
        }

        public void Clear()
        {
            Array.Clear(this.Wealth, 0, this.Wealth.Length);
            Array.Clear(this.Values, 0, this.Values.Length);

            this.Count = 0;
            this.LowestWealth = 255;
        }

        // Find the average distance items are from their target buckets. Debuggability.
        public double DistanceMean()
        {
            ulong distance = 0;
            for(int i = 0; i < this.Wealth.Length; ++i)
            {
                if (this.Wealth[i] > 0) distance += (ulong)(255 - this.Wealth[i]);
            }

            return ((double)distance / (double)this.Count);
        }

        // Return the counts of distance from desired bucket for each item. Debuggability.
        public int[] DistanceDistribution()
        {
            int[] result = new int[256];
            for (int i = 0; i < this.Wealth.Length; ++i)
            {
                if (this.Wealth[i] > 0)
                {
                    result[255 - this.Wealth[i]]++;
                }
            }

            return result;
        }

        private uint Bucket(uint hash)
        {
            // Use Lemire method to convert hash [0, 2^32) to [0, N) without modulus.
            // If hash is [0, 2^32), then N*hash is [0, N*2^32], and (N*hash)/2^32 is [0, N).
            // NOTE: This method uses the top bits of the hash only, so small integers with GetHashCode(i) == i will perform terribly.
            return (uint)(((ulong)hash * (ulong)this.Wealth.Length) >> 32);
        }

        private uint NextBucket(uint bucket)
        {
            // Sequential Probing - bad variance but fast due to cache coherency
            if (++bucket >= this.Wealth.Length) bucket = 0;
            return bucket;
        }

        /// <summary>
        ///  Return whether this set contains the given value.
        /// </summary>
        /// <param name="value">Value to find</param>
        /// <returns>True if in set, False otherwise</returns>
        public bool Contains(T value)
        {
            return IndexOf(value) != -1;
        }

        private int IndexOf(T value)
        {
            uint hash = (uint)value.GetHashCode();
            uint bucket = Bucket(hash);

            // To find a value, just compare every value starting with the expected bucket
            // up to the farthest any value had to be moved from the desired bucket.
            for (int wealth = 255; wealth >= this.LowestWealth; --wealth)
            {
                if (this.Values[bucket].Equals(value)) return (int)bucket;
                bucket = NextBucket(bucket);
            }

            return -1;
        }

        /// <summary>
        ///  Remove the given value from the set.
        /// </summary>
        /// <param name="value">Value to remove</param>
        /// <returns>True if removed, False if not found</returns>
        public bool Remove(T value)
        {
            int index = IndexOf(value);
            if (index == -1) return false;

            // To remove a value, just clear the value and wealth.
            // Searches don't stop on empty buckets, so this is safe.
            this.Wealth[index] = 0;
            this.Values[index] = default(T);
            this.Count--;

            return true;
        }

        /// <summary>
        ///  Add the given value to the set.
        /// </summary>
        /// <param name="value">Value to add</param>
        /// <returns>True if added, False if value was already in set</returns>
        public bool Add(T value)
        {
            // If the table is more than 7/8 full, expand it. 
            // Very full tables cause slower inserts as many items are shifted.
            if (this.Count >= (this.Wealth.Length - (this.Wealth.Length >> 3))) Expand();

            uint hash = (uint)value.GetHashCode();
            uint bucket = Bucket(hash);

            for(byte wealth = 255; wealth > 0; --wealth)            
            {
                byte wealthFound = this.Wealth[bucket];

                if (wealthFound == 0)
                {
                    // If we found an empty cell (wealth zero), add the item and return
                    this.Wealth[bucket] = wealth;
                    this.Values[bucket] = value;
                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;
                    this.Count++;

                    return true;
                }
                else if (wealthFound >= wealth)
                {
                    // If we found an item with a higher wealth, put the new item here and move the existing one
                    T valueMoved = this.Values[bucket];

                    // If this is a duplicate of the new item, stop
                    if (valueMoved.Equals(value)) return false;

                    this.Wealth[bucket] = wealth;
                    this.Values[bucket] = value;
                    if (wealth < this.LowestWealth) this.LowestWealth = wealth;

                    value = valueMoved;
                    wealth = wealthFound;
                }

                bucket = NextBucket(bucket);
            }

            // If we had to move an item more than 255 from the desired bucket, we need to resize
            Expand();

            // If we resized, re-add the new value (recalculating the bucket for the new size)
            return Add(value);
        }

        private void Expand()
        {
            // Expand the array to 1.5x the current size up to 1M items, 1.125x the current size thereafter
            int sizeShiftAmount = (this.Wealth.Length >= 1048576 ? 3 : 1);
            int newSize = this.Wealth.Length + (this.Wealth.Length >> sizeShiftAmount);

            // Save the current contents
            T[] oldValues = this.Values;
            byte[] oldWealth = this.Wealth;

            // Allocate the larger table
            Reset(newSize);

            // Add items to the enlarged table
            for (int i = 0; i < oldWealth.Length; ++i)
            {
                if (oldWealth[i] > 0) Add(oldValues[i]);
            }
        }

        public struct HashSet5Enumerator<U> : IEnumerator<U> where U : IEquatable<U>
        {
            private HashSet5<U> Set;
            private int NextBucket;

            public U Current => this.Set.Values[this.NextBucket];
            object IEnumerator.Current => this.Set.Values[this.NextBucket];

            public HashSet5Enumerator(HashSet5<U> set)
            {
                this.Set = set;
                this.NextBucket = -1;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                while(++this.NextBucket < this.Set.Wealth.Length)
                {
                    if (this.Set.Wealth[this.NextBucket] > 0) return true;
                }

                return false;
            }

            public void Reset()
            {
                this.NextBucket = 0;
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new HashSet5Enumerator<T>(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new HashSet5Enumerator<T>(this);
        }
    }
}