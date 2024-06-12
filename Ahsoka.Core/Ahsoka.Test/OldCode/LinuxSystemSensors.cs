using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ahsoka.Services.System.Platform;

internal static class LinuxSystemSensors
{
    // NOTE: The following two enumerations and the definition for the structs defined below came from the sensors.h
    // file that comes included with the lm-sensors library. You can get the sensors.h file from here:
    // https://raw.githubusercontent.com/lm-sensors/lm-sensors/master/lib/sensors.h
    /// <summary>
    /// Note from Jake: Moved this to Test Project to avoid usage
    /// </summary>

    #region Enumerations
    private enum SensorsFeatureType
    {
        In = 0x00,
        Fan = 0x01,
        Temp = 0x02,
        Power = 0x03,
        Energy = 0x04,
        Curr = 0x05,
        Humidity = 0x06,
        MaxMain,
        Vid = 0x10,
        Intrusion = 0x11,
        MaxOther,
        BeepEnable = 0x18,
        Max,
        Unknown = Int32.MaxValue,
    }

    private enum SensorsSubfeatureType
    {
        InInput = SensorsFeatureType.In << 8,
        InMin,
        InMax,
        InLcrit,
        InCrit,
        InAverage,
        InLowest,
        InHighest,
        InAlarm = (SensorsFeatureType.In << 8) | 0x80,
        InMinAlarm,
        InMaxAlarm,
        InBeep,
        InLcritAlarm,
        InCritAlarm,

        FanInput = SensorsFeatureType.Fan << 8,
        FanMin,
        FanMax,
        FanAlarm = (SensorsFeatureType.Fan << 8) | 0x80,
        FanFault,
        FanDiv,
        FanBeep,
        FanPulses,
        FanMinAlarm,
        FanMaxAlarm,

        TempInput = SensorsFeatureType.Temp << 8,
        TempMax,
        TempMaxHyst,
        TempMin,
        TempCrit,
        TempCritHyst,
        TempLcrit,
        TempEmergency,
        TempEmergencyHyst,
        TempLowest,
        TempHighest,
        TempMinHyst,
        TempLcritHyst,
        TempAlarm = (SensorsFeatureType.Temp << 8) | 0x80,
        TempMaxAlarm,
        TempMinAlarm,
        TempCritAlarm,
        TempFault,
        TempType,
        TempOffset,
        TempBeep,
        TempEmergencyAlarm,
        TempLcritAlarm,

        PowerAverage = SensorsFeatureType.Power << 8,
        PowerAverageHighest,
        PowerAverageLowest,
        PowerInput,
        PowerInputHighest,
        PowerInputLowest,
        PowerCap,
        PowerCapHyst,
        PowerMax,
        PowerCrit,
        PowerMin,
        PowerLcrit,
        PowerAverageInterval = (SensorsFeatureType.Power << 8) | 0x80,
        PowerAlarm,
        PowerCapAlarm,
        PowerMaxAlarm,
        PowerCritAlarm,
        PowerMinAlarm,
        PowerLcritAlarm,

        EnergyInput = SensorsFeatureType.Energy << 8,

        CurrInput = SensorsFeatureType.Curr << 8,
        CurrMin,
        CurrMax,
        CurrLcrit,
        CurrCrit,
        CurrAverage,
        CurrLowest,
        CurrHighest,
        CurrAlarm = (SensorsFeatureType.Curr << 8) | 0x80,
        CurrMinAlarm,
        CurrMaxAlarm,
        CurrBeep,
        CurrLcritAlarm,
        CurrCritAlarm,

        HumidityInput = SensorsFeatureType.Humidity << 8,

        Vid = SensorsFeatureType.Vid << 8,

        IntrusionAlarm = SensorsFeatureType.Intrusion << 8,
        IntrusionBeep,

        BeepEnable = SensorsFeatureType.BeepEnable << 8,

        Unknown = Int32.MaxValue,
    }
    #endregion

    #region MarshalledStructures
    [StructLayout(LayoutKind.Sequential)]
    private struct SensorsBusID
    {
        public Int16 type;
        public Int16 nr;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SensorsChipName
    {
        // NOTE: The prefix parameter is a string, but we are not marshalling it as a string because in certain cases there
        // is a buffer that is allocated by the sensor functions and we want to avoid a scenario where that buffer is freed
        // by both the managed and unmanaged sides of the program.
        public IntPtr prefix;
        public SensorsBusID bus;
        public Int32 addr;
        [MarshalAs(UnmanagedType.LPStr)]
        public string path;

        public string prefixString()
        {
            if (prefix != IntPtr.Zero)
            {
                return Marshal.PtrToStringAnsi(prefix)!;
            }
            else
            {
                return "";
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SensorsFeature
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;
        public Int32 number;
        public SensorsFeatureType type;
        public Int32 firstSubfeature;
        public Int32 padding1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SensorsSubfeature
    {
        [MarshalAs(UnmanagedType.LPStr)]
        public string name;
        public Int32 number;
        public SensorsSubfeatureType type;
        public Int32 mapping;
        public UInt32 flags;
    }
    #endregion

    #region Invocations
    /// <summary>
    /// Load the configuration file and the detected chips list. If this returns a value unequal to zero, you are in
    /// trouble; you can not assume anything will be initialized properly. If you want to reload the configuration file,
    /// call sensors_cleanup() below before calling sensors_init() again.
    /// </summary>
    /// <param name="handle"></param>
    /// <returns></returns>
    [DllImport("sensors", EntryPoint = "sensors_init", CharSet = CharSet.Ansi)]
    private static extern int Init(SafeFileHandle handle);

    /// <summary>
    /// Parse a chip name to the internal representation. Return 0 on success, &lt;0 on error.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="res"></param>
    /// <returns></returns>
    [DllImport("sensors", EntryPoint = "sensors_parse_chip_name", CharSet = CharSet.Ansi)]
    private static extern int ParseChipName(string name, ref SensorsChipName res);

    /// <summary>
    /// This function returns all detected chips that match a given chip name, one by one. If no chip name is provided,
    /// all detected chips are returned. To start at the beginning of the list, use 0 for nr; An error is thrown if we
    /// are at the end of the list. Do not try to change these chip names, as they point to internal structures!
    /// </summary>
    /// <param name="match"></param>
    /// <param name="nr"></param>
    /// <returns></returns>
    [DllImport("sensors", EntryPoint = "sensors_get_detected_chips", CharSet = CharSet.Ansi)]
    private static extern IntPtr GetDetectedChips(ref SensorsChipName match, ref int nr);

    /// <summary>
    /// Print a chip name from its internal representation. Note that chip should not contain wildcare values! Return
    /// the number of characters printed on success (same as snprintf), &lt;0 on error.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="size"></param>
    /// <param name="match"></param>
    /// <returns></returns>
    [DllImport("sensors", EntryPoint = "sensors_snprintf_chip_name", CharSet = CharSet.Ansi)]
    private static extern int SnprintfChipName([Out] char[] str, IntPtr size, ref SensorsChipName match);

    /// <summary>
    /// This returns all main features of a specific chip. nr is an internally used variable. Set it to zero to start at
    /// the begin of the list. If no more features are found an exception is thrown. Do not try to change the returned
    /// structure; you will corrupt internal data structures.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="nr"></param>
    /// <returns></returns>
    [DllImport("sensors", EntryPoint = "sensors_get_features", CharSet = CharSet.Ansi)]
    private static extern IntPtr GetFeatures(ref SensorsChipName name, ref int nr);

    [DllImport("sensors", EntryPoint = "sensors_get_subfeature", CharSet = CharSet.Ansi)]
    private static extern IntPtr _GetSubfeature(ref SensorsChipName name, ref SensorsFeature feature, SensorsSubfeatureType type);
    /// <summary>
    /// This returns the subfeature of the given type for a given main feature, if it exists, an exception is thrown
    /// otherwise. Do not try to change the returned structure; you will corrupt internal data structures.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="feature"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    private static SensorsSubfeature GetSubfeature(ref SensorsChipName name, ref SensorsFeature feature, SensorsSubfeatureType type)
    {
        IntPtr subfeaturePtr = _GetSubfeature(ref name, ref feature, type);
        if (subfeaturePtr != IntPtr.Zero)
        {
            return Marshal.PtrToStructure<SensorsSubfeature>(subfeaturePtr);
        }
        else
        {
            throw new NullReferenceException("Function returned a null pointer value.");
        }
    }

    [DllImport("sensors", EntryPoint = "sensors_get_label", CharSet = CharSet.Ansi)]
    private static extern IntPtr _GetLabel(ref SensorsChipName name, ref SensorsFeature feature);
    /// <summary>
    /// Look up the label for a given feature. Note that chip should not contain wildcard values! The returned string is
    /// newly allocated (free it yourself). On failure, an exception is returned. If no label exists for this feature,
    /// its name is returned itself.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="feature"></param>
    /// <returns></returns>
    private static string GetLabel(ref SensorsChipName name, ref SensorsFeature feature)
    {
        // NOTE: We have to marshal the string that is created here because it is allocated from within the get label
        // function, this guarantees that the string that is returned is properly managed a freed after it is created
        // from within the unmanaged code.
        string label = Marshal.PtrToStringAnsi(_GetLabel(ref name, ref feature));
        if (label == null)
        {
            throw new NullReferenceException("Function returned a null pointer to a string.");
        }
        else
        {
            return label;
        }
    }

    /// <summary>
    /// Read the value of a subfeature of a certain chip. Note that chip should not contain wildcard values! This
    /// function will return 0 on success, and &lt;0 on failure.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="subfeat_nr"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    [DllImport("sensors", EntryPoint = "sensors_get_value", CharSet = CharSet.Ansi)]
    private static extern int GetValue(ref SensorsChipName name, int subfeat_nr, ref double value);

    /// <summary>
    /// Free memory allocated for the internal representation of a chip name.
    /// </summary>
    /// <param name="bres"></param>
    [DllImport("sensors", EntryPoint = "sensors_free_chip_name", CharSet = CharSet.Ansi)]
    private static extern void FreeChipName(ref SensorsChipName bres);

    /// <summary>
    /// Cleanup-function: You can't access anything after this, until the next Init() call!
    /// </summary>
    [DllImport("sensors", EntryPoint = "sensors_cleanup", CharSet = CharSet.Ansi)]
    private static extern void Cleanup();
    #endregion

    static LinuxSystemSensors()
    {
        // Here we are basically creating our own null file pointer because there is not a default one in C#
        SafeFileHandle filePointer = new(IntPtr.Zero, false);
        Init(filePointer);
    }

    private static readonly Dictionary<string, IntPtr> chipDict = new();
    private static readonly Dictionary<string, IntPtr> featureDict = new();

    private static SensorsChipName GetChipFromString(string chipName)
    {
        if (!chipDict.TryGetValue(chipName, out IntPtr chipValue))
        {
            SensorsChipName match = new();
            if (ParseChipName(chipName, ref match) != 0)
            {
                throw new Exception("There was a problem parsing the name of the chip that was provided.");
            }

            int nrZero = 0;
            IntPtr chip = GetDetectedChips(ref match, ref nrZero);
            if (chip != IntPtr.Zero)
                chipDict[chipName] = chip;

            FreeChipName(ref match);
        }

        if (chipValue != IntPtr.Zero)
            return Marshal.PtrToStructure<SensorsChipName>(chipDict[chipName]);
        else
            return new SensorsChipName();
    }

    private static SensorsFeature GetFeatureFromString(string chipName, string featureName)
    {
        if (!featureDict.ContainsKey(featureName))
        {
            SensorsChipName chip = GetChipFromString(chipName);

            IntPtr featurePtr;
            SensorsFeature feature = new();
            int nrZeroFeature = 0;
            bool featureFound = false;
            while (true)
            {
                featurePtr = GetFeatures(ref chip, ref nrZeroFeature);
                feature = Marshal.PtrToStructure<SensorsFeature>(featurePtr);

                if (featureName != GetLabel(ref chip, ref feature))
                {
                    continue;
                }
                else
                {
                    // Once we have successfully match the label that was provided break out of the while loop.
                    featureFound = true;
                    break;
                }
            }

            if (!featureFound)
            {
                throw new Exception("There was a problem finding a feature that matched the string that was provided.");
            }

            featureDict[featureName] = featurePtr;
        }

        return Marshal.PtrToStructure<SensorsFeature>(featureDict[featureName]);
    }

    /// <summary>
    /// Given the chip name and the feature name for a sensor value, this function will return the corresponding double
    /// value.
    ///
    /// NOTE: If you're going to be getting a sensor value, make sure that the corresponding sensor type is supported
    /// within this function and that you're pulling the correct feature value.
    ///
    /// In order to use this functionality you will need to make sure that your system has the lm-sensors library
    /// installed, named libsensors.so. Along with libsensors.so, there is also a demo application that is usually
    /// included called "sensors". If you call this "sensors" binary you will be shown all the sensors within the system
    /// that you are running on. Using this executable you can find the chip and feature name for the sensor that you
    /// are interested in. The output of the "sensors" function will look something like the following:
    ///
    /// ```
    /// cpu_thermal-virtual-0
    /// Adapter: Virtual device
    /// temp1:        +58.9 C  (crit = +120.0 C)
    /// ```
    ///
    /// If we are interested in getting the value of temp1, we would use "temp1" as the feature name, and the chip would
    /// be the "cpu_thermal-virtual-0" name above it. With just these two values, you will be able to get the value that
    /// you're interested in.
    /// </summary>
    /// <param name="chipName"></param>
    /// <param name="featureName"></param>
    /// <returns>All sensor values are denoted as doubles.</returns>
    /// <exception cref="Exception"></exception>
    public static double GetSensorValue(string chipName, string featureName)
    {
        SensorsChipName chip = GetChipFromString(chipName);

        if (String.IsNullOrEmpty(chip.prefixString()))
            return 0;

        SensorsFeature feature = GetFeatureFromString(chipName, featureName);
        var
                // The default temperature unit is in deg C
                subfeatureType = feature.type switch
                {
                    SensorsFeatureType.Temp => SensorsSubfeatureType.TempInput,// The default temperature unit is in deg C
                                                                               // Note: If we are going to plan to support other sensor types, their corresponding feature type needs to be
                                                                               // filled out here. You will also need to make sure to note the units of the value that is being returned.
                    _ => throw new Exception(string.Format("Unsupported type reached. {0}", feature.name)),
                };
        double value = 0;
        SensorsSubfeature subfeature = GetSubfeature(ref chip, ref feature, subfeatureType);
        if (GetValue(ref chip, subfeature.number, ref value) != 0)
        {
        }

        return value;
    }
}

