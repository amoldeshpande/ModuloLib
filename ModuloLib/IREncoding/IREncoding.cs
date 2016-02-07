using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuloLib
{
    using uint32_t = UInt32;
    using uint8_t = Byte;

    // Most IR Protocols use either pulse width or pulse distance encoding
    // and vary primarily in the header pulses, specific timing, number of bits.
    // PulseModulationEncoding allows a protocol's encoding to be specified
    // in terms of those details.
    public struct PulseModulationEncoding
    {
        public UInt16 protocol;      // The protocol number (see below)
        public UInt16 headerMark;    // Length of the header mark in us
        public UInt16 headerSpace;   // Length of the header space in us
        public UInt16 numBits;       // Number of bits
        public UInt16 oneMark;       // Length of the mark for a "1" bit
        public UInt16 oneSpace;      // Length of the space for a "1" bit
        public UInt16 zeroMark;      // Length of the mark for a "0" bit
        public UInt16 zeroSpace;     // Length of the space for a "0" bit
        public UInt16 stopMark;      // Length of the trailing "stop" mark
        public UInt16 khz;           // Modulation frequency in kHz
    };
    public class IREncoding
    {
        public const int NUM_IR_ENCODINGS = 7;
        public const int IR_PROTOCOL_NEC = 0;
        public const int IR_PROTOCOL_NEC_REPEAT = 1;
        public const int IR_PROTOCOL_SONY = 2;
        public const int IR_PROTOCOL_JVC = 3;
        public const int IR_PROTOCOL_JVC_REPEAT = 4;
        public const int IR_PROTOCOL_RC5 = 5;
        public const int IR_PROTOCOL_RC6 = 6;
        public const int IR_PROTOCOL_PANASONIC = 7;
        public const int IR_PROTOCOL_LG = 8;

        public const int NEC_HDR_MARK = 9000;
        public const int NEC_HDR_SPACE = 4500;
        public const int NEC_BIT_MARK = 560;
        public const int NEC_ONE_SPACE = 1690;
        public const int NEC_ZERO_SPACE = 560;
        public const int NEC_RPT_SPACE = 2250;

        public const int SONY_HDR_MARK = 2400;
        public const int SONY_HDR_SPACE = 600;
        public const int SONY_ONE_MARK = 1200;
        public const int SONY_ZERO_MARK = 600;
        public const int SONY_RPT_LENGTH = 45000;
        public const int SONY_DOUBLE_SPACE_USECS = 500; // usually ssee 713 - not using ticks as get number wrapround

        public const int JVC_HDR_MARK = 8000;
        public const int JVC_HDR_SPACE = 4000;
        public const int JVC_BIT_MARK = 600;
        public const int JVC_ONE_SPACE = 1600;
        public const int JVC_ZERO_SPACE = 550;
        public const int JVC_RPT_LENGTH = 60000;

        public const int PANASONIC_HDR_MARK = 3502;
        public const int PANASONIC_HDR_SPACE = 1750;
        public const int PANASONIC_BIT_MARK = 502;
        public const int PANASONIC_ONE_SPACE = 1244;
        public const int PANASONIC_ZERO_SPACE = 400;

        public const int LG_HDR_MARK = 8000;
        public const int LG_HDR_SPACE = 4000;
        public const int LG_BIT_MARK = 600;
        public const int LG_ONE_SPACE = 1600;
        public const int LG_ZERO_SPACE = 550;
        public const int LG_RPT_LENGTH = 60000;

        public const int NEC_BITS = 32;
        public const int SONY_BITS = 12;
        public const int SANYO_BITS = 12;
        public const int MITSUBISHI_BITS = 16;
        public const int MIN_RC5_SAMPLES = 11;
        public const int MIN_RC6_SAMPLES = 1;
        public const int PANASONIC_BITS = 48;
        public const int JVC_BITS = 16;
        public const int LG_BITS = 28;
        public const int SAMSUNG_BITS = 32;
        public const int WHYNTER_BITS = 32;

        public static readonly List<PulseModulationEncoding> IREncodings = new List<PulseModulationEncoding>()
        {
            //NEC
            new PulseModulationEncoding()
            {
                protocol = IR_PROTOCOL_NEC,
                headerMark = NEC_HDR_MARK,
                headerSpace = NEC_HDR_SPACE,
                numBits = 32,
                oneMark = NEC_BIT_MARK,
                oneSpace = NEC_ONE_SPACE,
                zeroMark = NEC_BIT_MARK,
                zeroSpace = NEC_ZERO_SPACE,
                stopMark = NEC_BIT_MARK,
                khz = 38
            },
            // Special NEC Repeat Codes
	        new PulseModulationEncoding()
            {
                protocol = IR_PROTOCOL_NEC_REPEAT,
                headerMark = NEC_HDR_MARK,
                headerSpace = NEC_RPT_SPACE,
                numBits = 0,
                oneMark = NEC_BIT_MARK,
                oneSpace = NEC_ONE_SPACE,
                zeroMark = NEC_BIT_MARK,
                zeroSpace = NEC_ZERO_SPACE,
                stopMark = NEC_BIT_MARK,
                khz = 38
            },
            new PulseModulationEncoding()
            {
            protocol = IR_PROTOCOL_SONY,
            headerMark = SONY_HDR_MARK,
            headerSpace = SONY_HDR_SPACE,
            numBits = SONY_BITS,
            oneMark = SONY_ONE_MARK,
            oneSpace = SONY_HDR_SPACE,
            zeroMark = SONY_ZERO_MARK,
            zeroSpace = SONY_HDR_SPACE,
            stopMark = 0,
            khz = 40
        },

            new PulseModulationEncoding()
            {
            protocol = IR_PROTOCOL_JVC,
            headerMark = JVC_HDR_MARK,
            headerSpace = JVC_HDR_SPACE,
            numBits = JVC_BITS,
            oneMark = JVC_BIT_MARK,
            oneSpace = JVC_ONE_SPACE,
            zeroMark = JVC_BIT_MARK,
            zeroSpace = JVC_ZERO_SPACE,
            stopMark = JVC_BIT_MARK,
            khz = 38
        },

            new PulseModulationEncoding()
            {
            protocol = IR_PROTOCOL_JVC_REPEAT,
            headerMark = 0, // No Header for repeat codes
		    headerSpace = 0,
            numBits = JVC_BITS,
            oneMark = JVC_BIT_MARK,
            oneSpace = JVC_ONE_SPACE,
            zeroMark = JVC_BIT_MARK,
            zeroSpace = JVC_ZERO_SPACE,
            stopMark = JVC_BIT_MARK,
            khz = 38
        },

            new PulseModulationEncoding()
            {
            protocol = IR_PROTOCOL_PANASONIC,
            headerMark = PANASONIC_HDR_MARK,
            headerSpace = PANASONIC_HDR_SPACE,
            numBits = PANASONIC_BITS,
            oneMark = PANASONIC_BIT_MARK,
            oneSpace = PANASONIC_ONE_SPACE,
            zeroMark = PANASONIC_BIT_MARK,
            zeroSpace = PANASONIC_ZERO_SPACE,
            stopMark = PANASONIC_BIT_MARK,
            khz = 35
        },

         new PulseModulationEncoding()
         {
            protocol = IR_PROTOCOL_LG,
            headerMark = LG_HDR_MARK,
            headerSpace = LG_HDR_SPACE,
            numBits = LG_BITS,
            oneMark = LG_BIT_MARK,
            oneSpace = LG_ONE_SPACE,
            zeroMark = LG_BIT_MARK,
            zeroSpace = LG_ZERO_SPACE,
            stopMark = LG_BIT_MARK,
            khz = 38
         }

        };

        public const int USECPERTICK = 50 ; // microseconds per clock interrupt tick
        public const int MARK_EXCESS = 0;

        public const int LTOL =  3;
        public const int UTOL =  5;

        public static int TICKS_LOW(int us) => (((us) * LTOL / (4 * USECPERTICK)));
        public static int TICKS_HIGH(int us) => (((us) * UTOL / (4 * USECPERTICK) + 1));

       // const int MIN_RC5_SAMPLES  =   11;
        public const int RC5_T1           =  889;

       // const int MIN_RC6_SAMPLES     = 1;
        public const int RC6_HDR_MARK    =  2666;
        public const int RC6_HDR_SPACE   =   889;
        public const int RC6_T1          =   444;
        public const int RC6_RPT_LENGTH  = 46000;


        public bool IRDecode(UInt16[] rawData, UInt16 rawLen,ref sbyte protocol, ref UInt32 value)
        {
            IRDecoder decoder= new IRDecoder(rawData,rawLen);

            if (decoder.decodeRC5(ref value))
            {
                protocol = IR_PROTOCOL_RC5;
                return true;
            }

            if (decoder.decodeRC6(ref value))
            {
                protocol = IR_PROTOCOL_RC6;
                return true;
            }

            for (int i = 0; i < NUM_IR_ENCODINGS; i++)
            {
                if (decoder.decodePulseModulation(IREncodings[i],ref value))
                {
                    protocol = (sbyte)IREncodings[i].protocol;
                    return true;
                }
            }

            return false;
        }
        static int encodePulseModulation(PulseModulationEncoding encoding,
                                            uint32_t data, uint8_t[] rawData, uint8_t maxLen)
        {

            // Index 0 is a space. Set it to 0 length.
            rawData[0] = 0;
            int length = 1;

            if ((encoding.headerMark != 0) && (length + 2 < maxLen))
            {
                rawData[length++] = (byte)(encoding.headerMark / USECPERTICK);
                rawData[length++] = (byte)(encoding.headerSpace / USECPERTICK);
            }

            for (int i = encoding.numBits - 1; i >= 0 && length + 2 < maxLen; i--)
            {
                if ((data & (1 << i)) != 0)
                {
                    rawData[length++] = (byte)(encoding.oneMark / USECPERTICK);
                    rawData[length++] = (byte)(encoding.oneSpace / USECPERTICK);
                }
                else {
                    rawData[length++] = (byte)(encoding.zeroMark / USECPERTICK);
                    rawData[length++] = (byte)(encoding.zeroSpace / USECPERTICK);
                }
            }

            if ((encoding.stopMark != 0) && length < maxLen)
            {
                rawData[length++] = (byte)(encoding.stopMark / USECPERTICK);
            }

            return length;
        }

        public int IREncode(byte protocol, uint32_t value,byte[] data, byte maxLen)
        {
            for (int i = 0; i < NUM_IR_ENCODINGS; i++)
            {
                if (IREncodings[i].protocol == protocol)
                {
                    return encodePulseModulation(IREncodings[i], value,
                        data, maxLen);
                }
            }

            return 0;
        }
    }
}
