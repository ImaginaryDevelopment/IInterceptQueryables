using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace QueryInterception
{
    public sealed class DebugTextWriter : StreamWriter
    {
        public DebugTextWriter(string category = null) : base(new DebugTextWriter.DebugOutStream(category), System.Text.Encoding.Unicode, 1024)
        {
            this.AutoFlush = true;
        }

        private class DebugOutStream : Stream
        {
            private readonly string _category;

            public override bool CanRead
            {
                get
                {
                    return false;
                }
            }

            public override bool CanSeek
            {
                get
                {
                    return false;
                }
            }

            public override bool CanWrite
            {
                get
                {
                    return true;
                }
            }

            public override long Length
            {
                get
                {
                    throw new InvalidOperationException();
                }
            }

            public override long Position
            {
                get
                {
                    throw new InvalidOperationException();
                }
                set
                {
                    throw new InvalidOperationException();
                }
            }

            public DebugOutStream(string category)
            {
                this._category = category;
            }

            public override void Flush()
            {
                Debug.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new InvalidOperationException();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new InvalidOperationException();
            }

            public override void SetLength(long value)
            {
                throw new InvalidOperationException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (!string.IsNullOrEmpty(this._category))
                {
                    Debug.Write(System.Text.Encoding.Unicode.GetString(buffer, offset, count));
                }
                else
                {
                    Debug.Write(System.Text.Encoding.Unicode.GetString(buffer, offset, count), this._category);
                }
            }
        }
    }
}