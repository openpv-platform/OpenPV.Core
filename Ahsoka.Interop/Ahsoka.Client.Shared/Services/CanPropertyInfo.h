#pragma once
#include <string>
#include <functional>
#include <memory>
#include <math.h>
#include "AhsokaServices.h"

using namespace std;
using namespace AhsokaCAN;

namespace AhsokaCAN
{
    class CanPropertyInfo
    {
        public: 

            CanPropertyInfo( ){}
            CanPropertyInfo(const CanPropertyInfo& copy )
            {
                scale = copy.scale;
                offset = copy.offset;
                startBit = copy.startBit;
                bitLength = copy.bitLength;
                byteOrder = copy.byteOrder;
                dataType = copy.dataType;
                signalId = copy.signalId;
            }

            CanPropertyInfo(int astartBit, int abitLength, ByteOrder abyteOrder, ValueType adataType, double ascale, double aoffset, int signal = -1)
            {
                scale = ascale;
                offset = aoffset;
                startBit = astartBit;
                bitLength = abitLength;
                byteOrder = abyteOrder;
                dataType = adataType;
                signalId = signal;
            }
        
            template <typename T>
            void SetValue(string * data, T newValue, bool scaleValue = true) 
            {
                uint64_t longData[data->size() / 8];

                memcpy((void*)&longData, (void*)data->c_str(), data->size());

                SetValue<T>(longData, newValue, scaleValue);

                memcpy((void*)data->c_str(),(void*) &longData , data->size());
            }

            template <typename T>
            T GetValue(string * data, bool scaleValue = true) 
            {
                uint64_t longData[data->size() / 8];
                
                memcpy(&longData, data->c_str(), data->size());

                return GetValue<T>(longData, scaleValue);
            }

            template <typename T>
            void SetValue(uint64_t data[], T newValue, bool scaleValue = true) 
            {
                int startByte = startBit / 8;
                int messageIndex = startByte / 8;

                // Clear Bits
                data[messageIndex] &= ~(BitMask() << startBit);

                // Set New Value
                data[messageIndex] |= Pack(newValue, scaleValue);
            }

            template <typename T>
            T GetValue(uint64_t data[], bool scaleValue = true) 
            {
                int startByte = startBit / 8;
                int messageIndex = startByte / 8;

                T retVal;

                if (dataType == ValueType::Signed)
                    return (T)Unpack(data[messageIndex], scaleValue);
                else if (dataType == ValueType::Unsigned)
                    return (T)Unpack(data[messageIndex], scaleValue);
                else if (dataType == ValueType::Float)
                    return (T)Unpack(data[messageIndex], scaleValue);
                else if (dataType == ValueType::Double)
                    return (T)Unpack(data[messageIndex], scaleValue);
                else if (dataType == ValueType::Enum)
                    return (T)(Unpack(data[messageIndex]));
                
                return retVal;
            }
        
            uint64_t Pack(double value, bool scaleValue = true)
            {
                long iVal;
                uint64_t bitMask = BitMask();

                // Apply scaling
                auto rawValue = scaleValue ? (value - offset) / scale : value;

                // Convert to Byte[8]
                if (dataType == ValueType::Float)
                    iVal =   (long)(float)rawValue; 
                else if (dataType == ValueType::Double)
                    iVal =  (long)(double)rawValue; 
                else
                    iVal = (long)std::round(rawValue);                

                // Pack signal
                if (byteOrder == ByteOrder::LittleEndian) // Little endian 
                     return (((uint64_t)iVal & bitMask) << startBit);
                else // Big endian
                    return MirrorMsg(((uint64_t)iVal & bitMask) << GetStartBitLE());
            }

            double Unpack(uint64_t data, bool scaleValue = true)
            {
                long iVal;
                uint64_t bitMask = BitMask();

                // Unpack signal
                if (byteOrder == ByteOrder::LittleEndian) // Little endian 
                    iVal = (long)((data >> startBit) & bitMask);
                else // Big endian 
                    iVal = (long)((MirrorMsg(data) >> GetStartBitLE()) & bitMask);

                if (dataType == ValueType::Float)
                {
                    auto retVal = (float)iVal;
                    if (scaleValue)
                        retVal = (float)(retVal * scale + offset);

                    return retVal;
                }
                else if (dataType == ValueType::Double)
                {
                    auto retVal = (double)iVal;
                    if (scale)
                        retVal = retVal * scale + offset;

                    return retVal;
                }

                // Apply scaling for Int Values
                if (scaleValue)
                    return iVal * scale + offset;
                
                return iVal;
            }

            uint64_t BitMask()
            {
                return (ULLONG_MAX >> (64 - bitLength));
            }

            uint8_t GetStartBitLE()
            {
                uint8_t startByte = (uint8_t)(startBit / 8);
                return (uint8_t)(64 - (bitLength + 8 * startByte + (8 * (startByte + 1) - (startBit + 1)) % 8));
            }

            uint64_t MirrorMsg(uint64_t msg)
            {
                uint64_t swapped =  ((0x00000000000000FF) & (msg >> 56)
                                | (0x000000000000FF00) & (msg >> 40)
                                | (0x0000000000FF0000) & (msg >> 24)
                                | (0x00000000FF000000) & (msg >> 8)
                                | (0x000000FF00000000) & (msg << 8)
                                | (0x0000FF0000000000) & (msg << 24)
                                | (0x00FF000000000000) & (msg << 40)
                                | (0xFF00000000000000) & (msg << 56));

                return swapped;
            }

            int GetSignalId() { return signalId; }
            ValueType GetDataType() { return dataType; }      
            int GetStartBit() const { return startBit; }
            int GetBitLength() const { return bitLength; }

        private: 
            
            int signalId;
            double scale;
            double offset; 
            int startBit;
            int bitLength;
            ByteOrder byteOrder;
            ValueType dataType;

    };
}