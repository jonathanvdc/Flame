using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Front
{
    /// <summary>
    /// A stream that tries to preserve the underlying file, if possible.
    /// </summary>
    public class PreservingFileStream : Stream
    {
        public PreservingFileStream(string Path)
        {
            this.Path = Path;
            this.Buffer = new MemoryStream();
        }

        public string Path { get; private set; }
        public MemoryStream Buffer { get; private set; }

        public override bool CanRead
        {
            get { return Buffer.CanRead; }
        }

        public override bool CanSeek
        {
            get { return Buffer.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return Buffer.CanWrite; }
        }

        public override void Flush()
        {
            var info = new FileInfo(Path);
            if (!FileEquals(info, Buffer))
            {
                Buffer.Seek(0, SeekOrigin.Begin);
                using (var fs = info.Open(FileMode.Create, FileAccess.Write))
                {
                    Buffer.CopyTo(fs);
                }
            }
        }

        public override long Length
        {
            get { return Buffer.Length; }
        }

        public override long Position
        {
            get
            {
                return Buffer.Position;
            }
            set
            {
                Buffer.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Buffer.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Buffer.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Buffer.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Buffer.Write(buffer, offset, count);
        }

        #region Static

        public static bool FileEquals(FileInfo Info, Stream Data)
        {
            if (Info.Length != Data.Length)
            {
                return false;
            }

            using (var fs = Info.OpenRead())
            {
                Data.Seek(0, SeekOrigin.Begin);
                return StreamEquals(fs, Data);
            }
        }

        public static bool StreamEquals(Stream First, Stream Second)
        {
            const int bufSize = 2048;
            byte[] firstBuffer = new byte[bufSize]; //buffer size
            byte[] secondBuffer = new byte[bufSize];
            while (true)
            {
                int firstLen = First.Read(firstBuffer, 0, bufSize);
                int secondLen = Second.Read(secondBuffer, 0, bufSize);

                if (firstLen != secondLen)
                    return false;

                if (firstLen == 0)
                    return true;

                if (!firstBuffer.Take(firstLen).SequenceEqual(secondBuffer.Take(secondLen)))
                    return false;
            }
        }

        #endregion
    }
}
