using System;
using System.Collections.Generic;
using Swigged.Cuda;

namespace Turbo
{
    using static CudaHelpers;

    /// <summary>
    /// A wrapper around a CUDA device.
    /// </summary>
    internal struct CudaDevice
    {
        internal CudaDevice(int handle)
        {
            this.Handle = handle;
        }

        /// <summary>
        /// Gets the handle to the device.
        /// </summary>
        /// <value>A device handle.</value>
        public int Handle { get; private set; }

        /// <summary>
        /// Queries the device's compute capability.
        /// </summary>
        /// <returns>The device's compute capability.</returns>
        public Version ComputeCapability =>
            new Version(
                GetAttribute(CUdevice_attribute.CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MAJOR),
                GetAttribute(CUdevice_attribute.CU_DEVICE_ATTRIBUTE_COMPUTE_CAPABILITY_MINOR),
                0,
                0);

        /// <summary>
        /// Queries a device attribute.
        /// </summary>
        /// <param name="attribute">The device attribute to query.</param>
        /// <returns>The value of the device attribute.</returns>
        public int GetAttribute(CUdevice_attribute attribute)
        {
            return Call<CUdevice_attribute, int, int>(Cuda.cuDeviceGetAttribute, attribute, Handle);
        }

        /// <summary>
        /// Gets a sequence containing all devices on the machine.
        /// </summary>
        /// <returns>A sequence of devices.</returns>
        public static IEnumerable<CudaDevice> GetDevices()
        {
            var count = Call<int>(Cuda.cuDeviceGetCount);
            for (int i = 0; i < count; i++)
            {
                yield return new CudaDevice(Call<int, int>(Cuda.cuDeviceGet, i));
            }
        }
    }
}
