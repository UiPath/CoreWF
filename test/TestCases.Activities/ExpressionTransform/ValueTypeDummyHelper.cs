// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;

namespace TestCases.Activities.ExpressionTransform
{
    public struct ValueTypeDummyHelper
    {
        private int[] _indexerField;

        private int[] IndexerProperty
        {
            get
            {
                if (_indexerField == null)
                    _indexerField = new int[] { 1, 3, 5, 7, 9, 11, 13, 15, 17, 19 };

                return _indexerField;
            }
        }

        public static int StaticInt32Field = 121;
        public static int StaticInt32Property { get; set; }

        public int Int32Field;
        public String StringProperty { get; set; }
        public FileAccess EnumProperty { get; set; }
        public FileAccess EnumField;

        private int[][][,] _jaggedIntArrayOfMultiDimensionalArray;
        public int[][][,] JaggedArrayOfMultiDimensionalArrayProperty
        {
            get
            {
                if (_jaggedIntArrayOfMultiDimensionalArray == null)
                {
                    _jaggedIntArrayOfMultiDimensionalArray = new int[3][][,];
                    for (int i = 0; i < 3; i++)
                    {
                        _jaggedIntArrayOfMultiDimensionalArray[i] = new int[3][,];
                        for (int j = 0; j < 3; j++)
                        {
                            _jaggedIntArrayOfMultiDimensionalArray[i][j] = new int[3, 3];
                        }
                    }
                }

                return _jaggedIntArrayOfMultiDimensionalArray;
            }
        }

        private string[][] _twoDimJaggedStringArray;
        public string[][] TwoDimJaggedStringArrayProperty
        {
            get
            {
                if (_twoDimJaggedStringArray == null)
                {
                    _twoDimJaggedStringArray = new string[3][];
                    _twoDimJaggedStringArray[1] = new string[1];
                    _twoDimJaggedStringArray[2] = new string[2];
                    _twoDimJaggedStringArray[3] = new string[3];
                }

                return _twoDimJaggedStringArray;
            }
        }


        private int[][][] _threeDimJaggedIntArray;
        public int[][][] ThreeDimJaggedIntArrayProperty
        {
            get
            {
                if (_threeDimJaggedIntArray == null)
                {
                    _threeDimJaggedIntArray = new int[3][][];

                    for (int i = 0; i < 3; i++)
                    {
                        _threeDimJaggedIntArray[i] = new int[3][];
                        for (int j = 0; j < 3; j++)
                        {
                            _threeDimJaggedIntArray[i][j] = new int[3];
                        }
                    }
                }

                return _threeDimJaggedIntArray;
            }
        }

        private string[,,] _threeDimStringArray;
        public string[,,] ThreeDimStringArrayProperty
        {
            get
            {
                if (_threeDimStringArray == null)
                    _threeDimStringArray = new string[3, 3, 3];
                return _threeDimStringArray;
            }
        }

        public int this[int index]    // Indexer declaration
        {
            get
            {
                return IndexerProperty[index];
            }
            set
            {
                IndexerProperty[index] = value;
            }
        }

        public int this[int index1, int index2]
        {
            get
            {
                return IndexerProperty[index1 + index2];
            }
            set
            {
                IndexerProperty[index1 + index2] = value;
            }
        }

        public int this[int index1, float index2]
        {
            get
            {
                return IndexerProperty[index1 + (int)index2];
            }
            set
            {
                IndexerProperty[index1 + (int)index2] = value;
            }
        }
    }
}
