using System;
using System.Runtime.Serialization;
using Swigged.Cuda;

namespace Turbo
{
    [Serializable]
    public sealed class CudaException : Exception
    {
        public CudaException() { }
        public CudaException(string message) : base(message) { }
        public CudaException(string message, Exception inner) : base(message, inner) { }
        protected CudaException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) { }
    }

    internal static class CudaHelpers
    {
        public delegate CUresult CuApi<T>(out T result);
        public delegate CUresult CuApi<T1, TOut>(out TOut result, T1 arg1);
        public delegate CUresult CuApi<T1, T2, TOut>(out TOut result, T1 arg1, T2 arg2);

        public static T Call<T>(CuApi<T> callee)
        {
            T result;
            CheckError(callee(out result));
            return result;
        }

        public static TOut Call<T1, TOut>(CuApi<T1, TOut> callee, T1 arg1)
        {
            TOut result;
            CheckError(callee(out result, arg1));
            return result;
        }

        public static TOut Call<T1, T2, TOut>(CuApi<T1, T2, TOut> callee, T1 arg1, T2 arg2)
        {
            TOut result;
            CheckError(callee(out result, arg1, arg2));
            return result;
        }

        public static void CheckError(CUresult error)
        {
            if (error != CUresult.CUDA_SUCCESS)
            {
                throw new CudaException("CUDA error " + error.ToString());
            }
        }
    }
}
