namespace BlackberrySystemPacker.Decompressors
{
    public class UnsafeLzoDecompressor : IDecompressor
    {
        public byte[] Compress(byte[] data)
        {
            var output = new byte[16384];
            var count = lzo1x_compress(data, output);
            return new Span<byte>(output, 0, count).ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            var output = new byte[16384];
            var count = lzo1x_decompress(data, output);
            return new Span<byte>(output, 0, count).ToArray();
        }

        public unsafe static int lzo1x_decompress(byte[] src, byte[] dst)
        {
            fixed (byte* ptr = src)
            {
                fixed (byte* ptr3 = dst)
                {
                    uint num = 0u;
                    uint num2 = 0u;
                    uint num3 = 0u;
                    byte* ptr2 = ptr + src.Length;
                    byte* ptr4 = ptr3 + dst.Length;
                    byte* ptr5 = ptr;
                    byte* ptr6 = ptr3;
                    byte* ptr7 = ptr3;
                    bool flag = false;
                    bool flag2 = false;
                    if (src.Length < 3)
                    {
                        throw new OverflowException("Input Overrun");
                    }
                    if (*ptr5 > 17)
                    {
                        num = (uint)(*(ptr5++) - 17);
                        if (num < 4)
                        {
                            num2 = num;
                            flag2 = true;
                        }
                        if (!flag2)
                        {
                            flag = true;
                        }
                    }
                    while (true)
                    {
                        if (flag)
                        {
                            flag = false;
                            if (ptr4 - ptr6 < num)
                            {
                                throw new OverflowException("Output Overrun");
                            }
                            if (ptr2 - ptr5 < 1)
                            {
                                throw new OverflowException("Input Overrun");
                            }
                            do
                            {
                                *(ptr6++) = *(ptr5++);
                            }
                            while (--num != 0);
                            num3 = 4u;
                            continue;
                        }
                        if (!flag2)
                        {
                            num = *(ptr5++);
                            byte* ptr8;
                            if (num < 16)
                            {
                                if (num3 == 0)
                                {
                                    if (num == 0)
                                    {
                                        if (ptr2 - ptr5 < 1)
                                        {
                                            throw new OverflowException("Input Overrun");
                                        }
                                        while (*ptr5 == 0)
                                        {
                                            num += 255;
                                            ptr5++;
                                            if (ptr2 - ptr5 < 1)
                                            {
                                                throw new OverflowException("Input Overrun");
                                            }
                                        }
                                        num += (uint)(15 + *(ptr5++));
                                    }
                                    num += 3;
                                    if (ptr4 - ptr6 < num)
                                    {
                                        throw new OverflowException("Output Overrun");
                                    }
                                    if (ptr2 - ptr5 < 1)
                                    {
                                        throw new OverflowException("Input Overrun");
                                    }
                                    do
                                    {
                                        *(ptr6++) = *(ptr5++);
                                    }
                                    while (--num != 0);
                                    num3 = 4u;
                                    continue;
                                }
                                if (num3 != 4)
                                {
                                    num2 = num & 3;
                                    ptr8 = ptr6 - 1;
                                    ptr8 -= (int)(num >> 2);
                                    ptr8 -= *(ptr5++) << 2;
                                    if (ptr8 < ptr3 || ptr8 >= ptr6)
                                    {
                                        throw new OverflowException("Lookbehind Overrun");
                                    }
                                    if (ptr4 - ptr6 < 2)
                                    {
                                        throw new OverflowException("Output Overrun");
                                    }
                                    *ptr6 = *ptr8;
                                    ptr6[1] = ptr8[1];
                                    ptr6 += 2;
                                    goto IL_0412;
                                }
                                num2 = num & 3;
                                ptr8 = ptr6 - 2049u;
                                ptr8 -= (int)(num >> 2);
                                ptr8 -= *(ptr5++) << 2;
                                num = 3u;
                            }
                            else
                            {
                                switch (num)
                                {
                                    default:
                                        num2 = num & 3;
                                        ptr8 = ptr6 - 1;
                                        ptr8 -= (int)((num >> 2) & 7);
                                        ptr8 -= *(ptr5++) << 3;
                                        num = (num >> 5) - 1 + 2;
                                        break;
                                    case 32u:
                                    case 33u:
                                    case 34u:
                                    case 35u:
                                    case 36u:
                                    case 37u:
                                    case 38u:
                                    case 39u:
                                    case 40u:
                                    case 41u:
                                    case 42u:
                                    case 43u:
                                    case 44u:
                                    case 45u:
                                    case 46u:
                                    case 47u:
                                    case 48u:
                                    case 49u:
                                    case 50u:
                                    case 51u:
                                    case 52u:
                                    case 53u:
                                    case 54u:
                                    case 55u:
                                    case 56u:
                                    case 57u:
                                    case 58u:
                                    case 59u:
                                    case 60u:
                                    case 61u:
                                    case 62u:
                                    case 63u:
                                        num = (num & 0x1F) + 2;
                                        if (num == 2)
                                        {
                                            while (*ptr5 == 0)
                                            {
                                                num += 255;
                                                ptr5++;
                                                if (ptr2 - ptr5 < 1)
                                                {
                                                    throw new OverflowException("Input Overrun");
                                                }
                                            }
                                            num += (uint)(31 + *(ptr5++));
                                            if (ptr2 - ptr5 < 2)
                                            {
                                                throw new OverflowException("Input Overrun");
                                            }
                                        }
                                        ptr8 = ptr6 - 1;
                                        num2 = *(ushort*)ptr5;
                                        ptr5 += 2;
                                        ptr8 -= (int)(num2 >> 2);
                                        num2 &= 3;
                                        break;
                                    case 0u:
                                    case 1u:
                                    case 2u:
                                    case 3u:
                                    case 4u:
                                    case 5u:
                                    case 6u:
                                    case 7u:
                                    case 8u:
                                    case 9u:
                                    case 10u:
                                    case 11u:
                                    case 12u:
                                    case 13u:
                                    case 14u:
                                    case 15u:
                                    case 16u:
                                    case 17u:
                                    case 18u:
                                    case 19u:
                                    case 20u:
                                    case 21u:
                                    case 22u:
                                    case 23u:
                                    case 24u:
                                    case 25u:
                                    case 26u:
                                    case 27u:
                                    case 28u:
                                    case 29u:
                                    case 30u:
                                    case 31u:
                                        ptr8 = ptr6;
                                        ptr8 -= (int)((num & 8) << 11);
                                        num = (num & 7) + 2;
                                        if (num == 2)
                                        {
                                            while (*ptr5 == 0)
                                            {
                                                num += 255;
                                                ptr5++;
                                                if (ptr2 - ptr5 < 1)
                                                {
                                                    throw new OverflowException("Input Overrun");
                                                }
                                            }
                                            num += (uint)(7 + *(ptr5++));
                                            if (ptr2 - ptr5 < 2)
                                            {
                                                throw new OverflowException("Input Overrun");
                                            }
                                        }
                                        num2 = *(ushort*)ptr5;
                                        ptr5 += 2;
                                        ptr8 -= (int)(num2 >> 2);
                                        num2 &= 3;
                                        if (ptr8 != ptr6)
                                        {
                                            ptr8 -= 16384;
                                            break;
                                        }
                                        return (int)(ptr6 - ptr7);
                                }
                            }
                            if (ptr8 < ptr3 || ptr8 >= ptr6)
                            {
                                throw new OverflowException("Lookbehind Overrun");
                            }
                            byte* ptr9 = ptr6 + (int)num;
                            if (ptr4 - ptr6 < num)
                            {
                                throw new OverflowException("Output Overrun");
                            }
                            *ptr6 = *ptr8;
                            ptr6[1] = ptr8[1];
                            ptr6 += 2;
                            ptr8 += 2;
                            do
                            {
                                *(ptr6++) = *(ptr8++);
                            }
                            while (ptr6 < ptr9);
                        }
                        goto IL_0412;
                    IL_0412:
                        flag2 = false;
                        num3 = num2;
                        num = num2;
                        if (ptr2 - ptr5 < num + 3)
                        {
                            throw new OverflowException("Input Overrun");
                        }
                        if (ptr4 - ptr6 < num)
                        {
                            break;
                        }
                        while (num != 0)
                        {
                            *(ptr6++) = *(ptr5++);
                            num--;
                        }
                    }
                    throw new OverflowException("Output Overrun");
                }
            }
        }

        public unsafe static int lzo1x_compress(byte[] src, byte[] dst)
        {
            fixed (byte* inPtr = src)
            fixed (byte* outPtr = dst)
            {
                byte* ip = inPtr;
                byte* op = outPtr;
                byte* in_end = ip + src.Length;
                byte* out_end = op + dst.Length;
                byte* ii = ip;
                uint state = 4; // Matches num3 in decompress
                
                // Start with literal run
                byte* op_start = op++;  // Leave first byte for literal run length

                while (ip < in_end - 4)
                {
                    byte* pos = null;
                    uint len = 0;

                    // Look for a match
                    if (ip + 4 <= in_end)
                    {
                        // Simple match finding - can be improved with better hashing
                        for (byte* p = ip - 1; p >= inPtr && p >= ip - 0x800; p--)
                        {
                            if (*(uint*)p == *(uint*)ip)
                            {
                                pos = p;
                                len = 4;
                                while (len < 9 && ip + len < in_end && pos[len] == ip[len])
                                    len++;
                                break;
                            }
                        }
                    }

                    if (pos != null && len >= 3)
                    {
                        uint offset = (uint)(ip - pos);
                        
                        // Output literal run if any
                        uint lit = (uint)(ip - ii);
                        if (lit > 0)
                        {
                            uint t = lit - 1;
                            if (t < 3)
                            {
                                op_start[0] = (byte)(t << 5);
                            }
                            else if (t <= 18)
                            {
                                op_start[0] = (byte)(t + 17);
                            }
                            else
                            {
                                uint tt = t - 18;
                                op_start[0] = 0;
                                while (tt > 255)
                                {
                                    tt -= 255;
                                    *op++ = 0;
                                }
                                *op++ = (byte)tt;
                            }

                            do
                            {
                                *op++ = *ii++;
                            } while (--lit > 0);
                        }

                        // Encode match based on length and offset
                        if (len <= 8 && offset <= 2048)
                        {
                            // Short match
                            uint code = ((offset >> 3) << 2) | (len - 2);
                            *op++ = (byte)code;
                            *op++ = (byte)((offset & 7) << 5);
                        }
                        else
                        {
                            // Long match
                            if (len > 33)
                            {
                                len -= 33;
                                *op++ = 32 | 1;
                                while (len > 255)
                                {
                                    len -= 255;
                                    *op++ = 0;
                                }
                                *op++ = (byte)len;
                            }
                            else
                                *op++ = (byte)(32 | (len - 2));

                            *(ushort*)op = (ushort)offset;
                            op += 2;
                        }

                        ip += len;
                        ii = ip;
                        state = 4;
                        op_start = op;
                    }
                    else
                    {
                        ip++;
                        if ((uint)(ip - ii) >= 3)
                        {
                            op_start = op;
                            uint lit = (uint)(ip - ii);
                            if (lit <= 3)
                            {
                                *op++ = (byte)lit;
                                do
                                {
                                    *op++ = *ii++;
                                } while (--lit > 0);
                                state = lit;
                            }
                            else if (lit <= 18)
                            {
                                *op++ = (byte)(lit + 17);
                                do
                                {
                                    *op++ = *ii++;
                                } while (--lit > 0);
                                state = 4;
                            }
                            else
                            {
                                uint t = lit - 18;
                                *op++ = 0;
                                while (t > 255)
                                {
                                    t -= 255;
                                    *op++ = 0;
                                }
                                *op++ = (byte)t;
                                do
                                {
                                    *op++ = *ii++;
                                } while (--lit > 0);
                                state = 4;
                            }
                        }
                    }
                }

                // Write remaining literals
                uint remaining = (uint)(in_end - ii);
                if (remaining > 0)
                {
                    op_start = op;
                    if (remaining <= 3)
                    {
                        *op++ = (byte)remaining;
                    }
                    else if (remaining <= 18)
                    {
                        *op++ = (byte)(remaining + 17);
                    }
                    else
                    {
                        uint t = remaining - 18;
                        *op++ = 0;
                        while (t > 255)
                        {
                            t -= 255;
                            *op++ = 0;
                        }
                        *op++ = (byte)t;
                    }

                    do
                    {
                        *op++ = *ii++;
                    } while (--remaining > 0);
                }

                // End marker
                *op++ = 17;
                *op++ = 0;
                *op++ = 0;

                return (int)(op - outPtr);
            }
        }
    }
}
