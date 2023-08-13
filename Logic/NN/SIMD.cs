using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Drawing;
using System.Net;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Reflection;

namespace LTChess.Logic.NN
{

    /// <summary>
    /// Contains alias methods for accessing Avx2 intrinsics.
    /// </summary>
    public static unsafe class SIMD
    {

        /// <summary>
        /// Returns a vector with the sums of the products of the half-sized elements in <paramref name="left"/> and <paramref name="right"/>.
        /// The resulting vector is half the size of the input vectors.
        /// <br></br>
        /// Fox example, MultiplyAddAdjacent256([A1, A2, ...], [B1, B2, ...]) returns [(A1 * B1) + (A2 * B2), ...]
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it.
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<int> MultiplyAddAdjacent256(Vector256<short> left, Vector256<short> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.MultiplyAddAdjacent(left, right);
            }
            else
            {
                Span<int> products = stackalloc int[Vector256<short>.Count];
                for (int i = 0; i < Vector256<short>.Count; i++)
                {
                    products[i] = left[i] * right[i];
                }

                int vectI = 0;
                int[] result = new int[Vector256<int>.Count];
                for (int i = 0; i < Vector256<int>.Count; i++)
                {
                    result[i] = products[vectI++] + products[vectI++];
                }

                return Unsafe.ReadUnaligned<Vector256<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(result), 0)));
            }
        }


        /// <summary>
        /// Returns a vector with the elements of <paramref name="left"/> minus <paramref name="right"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it.
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<short> Sub256(Vector256<short> left, Vector256<short> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Subtract(left, right);
            }
            else
            {
                return Vector256.Subtract(left, right);
            }
        }


        /// <summary>
        /// Returns a vector with the elements of <paramref name="left"/> plus <paramref name="right"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it.
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<short> Add256(Vector256<short> left, Vector256<short> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Add(left, right);
            }
            else
            {
                return Vector256.Add(left, right);
            }
        }

        /// <summary>
        /// Writes without alignment the values in <paramref name="vector"/> into the <paramref name="array"/> beginning at the <paramref name="index"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it.
        /// </summary>
        [MethodImpl(Inline)]
        public static void Store256(ref Vector256<short> vector, short[] array, int index)
        {
            if (Avx2.IsSupported)
            {
                Avx.Store((short*)UnsafeAddrOfPinnedArrayElementUnchecked(array, index), vector);
            }
            else
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<short, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index)), vector);
            }
        }


        /// <summary>
        /// Loads an unaligned <see cref="Vector256"/> from the <paramref name="array"/> beginning at the <paramref name="index"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it.
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<short> Load256(short[] array, int index)
        {
            if (Avx2.IsSupported)
            {
                return Avx.LoadDquVector256((short*)UnsafeAddrOfPinnedArrayElementUnchecked(array, index));
            }

            return Unsafe.ReadUnaligned<Vector256<short>>(ref Unsafe.As<short, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index)));
        }



        /// <summary>
        /// Clamps the values in <paramref name="vector"/> between the values in <paramref name="min"/> and <paramref name="max"/>
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it.
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<short> Clamp256(this Vector256<short> vector, ref Vector256<short> min, ref Vector256<short> max)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Max(min, Avx2.Min(max, vector));
            }

            return Vector256.Max(min, Vector256.Min(max, vector));
        }





        /// <summary>
        /// Same as <see cref="Marshal.UnsafeAddrOfPinnedArrayElement{T}(T[], int)"/> but doesn't check if <paramref name="arr"/> is null
        /// </summary>
        [MethodImpl(Inline)]
        public static unsafe IntPtr UnsafeAddrOfPinnedArrayElementUnchecked<T>(T[] arr, int index)
        {
            void* pRawData = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
#pragma warning disable 8500 // sizeof of managed types
            return (IntPtr)((byte*)pRawData + (uint)index * (nuint)sizeof(T));
#pragma warning restore 8500
        }
    }
}
