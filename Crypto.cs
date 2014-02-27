using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DNT.Database
{
  class Crypto
  {
    static byte[] key = 
    {
      0xFA, 0xC2, 0xCC, 0x82, 
      0x8C, 0xFD, 0x42, 0x17, 
      0xA0, 0xB2, 0x97, 0x4D, 
      0x19, 0xC8, 0xA4, 0xB1, 
      0xF5, 0x73, 0x23, 0x7C, 
      0xB1, 0xC4, 0xC0, 0x38, 
      0xC9, 0x80, 0xB9, 0xF7, 
      0xC3, 0x3E, 0xC9, 0x12 
    };

    static byte[] iv = 
    {
      0x7C, 0xF4, 0xF0, 0x7D, 
      0x3B, 0x0D, 0xA1, 0xC6, 
      0x35, 0x74, 0x18, 0xB3, 
      0x51, 0xA3, 0x87, 0x8E
    };

    static Dictionary<byte[], byte[]> byteMap = new Dictionary<byte[], byte[]>(new ByteArrayComparer());

    public static byte[] EncryptBytesToBytes(byte[] plainBytes)
    {
      // Check arguments
      if (plainBytes == null || plainBytes.Length <= 0)
        throw new ArgumentException("plainBytes");

      if (byteMap.ContainsKey(plainBytes))
        return byteMap[plainBytes];

      byte[] cipherBytes = null;

      using (Aes alg = Aes.Create())
      {
        alg.Key = key;
        alg.IV = iv;
        alg.Mode = CipherMode.CBC;
        //alg.Padding = PaddingMode.Zeros;
        alg.Padding = PaddingMode.PKCS7;

        using (ICryptoTransform encryptor = alg.CreateEncryptor(alg.Key, alg.IV))
        {
          using (MemoryStream ms = new MemoryStream())
          {
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
              cs.Write(plainBytes, 0, plainBytes.Length);
              cs.FlushFinalBlock();
              cipherBytes = ms.ToArray();
            }
          }
        }
      }

      byteMap.Add(plainBytes, cipherBytes);
      return cipherBytes;
    }

    public static byte[] EncryptStringToBytes(string plainText)
    {
      byte[] plainBytes = UTF8Encoding.UTF8.GetBytes(plainText);
      return EncryptBytesToBytes(plainBytes);
    }
  }
}
