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
using System.Reflection;
using System.Reflection.Metadata;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx2.MultiplyAddAdjacent(Vector256{short}, Vector256{short})"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<int> MultiplyAddAdjacent256(in Vector256<short> left, in Vector256<short> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.MultiplyAddAdjacent(left, right);
            }
            else
            {
                Span<int> products = stackalloc int[VSize.Short];
                for (int i = 0; i < VSize.Short; i++)
                {
                    products[i] = left[i] * right[i];
                }

                int vectI = 0;
                int[] result = new int[VSize.Int];
                for (int i = 0; i < VSize.Int; i++)
                {
                    result[i] = products[vectI++] + products[vectI++];
                }

                return Unsafe.ReadUnaligned<Vector256<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(result), 0)));
            }
        }


        /// <summary>
        /// Returns a vector with the elements of <paramref name="left"/> minus <paramref name="right"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx2.Subtract(Vector256{short}, Vector256{short})"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<short> Sub256(in Vector256<short> left, in Vector256<short> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Subtract(left, right);
            }

            return Vector256.Subtract(left, right);
        }

        /// <inheritdoc cref="Sub256"/>
        [MethodImpl(Inline)]
        public static Vector256<int> Sub256(in Vector256<int> left, in Vector256<int> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Subtract(left, right);
            }

            return Vector256.Subtract(left, right);
        }


        /// <summary>
        /// Returns a vector with the elements of <paramref name="left"/> plus <paramref name="right"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx2.Add(Vector256{short}, Vector256{short})"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<short> Add256(in Vector256<short> left, in Vector256<short> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Add(left, right);
            }

            return Vector256.Add(left, right);
        }


        /// <summary>
        /// Returns a vector with the elements of <paramref name="left"/> plus <paramref name="right"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx2.Add(Vector256{int}, Vector256{int})"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<int> Add256(in Vector256<int> left, in Vector256<int> right)
        {
            if (Avx2.IsSupported)
            {
                return Avx2.Add(left, right);
            }

            return Vector256.Add(left, right);
        }


        /// <summary>
        /// Writes without alignment the values in <paramref name="vector"/> into the <paramref name="array"/> beginning at the <paramref name="index"/>.
        /// <para></para>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx.Store"/>
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

        /// <inheritdoc cref="Store256"/>
        [MethodImpl(Inline)]
        public static void Store256(ref Vector256<int> vector, int[] array, int index)
        {
            if (Avx2.IsSupported)
            {
                Avx.Store((int*)UnsafeAddrOfPinnedArrayElementUnchecked(array, index), vector);
            }
            else
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index)), vector);
            }
        }


        /// <summary>
        /// Loads an unaligned <see cref="Vector256"/> from the <paramref name="array"/> beginning at the <paramref name="index"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx.LoadDquVector256"/>
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



        /// <inheritdoc cref="Load256"/>
        [MethodImpl(Inline)]
        public static Vector256<int> Load256(int[] array, int index)
        {
            if (Avx2.IsSupported)
            {
                return Avx.LoadDquVector256((int*)UnsafeAddrOfPinnedArrayElementUnchecked(array, index));
            }

            return Unsafe.ReadUnaligned<Vector256<int>>(ref Unsafe.As<int, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index)));
        }

        /// <inheritdoc cref="Load256"/>
        [MethodImpl(Inline)]
        public static Vector256<sbyte> Load256(sbyte[] array, int index)
        {
            if (Avx2.IsSupported)
            {
                return Avx.LoadDquVector256((sbyte*)UnsafeAddrOfPinnedArrayElementUnchecked(array, index));
            }

            return Unsafe.ReadUnaligned<Vector256<sbyte>>(ref Unsafe.As<sbyte, byte>(ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index)));
        }




        public static Vector256<int> LoadPointer256(void* arrayPointer, int index)
        {
            if (Avx2.IsSupported)
            {
                return Avx.LoadDquVector256((int*)arrayPointer + index * sizeof(int));
            }

            return Unsafe.ReadUnaligned<Vector256<int>>((int*)arrayPointer + index * sizeof(int));
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
        /// Writes without alignment the values in <paramref name="vector"/> into the <paramref name="span"/> beginning at the <paramref name="index"/>.
        /// <para></para>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx.Store"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static void StoreSpan256(ref Vector256<int> vector, in Span<int> span, int index)
        {
            if (Avx2.IsSupported)
            {
                Avx.Store((int*) Unsafe.AsPointer(ref span[index]), vector);
            }
            else
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<int, byte>(ref span[index]), vector);
            }
        }

        /// <inheritdoc cref="StoreSpan256"/>
        [MethodImpl(Inline)]
        public static void StoreSpan256(ref Vector256<int> vector, in Span<sbyte> span, int index)
        {
            if (Avx2.IsSupported)
            {
                Avx.Store((int*) Unsafe.AsPointer(ref span[index]), vector);
            }
            else
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref span[index]), vector);
            }
        }

        /// <inheritdoc cref="StoreSpan256"/>
        [MethodImpl(Inline)]
        public static void StoreSpan256(ref Vector256<sbyte> vector, in Span<sbyte> span, int index)
        {
            if (Avx2.IsSupported)
            {
                Avx.Store((sbyte*)Unsafe.AsPointer(ref span[index]), vector);
            }
            else
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref span[index]), vector);
            }
        }



        /// <summary>
        /// Loads an unaligned <see cref="Vector256"/> from the <paramref name="span"/> beginning at the <paramref name="index"/>.
        /// <br></br>
        /// Uses <see cref="Avx2"/> if the CPU supports it: <inheritdoc cref="Avx.LoadDquVector256"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector256<int> LoadSpan256(in Span<int> span, int index)
        {
            if (Avx2.IsSupported)
            {
                return Avx.LoadDquVector256((int*) Unsafe.AsPointer(ref span[index]));
            }

            return Unsafe.ReadUnaligned<Vector256<int>>(ref Unsafe.As<int, byte>(ref span[index]));
        }
        
        /// <inheritdoc cref="LoadSpan256"/>
        [MethodImpl(Inline)]
        public static Vector256<byte> LoadSpan256(in Span<sbyte> span, int index)
        {
            if (Avx2.IsSupported)
            {
                return Avx.LoadDquVector256((byte*) Unsafe.AsPointer(ref span[index]));
            }

            return Unsafe.ReadUnaligned<Vector256<byte>>(ref Unsafe.As<sbyte, byte>(ref span[index]));
        }

        /// <summary>
        /// Loads an unaligned <see cref="Vector128"/> from the <paramref name="span"/> beginning at the <paramref name="index"/>.
        /// <br></br>
        /// Uses <see cref="Sse2"/> if the CPU supports it: <inheritdoc cref="Sse2.LoadVector128"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static Vector128<int> LoadSpan128(in Span<int> span, int index)
        {
            if (Sse2.IsSupported)
            {
                Sse2.LoadVector128((int*) Unsafe.AsPointer(ref span[index]));
            }

            return Unsafe.ReadUnaligned<Vector128<int>>(ref Unsafe.As<int, byte>(ref span[index]));
        }

        /// <summary>
        /// Writes without alignment the values in <paramref name="vector"/> into the <paramref name="span"/> beginning at the <paramref name="index"/>.
        /// <br></br>
        /// Uses <see cref="Sse2"/> if the CPU supports it: <inheritdoc cref="Sse2.Store"/>
        /// </summary>
        [MethodImpl(Inline)]
        public static void StoreSpan128(ref Vector128<sbyte> vector, in Span<sbyte> span, int index)
        {
            if (Sse2.IsSupported)
            {
                Sse2.Store((sbyte*) Unsafe.AsPointer(ref span[index]), vector);
            }
            else
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<sbyte, byte>(ref span[index]), vector);
            }
        }



        /// <summary>
        /// Same as <see cref="Marshal.UnsafeAddrOfPinnedArrayElement{T}(T[], int)"/> but doesn't check if <paramref name="arr"/> is null
        /// </summary>
        [MethodImpl(Inline)]
        public static IntPtr UnsafeAddrOfPinnedArrayElementUnchecked<T>(in T[] arr, int index)
        {
            void* pRawData = Unsafe.AsPointer(ref MemoryMarshal.GetArrayDataReference(arr));
#pragma warning disable 8500 // sizeof of managed types
            return (IntPtr)((byte*)pRawData + (uint)index * (nuint)sizeof(T));
#pragma warning restore 8500
        }
    }
}
