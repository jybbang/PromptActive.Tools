using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PromptActive.Tools.Utils;

namespace PromptActive.Tools.Collections
{
    public sealed class ByteArrayList : IEnumerable<byte>
    {
        #region Fields
        // 배열의 순서가 보증되어야 한다
        private readonly List<byte> Memory = new List<byte>();
        #endregion

        #region Properties
        public int Begin { get; private set; } = int.MaxValue;
        public int End { get; private set; }
        public int Count => Memory.Count;
        public byte this[int index]
        {
            get
            {
                // 0보다 작을수 없다.
                if (index < 0) throw new IndexOutOfRangeException();
                // 정상범위일경우 값을 리턴한다.
                if (index < Count) return Memory[index];
                // 초과할경우 추가한다.
                for (int i = Count; i <= index; i++)
                {
                    Memory.Add(0);
                }
                return 0;
            }
            set
            {
                // 0보다 작을수 없다.
                if (index < 0) throw new IndexOutOfRangeException();
                // 정상범위일경우 값을 수정한다.
                if (index < Count) Memory[index] = value;
                else
                {
                    // 초과할경우 추가한다.
                    for (int i = Count; i <= index; i++)
                    {
                        Memory.Add(0);
                    }
                    Memory[index] = value;
                }
            }
        }
        #endregion

        #region Public Methods
        public IEnumerator<byte> GetEnumerator()
        {
            return this.Memory.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Memory.GetEnumerator();
        }

        public void Clear() => Memory.Clear();

        public List<byte> ToList() => Memory;

        public byte[] ToArray() => Memory.ToArray();

        public string Read(string addr)
        {
            try
            {
                var by = addr.ToInt();
                Begin = by < Begin ? by : Begin;
                End = by > End ? by : End;

                addr = addr[0] == 'D' ? string.Concat(addr.Skip(1)) : addr;
                switch (addr[1])
                {
                    case 'B':
                        {
                            var BYTE = addr.ToInt();
                            return B(BYTE).ToString();
                        }
                    case 'W':
                        {
                            var BYTE = addr.ToInt();
                            return W(BYTE).ToString();
                        }
                    case 'D':
                        {
                            var BYTE = addr.ToInt();
                            return D(BYTE).ToString();
                        }
                    default:
                        {
                            var BYTE = addr.TakeWhile((x) => (x != '.')).ToInt();
                            var BIT = addr.SkipWhile((x) => (x != '.')).Skip(1).ToInt();
                            return X(BYTE, BIT) == false ? "0" : "1";
                        }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"NG, {nameof(ByteArrayList)}.Read() -> {ex.StackTrace}");
                return "0";
            }
        }

        public void Write(string addr, string value)
        {
            try
            {
                var by = addr.ToInt();
                Begin = by < Begin ? by : Begin;
                End = by > End ? by : End;

                addr = addr[0] == 'D' ? string.Concat(addr.Skip(1)) : addr;
                switch (addr[1])
                {
                    case 'B':
                        {
                            var BYTE = addr.ToInt();
                            if (byte.TryParse(value, out byte ret)) B(BYTE, ret);
                        }
                        break;
                    case 'W':
                        {
                            var BYTE = addr.ToInt();
                            if (UInt16.TryParse(value, out UInt16 ret)) W(BYTE, ret);
                        }
                        break;
                    case 'D':
                        {
                            var BYTE = addr.ToInt();
                            if (UInt32.TryParse(value, out UInt32 ret)) D(BYTE, ret);
                        }
                        break;
                    default:
                        {
                            var BYTE = addr.TakeWhile((x) => (x != '.')).ToInt();
                            var BIT = addr.SkipWhile((x) => (x != '.')).Skip(1).ToInt();
                            X(BYTE, BIT, value.ToBool());
                        }
                        break;
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AddrCheck(string addr)
        {
            try
            {
                var by = addr.ToInt();
                Begin = by < Begin ? by : Begin;
                End = by > End ? by : End;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public byte B(int BYTE) => this[BYTE];

        public bool X(int BYTE, int BIT) => this[BYTE].GetBit(BIT);

        public UInt16 W(int BYTE) => (UInt16)(this[BYTE] * 0x100 + this[BYTE + 1]);

        public UInt32 D(int BYTE) => (UInt32)(this[BYTE] * 0x1000000 + this[BYTE + 1] * 0x10000 + this[BYTE + 2] * 0x100 + this[BYTE + 3]);

        public void B(int BYTE, byte Value) => this[BYTE] = Value;

        public void X(int BYTE, int BIT, bool Value)
        {
            if (Value)
                this[BYTE] |= (byte)(1 << BIT);
            else
                this[BYTE] &= (byte)~(1 << BIT);
        }

        public void W(int BYTE, UInt16 Value)
        {
            this[BYTE] = (byte)(Value >> 8 & 0xFF);
            this[BYTE + 1] = (byte)(Value & 0xFF);
        }

        public void D(int BYTE, UInt32 Value)
        {
            this[BYTE] = (byte)(Value >> 24 & 0xFF);
            this[BYTE + 1] = (byte)(Value >> 16 & 0xFF);
            this[BYTE + 2] = (byte)(Value >> 8 & 0xFF);
            this[BYTE + 3] = (byte)(Value & 0xFF);
        }
        #endregion
    }
}
