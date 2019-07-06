using System;
using Swigged.Cuda;

namespace Turbo
{
    using static CudaHelpers;

    /// <summary>
    /// A wrapper around a CUDA context.
    /// </summary>
    internal sealed class CudaContext : IDisposable
    {
        private CudaContext(CUcontext handle)
        {
            this.Handle = handle;
            this.isOwner = false;
        }

        /// <summary>
        /// Creates a CUDA context.
        /// </summary>
        /// <param name="device">The device to use for the context.</param>
        /// <param name="flags">The context's flags.</param>
        public CudaContext(CudaDevice device, uint flags=0)
        {
            this.Handle = Call<uint, int, CUcontext>(Cuda.cuCtxCreate_v2, flags, device.Handle);
            this.isOwner = true;
        }

        /// <summary>
        /// Gets the handle to the context.
        /// </summary>
        /// <value>A context handle.</value>
        public CUcontext Handle { get; private set; }

        /// <summary>
        /// Gets the device associated with this context.
        /// </summary>
        /// <returns>A CUDA device.</returns>
        public CudaDevice Device => new CudaDevice(Call<int>(Cuda.cuCtxGetDevice));

        /// <summary>
        /// Gets or sets a Boolean that tells if this CUDA context is the current CUDA context.
        /// </summary>
        public bool IsCurrent
        {
            get
            {
                return Call<CUcontext>(Cuda.cuCtxGetCurrent).Pointer == Handle.Pointer;
            }
            set
            {
                CheckError(Cuda.cuCtxSetCurrent(Handle));
            }
        }

        private bool isOwner;

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (isOwner)
                {
                    Cuda.cuCtxDestroy_v2(Handle);
                }

                disposedValue = true;
            }
        }

        ~CudaContext()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
