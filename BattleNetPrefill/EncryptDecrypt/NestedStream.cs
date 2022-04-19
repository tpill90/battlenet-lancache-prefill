using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BattleNetPrefill.EncryptDecrypt
{
    /// <summary>
    /// A stream that allows for reading from another stream up to a given number of bytes.
    /// </summary>
    internal class NestedStream : Stream
    {
        /// <summary>
        /// The stream to read from.
        /// </summary>
        private readonly Stream underlyingStream;

        /// <summary>
        /// The total length of the stream.
        /// </summary>
        private readonly long length;

        private readonly bool leaveOpen;

        /// <summary>
        /// The remaining bytes allowed to be read.
        /// </summary>
        private long remainingBytes;

        /// <summary>
        /// Initializes a new instance of the <see cref="NestedStream"/> class.
        /// </summary>
        /// <param name="underlyingStream">The stream to read from.</param>
        /// <param name="length">The number of bytes to read from the parent stream.</param>
        public NestedStream(Stream underlyingStream, long length, bool leaveOpen = false)
        {
            if (underlyingStream == null)
                throw new ArgumentNullException(nameof(underlyingStream));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (!underlyingStream.CanRead)
                throw new ArgumentException("Stream must be readable.", nameof(underlyingStream));

            this.underlyingStream = underlyingStream;
            this.remainingBytes = length;
            this.length = length;
            this.leaveOpen = leaveOpen;
        }

        /// <inheritdoc />
        public bool IsDisposed { get; private set; }

        /// <inheritdoc />
        public override bool CanRead => !this.IsDisposed;

        /// <inheritdoc />
        public override bool CanSeek => !this.IsDisposed && this.underlyingStream.CanSeek;

        /// <inheritdoc />
        public override bool CanWrite => false;

        /// <inheritdoc />
        public override long Length
        {
            get
            {
                CheckDisposed();
                return this.underlyingStream.CanSeek ? this.length : throw new NotSupportedException();
            }
        }

        /// <inheritdoc />
        public override long Position
        {
            get
            {
                CheckDisposed();
                return this.length - this.remainingBytes;
            }
            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        /// <inheritdoc />
        public override void Flush() => this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken) => throw this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc />
        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException();
            }

            count = (int)Math.Min(count, this.remainingBytes);

            if (count <= 0)
            {
                return 0;
            }

            int bytesRead = await this.underlyingStream.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
            this.remainingBytes -= bytesRead;
            return bytesRead;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDisposed();

            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0 || count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (offset + count > buffer.Length)
            {
                throw new ArgumentException();
            }

            count = (int)Math.Min(count, this.remainingBytes);

            if (count <= 0)
            {
                return 0;
            }

            int bytesRead = this.underlyingStream.Read(buffer, offset, count);
            this.remainingBytes -= bytesRead;
            return bytesRead;
        }

#if NETSTANDARD2_1_OR_GREATER || NETCOREAPP3_0_OR_GREATER
        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            CheckDisposed();

            // If we're beyond the end of the stream (as the result of a Seek operation), return 0 bytes.
            if (this.remainingBytes < 0)
            {
                return 0;
            }

            buffer = buffer.Slice(0, (int)Math.Min(buffer.Length, this.remainingBytes));

            if (buffer.IsEmpty)
            {
                return 0;
            }

            int bytesRead = await this.underlyingStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            this.remainingBytes -= bytesRead;
            return bytesRead;
        }
#endif

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            CheckDisposed();

            if (!this.CanSeek)
            {
                throw new NotSupportedException("The underlying stream does not support seeking.");
            }

            // Recalculate offset relative to the current position
            long newOffset = origin switch
            {
                SeekOrigin.Current => offset,
                SeekOrigin.End => this.length + offset - this.Position,
                SeekOrigin.Begin => offset - this.Position,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), "Invalid seek origin."),
            };

            // Determine whether the requested position is within the bounds of the stream
            if (this.Position + newOffset < 0)
            {
                throw new IOException("An attempt was made to move the position before the beginning of the stream.");
            }

            long currentPosition = this.underlyingStream.Position;
            long newPosition = this.underlyingStream.Seek(newOffset, SeekOrigin.Current);
            this.remainingBytes -= newPosition - currentPosition;
            return this.Position;
        }

        /// <inheritdoc />
        public override void SetLength(long value) => throw this.ThrowDisposedOr(new NotSupportedException());

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDisposed();
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (!this.leaveOpen)
                this.underlyingStream.Dispose();
            this.IsDisposed = true;
            base.Dispose(disposing);
        }

        private Exception ThrowDisposedOr(Exception ex)
        {
            CheckDisposed();
            throw ex;
        }

        /// <summary>
        /// Throws an System.ObjectDisposedException if an object is disposed.
        /// </summary>
        /// <exception cref="ObjectDisposedException"></exception>
        [DebuggerStepThrough]
        private void CheckDisposed()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(NestedStream));
        }
    }
}
