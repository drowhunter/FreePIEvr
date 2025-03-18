using System;
using System.IO.MemoryMappedFiles;
using FreePIE.Core.Contracts;

namespace FreePIE.Core.Plugins
{

    [GlobalType(Type = typeof(MemoryMappedFilePluginGlobal))]
    public class MemoryMappedFilePlugin : Plugin
    {
        private int m_memorySize = 0;
        private MemoryMappedFile m_memoryMappedFile;
        private MemoryMappedViewAccessor m_accessor;

        public MemoryMappedViewAccessor Accessor => m_accessor ?? throw new Exception("mmf has not yet been opened for read and write.");
        
        public override object CreateGlobal()
        {
            return new MemoryMappedFilePluginGlobal(this);
        }

        public override string FriendlyName
        {
            get { return "Memory Mapped File Plugin"; }
        }

        public override Action Start()
        {
            m_memorySize = 0;
            m_memoryMappedFile = null;
            m_accessor = null;

            return null;
        }

        public int Register(int size)
        {
            if (m_memoryMappedFile != null)
            {
                throw new Exception("mmf has already been opened, all register calls have to be done before.");
            }

            int lastAdress = m_memorySize;

            m_memorySize += size;

            return lastAdress;
        }

        public void Open(string fileName)
        {
            if (m_memorySize == 0)
            {
                throw new Exception("Trying to create an empty mmf, use register before.");
            }

            m_memoryMappedFile = MemoryMappedFile.CreateOrOpen(fileName, m_memorySize);
            m_accessor = m_memoryMappedFile.CreateViewAccessor();
        }

        public override void Stop()
        {
            base.Stop();

            m_accessor?.Dispose();
            m_memoryMappedFile?.Dispose();

            m_memorySize = 0;
            m_memoryMappedFile = null;
            m_accessor = null;
        }
    }

    [Global(Name = "mmf")]
    public class MemoryMappedFilePluginGlobal
    {
        MemoryMappedFilePlugin m_plugin;

        public MemoryMappedFilePluginGlobal(MemoryMappedFilePlugin plugin)
        {
            m_plugin = plugin;
        }

        public int RegisterBool()
        {
            return m_plugin.Register(sizeof(byte));
        }

        public int RegisterInt()
        {
            return m_plugin.Register(sizeof(int));
        }

        public int RegisterFloat()
        {
            return m_plugin.Register(sizeof(float));
        }

        public int RegisterDouble()
        {
            return m_plugin.Register(sizeof(double));
        }

        public int RegisterVRPose()
        {
            return m_plugin.Register(6 * sizeof(float));
        }

        public void Open(string fileName)
        {
            m_plugin.Open(fileName);
        }

        public void WriteBool(int address, bool value)
        {
            m_plugin.Accessor.Write(address, (byte)(value ? 1 : 0));
        }

        public void WriteInt(int address, int value)
        {
            m_plugin.Accessor.Write(address, value);
        }

        public void WriteFloat(int address, float value)
        {
            m_plugin.Accessor.Write(address, value);
        }

        public void WriteDouble(int address, double value)
        {
            m_plugin.Accessor.Write(address, value);
        }

        public void WriteVRPose(int address, Vr6DofGlobal pose)
        {
            m_plugin.Accessor.Write(address, pose.x);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, pose.y);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, pose.z);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, pose.yawRaw);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, pose.pitchRaw);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, pose.rollRaw);
        }

        public bool ReadBool(int address)
        {
            return m_plugin.Accessor.ReadByte(address) != 0;
        }

        public int ReadInt(int address)
        {
            return m_plugin.Accessor.ReadInt32(address);
        }

        public float ReadFloat(int address)
        {
            return m_plugin.Accessor.ReadSingle(address);
        }

        public double ReadDouble(int address)
        {
            return m_plugin.Accessor.ReadDouble(address);
        }

        public Vr6DofGlobal ReadVRPose(int address)
        {
            Vr6DofGlobal v = new Vr6DofGlobal();

            m_plugin.Accessor.Write(address, v.position.x);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, v.position.y);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, v.position.z);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, v.yawRaw);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, v.pitchRaw);
            address += sizeof(float);

            m_plugin.Accessor.Write(address, v.rollRaw);

            return v;
        }
    }
}
