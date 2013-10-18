#include <cstdint>
#include <map>
#include <string>
#include "cryptlib.h"
#include "osrng.h"
#include "modes.h"
#include "hex.h"
#include "files.h"

using namespace std;
using namespace CryptoPP;

static uint8_t key[] = 
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

static uint8_t iv[] =
{
	0x7C, 0xF4, 0xF0, 0x7D, 
	0x3B, 0x0D, 0xA1, 0xC6, 
	0x35, 0x74, 0x18, 0xB3, 
	0x51, 0xA3, 0x87, 0x8E 
};

extern "C"
{
_declspec(dllexport) int32_t Encrypt(const uint8_t *buff, int32_t count, uint8_t *out)
{
	CBC_Mode<AES>::Encryption encryptor(key, sizeof(key), iv);
	string plain((const char *)buff, static_cast<string::size_type>(count));
	string cipher;

	StringSource(plain,
		true,
		new StreamTransformationFilter(encryptor,
		new StringSink(cipher),
		BlockPaddingSchemeDef::BlockPaddingScheme::ZEROS_PADDING,
		true));

	memcpy(out, cipher.c_str(), cipher.length());
	return static_cast<int32_t>(cipher.length());
}
}