using System;
using NavalBattles.Runtime.Protocol.Messages;

namespace NavalBattles.Runtime.Protocol.Serialization
{
    public sealed class LittleEndianReader
    {
        private const int MAX_COLLECTION_LENGTH = 1024;

        private readonly byte[] _bytes;
        private int _position;

        public ProtocolError error { get; private set; }

        public bool hasRemainingBytes => _position < _bytes.Length;

        public LittleEndianReader(ReadOnlyMemory<byte> bytes)
        {
            _bytes = bytes.ToArray();
        }

        public bool TryReadByte(out byte value)
        {
            value = 0;

            if (CanRead(sizeof(byte)) == false)
            {
                return false;
            }

            value = ReadByteUnchecked();

            return true;
        }

        public bool TryReadInt32(out int value)
        {
            value = 0;

            if (CanRead(sizeof(int)) == false)
            {
                return false;
            }

            value = ReadInt32Unchecked();

            return true;
        }

        public bool TryReadUInt64(out ulong value)
        {
            value = 0;

            if (CanRead(sizeof(ulong)) == false)
            {
                return false;
            }

            for (int byteIndex = 0; byteIndex < sizeof(ulong); byteIndex++)
            {
                value |= (ulong)_bytes[_position + byteIndex] << (byteIndex * 8);
            }

            _position += sizeof(ulong);

            return true;
        }

        public bool TryReadDouble(out double value)
        {
            bool wasRead = TryReadUInt64(out ulong rawValue);
            value = BitConverter.Int64BitsToDouble(unchecked((long)rawValue));

            return wasRead;
        }

        public bool TryReadIntArray(out int[] values)
        {
            values = null;

            if (TryReadCollectionLength(sizeof(int), out int count) == false)
            {
                return false;
            }

            values = new int[count];

            for (int index = 0; index < count; index++)
            {
                values[index] = ReadInt32Unchecked();
            }

            return true;
        }

        public bool TryReadSByteArray(out sbyte[] values)
        {
            values = null;

            if (TryReadCollectionLength(sizeof(byte), out int count) == false)
            {
                return false;
            }

            values = new sbyte[count];

            for (int index = 0; index < count; index++)
            {
                values[index] = unchecked((sbyte)ReadByteUnchecked());
            }

            return true;
        }

        public bool TryReadShotStateArray(out NetworkShotState[] values)
        {
            values = null;

            if (TryReadCollectionLength(sizeof(byte), out int count) == false)
            {
                return false;
            }

            values = new NetworkShotState[count];

            for (int index = 0; index < count; index++)
            {
                values[index] = (NetworkShotState)ReadByteUnchecked();
            }

            return true;
        }

        public bool TryReadBoolArray(out bool[] values)
        {
            values = null;

            if (TryReadCollectionLength(sizeof(byte), out int count) == false)
            {
                return false;
            }

            values = new bool[count];

            for (int index = 0; index < count; index++)
            {
                values[index] = ReadByteUnchecked() != 0;
            }

            return true;
        }

        private bool TryReadCollectionLength(int elementSize, out int count)
        {
            if (TryReadInt32(out count) == false)
            {
                return false;
            }

            if (count < 0 || count > MAX_COLLECTION_LENGTH)
            {
                error = ProtocolError.InvalidCollectionLength;

                return false;
            }

            return CanRead(count * elementSize);
        }

        private byte ReadByteUnchecked()
        {
            return _bytes[_position++];
        }

        private int ReadInt32Unchecked()
        {
            int value = _bytes[_position] |
                _bytes[_position + 1] << 8 |
                _bytes[_position + 2] << 16 |
                _bytes[_position + 3] << 24;
            _position += sizeof(int);

            return value;
        }

        private bool CanRead(int byteCount)
        {
            if (byteCount < 0 || _position > _bytes.Length - byteCount)
            {
                error = ProtocolError.UnexpectedEnd;

                return false;
            }

            return true;
        }
    }
}
