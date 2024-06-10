using System;

namespace Ahsoka.Services.Bluez.Gatt.Model;

internal class ReadWriteUtility
{
    public const string ReadOffsetKeyString = "offset";
    public const string CharacteristicUserDescriptionUUID = "00002901-0000-1000-8000-00805F9B34FB";

    public static byte[] GetValueAtOffset(byte[] value, int offset)
    {
        // Apply a floor of size 0 to the new Array
        int newArraySize = Math.Max(value.Length - offset, 0);

        byte[] returnArray = new byte[newArraySize];
        // If there is data in the return array, make sure we copy over that data.
        if (newArraySize > 0)
            Array.Copy(value, offset, returnArray, 0, newArraySize);

        return returnArray;
    }

    public static byte[] SetValueWithOffsetOnArray(byte[] startingArray, byte[] valueArray, ushort valueOffset)
    {
        // NOTE: I'm not 100% sure this is the way it typically works, but it's what made sense. The size of the
        // valueArray would be the existing length plus the offset.
        int returnArraySize = valueArray.Length + valueOffset;
        byte[] returnArray = new byte[returnArraySize];
        // Copy the starting array to this new array we've created.
        Array.Copy(startingArray, 0, returnArray, 0, Math.Min(startingArray.Length, returnArraySize));

        // Copy the value array to the return ARray at the offset.
        Array.Copy(valueArray, 0, returnArray, valueOffset, valueArray.Length);

        return returnArray;
    }
}
