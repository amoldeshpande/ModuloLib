using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModuloLib
{
    using int16_t = Int16;
    using uint16_t = UInt16;
    using uint32_t = UInt32;

    public class IRDecoder
    {
        int16_t _currentIndex;
        uint16_t[] _rawData;
        uint16_t _rawLen;

        public IRDecoder(uint16_t[] rawData, uint16_t validLen)
        {
            _rawData = rawData;
            _rawLen = validLen;
        }

        public bool decodePulseModulation(PulseModulationEncoding encoding, ref uint32_t value)
        {
            _currentIndex = 0;
            value = 0;

            // Skip leading space
            getNextInterval();

            // Match the header
            if (encoding.headerMark != 0) {
                if (!matchMark(getNextInterval(), encoding.headerMark)) {
                    return false;
                }

                if (!matchSpace(getNextInterval(), encoding.headerSpace)) {
                    return false;
                }
            }

            // Match the data
            for (int i = 0; i < (int)encoding.numBits; i++) {
                uint16_t mark = getNextInterval();
                uint16_t space = getNextInterval();

                if (matchMark(mark, encoding.oneMark) &&
                    matchSpace(space, encoding.oneSpace)) {
                    value = (value << 1) | 1;
                } else if (matchMark(mark, encoding.zeroMark) &&
                           matchSpace(space, encoding.zeroSpace)) {
                    value <<= 1;
                } else {
                    return false;
                }
            }

            // Match the stop mark
            if (encoding.stopMark != 0) {
                if (!matchMark(getNextInterval(), encoding.stopMark)) {
                    return false;
                }
            }

            // Success!
            return true;
        }

        // Specialized decoding of the RC5 protocol.
        // In the future it would be nice to have a general manchester decoder.
        public bool decodeRC5(ref uint32_t value)
        {
            uint data = 0;
            int used = 0;
            _currentIndex = 1;  // Skip gap space

            if (_rawLen < IREncoding.MIN_RC5_SAMPLES + 2)
                return false;

            // Get start bits
            if (getRClevel(ref used, IREncoding.RC5_T1) != 1)
                return false;
            if (getRClevel(ref used, IREncoding.RC5_T1) != 0)
                return false;
            if (getRClevel(ref used, IREncoding.RC5_T1) != 1)
                return false;

            for (int nbits = 0; _currentIndex < _rawLen; nbits++)
            {
                int levelA = getRClevel(ref used, IREncoding.RC5_T1);
                int levelB = getRClevel(ref used, IREncoding.RC5_T1);

                if ((levelA == 0) && (levelB == 1))
                    data = (data << 1) | 1;
                else if ((levelA == 1) && (levelB == 0))
                    data = (data << 1) | 0;
                else
                    return false;
            }

            // Success
            value = data;
            return true;
        }

        // Specialized decoding of the RC6 protocol.
        // In the future it would be nice to have a general manchester decoder.
        public bool decodeRC6(ref uint32_t value)
        {
            uint data = 0;
            int used = 0;
            _currentIndex = 1;  // Skip first space

            if (_rawLen < IREncoding.MIN_RC6_SAMPLES)
                return false;

            // Initial mark
            if (!matchMark(_rawData[_currentIndex++], IREncoding.RC6_HDR_MARK))
                return false;

            if (!matchSpace(_rawData[_currentIndex++], IREncoding.RC6_HDR_SPACE))
                return false;

            // Get start bit (1)
            if (getRClevel(ref used, IREncoding.RC6_T1) != 1)
                return false;

            if (getRClevel(ref used, IREncoding.RC6_T1) != 0)
                return false;

            for (int nbits = 0; _currentIndex < _rawLen; nbits++)
            {
                int levelA, levelB;  // Next two levels

                levelA = getRClevel(ref used, IREncoding.RC6_T1);
                if (nbits == 3)
                {
                    // T bit is double wide; make sure second half matches
                    if (levelA != getRClevel(ref used, IREncoding.RC6_T1)) return false;
                }

                levelB = getRClevel(ref used, IREncoding.RC6_T1);
                if (nbits == 3)
                {
                    // T bit is double wide; make sure second half matches
                    if (levelB != getRClevel(ref used, IREncoding.RC6_T1)) return false;
                }

                if ((levelA == 1) && (levelB == 0))
                    data = (data << 1) | 1;  // inverted compared to RC5
                else if ((levelA == 0) && (levelB == 1))
                    data = (data << 1) | 0;  // ...
                else
                    return false;            // Error
            }

            // Success
            value = data;
            return true;
        }
        bool match(uint16_t measured, int16_t desired_us)
        {
            return (measured >= (short)IREncoding.TICKS_LOW(desired_us) && measured <= (short)IREncoding.TICKS_HIGH(desired_us));
        }

        bool matchMark(uint16_t measured, uint16_t desired_us)
        {
            return match(measured, (short)(desired_us + IREncoding.MARK_EXCESS));
        }

        bool matchSpace(uint16_t measured, uint16_t desired_us)
        {
            return match(measured, (short)(desired_us - IREncoding.MARK_EXCESS));
        }

        uint16_t getNextInterval()
        {
            // Return 0 if we have gone off the end of the data
            if (_currentIndex >= _rawLen)
            {
                return 0;
            }

            // Get the next interval
            uint16_t next = _rawData[_currentIndex++];

            // 0 is a special token indicating that we should read a 16 bit value
            if (next == 0)
            {
                next = _rawData[_currentIndex++];
                next |= (ushort)(_rawData[_currentIndex++] << 8);
            }
            return next;
        }

        // Gets one undecoded level at a time from the raw buffer.
        // The RC5/6 decoding is easier if the data is broken into time intervals.
        // E.g. if the buffer has MARK for 2 time intervals and SPACE for 1,
        // successive calls to getRClevel will return MARK, MARK, SPACE.
        // offset and used are updated to keep track of the current position.
        // t1 is the time interval for a single bit in microseconds.
        // Returns -1 for error (measured time interval is not a multiple of t1).
        //
        int getRClevel(ref int used, int t1)
        {
            if (_currentIndex >= _rawLen)
                return 0;  // After end of recorded buffer, assume SPACE.
            ushort width = _rawData[_currentIndex];
            int val = (_currentIndex % 2);
            int correction = (val != 0) ? IREncoding.MARK_EXCESS : -IREncoding.MARK_EXCESS;

            int avail = 0;
            if (match(width, (short)((t1) + correction)))
                avail = 1;
            else if (match(width, (short)((2 * t1) + correction)))
                avail = 2;
            else if (match(width, (short)((3 * t1) + correction)))
                avail = 3;
            else
                return -1;

            (used)++;
            if (used >= avail)
            {
                used = 0;
                _currentIndex++;
            }

            return val;
        }

    }
}
