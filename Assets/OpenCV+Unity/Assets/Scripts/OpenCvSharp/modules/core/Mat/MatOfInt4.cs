using System;
using System.Collections.Generic;
using OpenCvSharp.Util;

namespace OpenCvSharp
{
    /// <summary>
    /// A matrix whose element is 32SC1 (cv::Mat_&lt;int&gt;)
    /// </summary>
    public unsafe class MatOfInt4 : Mat<Vec4i, MatOfInt4>
    {
        private static readonly MatType ThisType = MatType.CV_32SC4;
        private const int ThisDepth = MatType.CV_32S;
        private const int ThisChannels = 4;

        #region Init

        // ... (rest of the code remains unchanged)

        #region Indexer
        /// <summary>
        /// Matrix indexer
        /// </summary>
        public sealed class Indexer : MatIndexer<Vec4i>
        {
            private readonly byte* ptr;

            internal Indexer(Mat parent)
                : base(parent)
            {
                this.ptr = (byte*)parent.Data.ToPointer();
            }
            /// <summary>
            /// 1-dimensional indexer
            /// </summary>
            /// <param name="i0">Index along the dimension 0</param>
            /// <returns>A value to the specified array element.</returns>
            public override Vec4i this[int i0]
            {
                get
                {
                    return *(Vec4i*)(ptr + (steps[0] * i0));
                }
                set
                {
                    *(Vec4i*)(ptr + (steps[0] * i0)) = value;
                }
            }
            /// <summary>
            /// 2-dimensional indexer
            /// </summary>
            /// <param name="i0">Index along the dimension 0</param>
            /// <param name="i1">Index along the dimension 1</param>
            /// <returns>A value to the specified array element.</returns>
            public override Vec4i this[int i0, int i1]
            {
                get
                {
                    return *(Vec4i*)(ptr + (steps[0] * i0) + (steps[1] * i1));
                }
                set
                {
                    *(Vec4i*)(ptr + (steps[0] * i0) + (steps[1] * i1)) = value;
                }
            }
            /// <summary>
            /// 3-dimensional indexer
            /// </summary>
            /// <param name="i0">Index along the dimension 0</param>
            /// <param name="i1">Index along the dimension 1</param>
            /// <param name="i2"> Index along the dimension 2</param>
            /// <returns>A value to the specified array element.</returns>
            public override Vec4i this[int i0, int i1, int i2]
            {
                get
                {
                    return *(Vec4i*)(ptr + (steps[0] * i0) + (steps[1] * i1) + (steps[2] * i2));
                }
                set
                {
                    *(Vec4i*)(ptr + (steps[0] * i0) + (steps[1] * i1) + (steps[2] * i2)) = value;
                }
            }
            /// <summary>
            /// n-dimensional indexer
            /// </summary>
            /// <param name="idx">Array of Mat::dims indices.</param>
            /// <returns>A value to the specified array element.</returns>
            public override Vec4i this[params int[] idx]
            {
                get
                {
                    long offset = 0;
                    for (int i = 0; i < idx.Length; i++)
                    {
                        offset += steps[i] * idx[i];
                    }
                    return *(Vec4i*)(ptr + offset);
                }
                set
                {
                    long offset = 0;
                    for (int i = 0; i < idx.Length; i++)
                    {
                        offset += steps[i] * idx[i];
                    }
                    *(Vec4i*)(ptr + offset) = value;
                }
            }
        }
        /// <summary>
        /// Gets a type-specific indexer. The indexer has getters/setters to access each matrix element.
        /// </summary>
        /// <returns></returns>
        public override MatIndexer<Vec4i> GetIndexer()
        {
            return new Indexer(this);
        }
        #endregion

        #region FromArray
#if LANG_JP
        /// <summary>
        /// N x 1 の行列(ベクトル)として初期化し、指定した配列からデータをコピーする
        /// </summary>
        /// <param name="arr">この行列にコピーされるデータ</param>
#else
        /// <summary>
        /// Initializes as N x 1 matrix and copys array data to this
        /// </summary>
        /// <param name="arr">Source array data to be copied to this</param>
#endif
        public static MatOfInt4 FromArray(params Vec4i[] arr)
        {
            if (arr == null)
                throw new ArgumentNullException("nameof(arr)");
            if (arr.Length == 0)
                throw new ArgumentException("arr.Length == 0");

            int numElems = arr.Length;
            var mat = new MatOfInt4();
            mat.Create(numElems, 1, ThisType);
            mat.SetArray(0, 0, arr);
            return mat;
        }
#if LANG_JP
        /// <summary>
        /// M x N の行列として初期化し、指定した配列からデータをコピーする
        /// </summary>
        /// <param name="arr">この行列にコピーされるデータ</param>
#else
        /// <summary>
        /// Initializes as M x N matrix and copys array data to this
        /// </summary>
        /// <param name="arr">Source array data to be copied to this</param>
#endif
        public static MatOfInt4 FromArray(Vec4i[,] arr)
        {
            if (arr == null)
                throw new ArgumentNullException("nameof(arr)");
            if (arr.Length == 0)
                throw new ArgumentException("arr.Length == 0");

            int rows = arr.GetLength(0);
            int cols = arr.GetLength(1);
            var mat = new MatOfInt4();
            mat.Create(rows, cols, ThisType);
            mat.SetArray(0, 0, arr);
            return mat;
        }
#if LANG_JP
        /// <summary>
        /// N x 1 の行列(ベクトル)として初期化し、指定した配列からデータをコピーする
        /// </summary>
        /// <param name="enumerable">この行列にコピーされるデータ</param>
#else
        /// <summary>
        /// Initializes as N x 1 matrix and copys array data to this
        /// </summary>
        /// <param name="enumerable">Source array data to be copied to this</param>
#endif
        public static MatOfInt4 FromArray(IEnumerable<Vec4i> enumerable)
        {
            return FromArray(EnumerableEx.ToArray(enumerable));
        }
        #endregion

        #region ToArray
        /// <summary>
        /// Convert this mat to managed array
        /// </summary>
        /// <returns></returns>
        public override Vec4i[] ToArray()
        {
            long numOfElems = Total();
            if (numOfElems == 0)
                return new Vec4i[0];
            var arr = new Vec4i[numOfElems];
            GetArray(0, 0, arr);
            return arr;
        }
        /// <summary>
        /// Convert this mat to managed rectangular array
        /// </summary>
        /// <returns></returns>
        public override Vec4i[,] ToRectangularArray()
        {
            if (Rows == 0 || Cols == 0)
                return new Vec4i[0, 0];
            var arr = new Vec4i[Rows, Cols];
            GetArray(0, 0, arr);
            return arr;
        }
        #endregion

        #region GetEnumerator
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override IEnumerator<Vec4i> GetEnumerator()
        {
            ThrowIfDisposed();
            var indexer = new Indexer(this);

            int dims = Dims();
            if (dims == 2)
            {
                int rows = Rows;
                int cols = Cols;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        yield return indexer[r, c];
                    }
                }
            }
            else
            {
                throw new NotImplementedException("GetEnumerator supports only 2-dimensional Mat");
            }
        }

        #endregion

        /// <summary>
        /// Adds elements to the bottom of the matrix. (Mat::push_back)
        /// </summary>
        /// <param name="value">Added element(s)</param>
        public override void Add(Vec4i value)
        {
            ThrowIfDisposed();
            NativeMethods.core_Mat_push_back_Vec4i(ptr, value);
        }
    }
}
#endregion