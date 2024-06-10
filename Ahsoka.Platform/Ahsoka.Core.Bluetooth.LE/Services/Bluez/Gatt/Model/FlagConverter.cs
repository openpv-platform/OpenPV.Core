using System.Collections.Generic;
using System.Linq;

namespace Ahsoka.Services.Bluetooth.LE.Bluez.Gatt.Model;

internal class FlagConverter
{
    public static readonly Dictionary<BLECharacteristicFlag, string> CharacteristicFlagToStringDict = new()
    {
        { BLECharacteristicFlag.Broadcast                       , "broadcast" },
        { BLECharacteristicFlag.Read                            , "read" },
        { BLECharacteristicFlag.WriteWithoutResponse            , "write-without-response" },
        { BLECharacteristicFlag.Write                           , "write" },
        { BLECharacteristicFlag.Notify                          , "notify" },
        { BLECharacteristicFlag.Indicate                        , "indicate" },
        { BLECharacteristicFlag.AuthenticatedSignedWrites       , "authenticated-signed-writes" },
        { BLECharacteristicFlag.ExtendedProperties              , "extended-properties" },
        { BLECharacteristicFlag.ReliableWrite                   , "reliable-write" },
        { BLECharacteristicFlag.WritableAuxiliaries             , "writable-auxiliaries" },
        { BLECharacteristicFlag.EncryptRead                     , "encrypt-read" },
        { BLECharacteristicFlag.EncryptWrite                    , "encrypt-write" },
        { BLECharacteristicFlag.EncryptNotify                   , "encrypt-notify" },
        { BLECharacteristicFlag.EncryptIndicate                 , "encrypt-indicate" },
        { BLECharacteristicFlag.EncryptAuthenticatedRead        , "encrypt-authenticated-read" },
        { BLECharacteristicFlag.EncryptAuthenticatedWrite       , "encrypt-authenticated-write" },
        { BLECharacteristicFlag.EncryptAuthenticatedNotify      , "encrypt-authenticated-notify" },
        { BLECharacteristicFlag.EncryptAuthenticatedIndicate    , "encrypt-authenticated-indicate" },
        { BLECharacteristicFlag.SecureRead                      , "secure-read" },
        { BLECharacteristicFlag.SecureWrite                     , "secure-write" },
        { BLECharacteristicFlag.SecureNotify                    , "secure-notify" },
        { BLECharacteristicFlag.SecureIndicate                  , "secure-indicate" },
        { BLECharacteristicFlag.Authorize                       , "authorize" }
    };

    public static string[] ConvertCharacteristicFlags(List<BLECharacteristicFlag> characteristicFlags)
    {
        return characteristicFlags.Distinct().Select(flag => CharacteristicFlagToStringDict[flag]).ToArray();
    }

    public static readonly Dictionary<BLEDescriptorFlag, string> DescriptorFlagToStringDict = new()
    {
        { BLEDescriptorFlag.DescriptorRead                       , "read" },
        { BLEDescriptorFlag.DescriptorWrite                      , "write" },
        { BLEDescriptorFlag.DescriptorEncryptRead                , "encrypt-read" },
        { BLEDescriptorFlag.DescriptorEncryptWrite               , "encrypt-write" },
        { BLEDescriptorFlag.DescriptorEncryptAuthenticatedRead   , "encrypt-authenticated-read" },
        { BLEDescriptorFlag.DescriptorEncryptAuthenticatedWrite  , "encrypt-authenticated-write" },
        { BLEDescriptorFlag.DescriptorSecureRead                 , "secure-read" },
        { BLEDescriptorFlag.DescriptorSecureWrite                , "secure-write" },
        { BLEDescriptorFlag.DescriptorAuthorize                  , "authorize" },
    };

    public static string[] ConvertDescriptorFlags(List<BLEDescriptorFlag> descriptorFlags)
    {
        return descriptorFlags.Distinct().Select(flag => DescriptorFlagToStringDict[flag]).ToArray();
    }
}