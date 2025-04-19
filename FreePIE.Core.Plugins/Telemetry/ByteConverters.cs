using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.Telemetry
{
    public interface IByteConvertor<T> where T : struct
    {
        T FromBytes(byte[] data);
        byte[] ToBytes(T data);
    }

    /// <summary>
    /// Convert json to byte array and vice versa.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class JsonByteConverter<T> : IByteConvertor<T> where T : struct
    {
        public byte[] ToBytes(T data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
        public T FromBytes(byte[] data)
        {
            string json = System.Text.Encoding.UTF8.GetString(data);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
        }
    }

    /// <summary>
    /// Convert byte array to struct and vice versa.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MarshalByteConvertor<T> : IByteConvertor<T> where T : struct
    {
        public byte[] ToBytes(T data)
        {
            int size = Marshal.SizeOf(data);
            byte[] arr = new byte[size];
            using (SafeBuffer buffer = new SafeBuffer(size))
            {
                Marshal.StructureToPtr(data, buffer.DangerousGetHandle(), true);
                Marshal.Copy(buffer.DangerousGetHandle(), arr, 0, size);
            }
            return arr;
        }

        public T FromBytes(byte[] data)
        {
            T result = default(T);
            using (SafeBuffer buffer = new SafeBuffer(data.Length))
            {
                Marshal.Copy(data, 0, buffer.DangerousGetHandle(), data.Length);
                result = (T)Marshal.PtrToStructure(buffer.DangerousGetHandle(), typeof(T));
            }
            return result;
        }

        private class SafeBuffer : SafeHandle
        {
            public SafeBuffer(int size) : base(IntPtr.Zero, true)
            {
                SetHandle(Marshal.AllocHGlobal(size));
            }

            public override bool IsInvalid => handle == IntPtr.Zero;

            protected override bool ReleaseHandle()
            {
                Marshal.FreeHGlobal(handle);
                return true;
            }
        }

    }

}