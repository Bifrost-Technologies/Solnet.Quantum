using Markdig.Extensions.Tables;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Solnet.Wallet.Bip39;
using Solnet.Wallet.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solnet.Quantum
{

    [DebuggerDisplay("QMnemonic = {_mnemonic}")]
    public class QMnemonic
    {
        //
        // Summary:
        //     The word count array.
        private static readonly int[] MsArray = new int[5] { 12, 15, 18, 21, 24 };

        //
        // Summary:
        //     The bit count array.
        private static readonly int[] CsArray = new int[5] { 4, 5, 6, 7, 8 };

        //
        // Summary:
        //     The entropy value array.
        private static readonly int[] EntArray = new int[5] { 128, 160, 192, 224, 256 };

        //
        // Summary:
        //     Whether the checksum of the mnemonic is valid.
        private bool? _isValidChecksum;

        //
        // Summary:
        //     Utf8 encoding.
        private static readonly Encoding _noBomutf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        //
        // Summary:
        //     Whether the OS normalization is supported.
        private static bool? _supportOsNormalization;

        //
        // Summary:
        //     The mnemonic string.
        private readonly string _mnemonic;

        //
        // Summary:
        //     Whether the checksum of the mnemonic is valid.
        public bool IsValidChecksum
        {
            get
            {
                if (_isValidChecksum.HasValue)
                {
                    return _isValidChecksum.Value;
                }

                int num = Array.IndexOf(MsArray, Indices.Length);
                int bitCount = CsArray[num];
                int bitCount2 = EntArray[num];
                BitWriter bitWriter = new();
                BitArray bitArray = WordList.ToBits(Indices);
                bitWriter.Write(bitArray, bitCount2);
                byte[] bytes = Utils.Sha256(bitWriter.ToBytes());
                bitWriter.Write(bytes, bitCount);
                int[] first = bitWriter.ToIntegers();
                _isValidChecksum = first.SequenceEqual(Indices);
                return _isValidChecksum.Value;
            }
        }

        //
        // Summary:
        //     The word list.
        public WordList WordList { get; }

        //
        // Summary:
        //     The indices.
        public int[] Indices { get; }

        //
        // Summary:
        //     The words of the mnemonic.
        public string[] Words { get; }

        //
        // Summary:
        //     Initialize a mnemonic from the given string and wordList type.
        //
        // Parameters:
        //   mnemonic:
        //     The mnemonic string.
        //
        //   wordList:
        //     The word list type.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     Thrown when the mnemonic string is null.
        //
        //   T:System.FormatException:
        //     Thrown when the word count of the mnemonic is invalid.
        public QMnemonic(string mnemonic, WordList wordList = null)
        {
            if (mnemonic == null)
            {
                throw new ArgumentNullException("mnemonic");
            }

            _mnemonic = mnemonic.Trim();
            if (wordList == null)
            {
                wordList = WordList.AutoDetect(mnemonic) ?? WordList.English;
            }

            string[] array = mnemonic.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            _mnemonic = string.Join(wordList.Space.ToString(), array);
            if (!CorrectWordCount(array.Length))
            {
                throw new FormatException("Word count should be 12,15,18,21 or 24");
            }

            Words = array;
            WordList = wordList;
            Indices = wordList.ToIndices(array);
        }

        //
        // Summary:
        //     Generate a mnemonic
        //
        // Parameters:
        //   wordList:
        //     The word list of the mnemonic.
        //
        //   entropy:
        //     The entropy.
        private QMnemonic(WordList wordList, byte[] entropy = null)
        {
            if (wordList == null)
            {
                wordList = WordList.English;
            }

            WordList = wordList;
            if (entropy == null)
            {
                entropy = RandomUtils.GetBytes(32);
            }

            int num = Array.IndexOf(EntArray, entropy.Length * 8);
            if (num == -1)
            {
                throw new ArgumentException("The length for entropy should be " + string.Join(",", EntArray) + " bits", "entropy");
            }

            int bitCount = CsArray[num];
            byte[] bytes = Utils.Sha256(entropy);
            BitWriter bitWriter = new BitWriter();
            bitWriter.Write(entropy);
            bitWriter.Write(bytes, bitCount);
            Indices = bitWriter.ToIntegers();
            Words = WordList.GetWords(Indices);
            _mnemonic = WordList.GetSentence(Indices);
        }

        //
        // Summary:
        //     Initialize a mnemonic from the given word list and word count..
        //
        // Parameters:
        //   wordList:
        //     The word list.
        //
        //   wordCount:
        //     The word count.
        public Mnemonic(WordList wordList, WordCount wordCount)
            : this(wordList, GenerateEntropy(wordCount))
        {
        }

        //
        // Summary:
        //     Generate entropy for the given word count.
        //
        // Parameters:
        //   wordCount:
        //
        // Exceptions:
        //   T:System.ArgumentException:
        //     Thrown when the word count is invalid.
        private static byte[] GenerateEntropy(WordCount wordCount)
        {
            if (!CorrectWordCount((int)wordCount))
            {
                throw new ArgumentException("Word count should be 12,15,18,21 or 24", "wordCount");
            }

            int num = Array.IndexOf(MsArray, (int)wordCount);
            return RandomUtils.GetBytes(EntArray[num] / 8);
        }

        //
        // Summary:
        //     Whether the word count is correct.
        //
        // Parameters:
        //   ms:
        //     The number of words.
        //
        // Returns:
        //     True if it is, otherwise false.
        private static bool CorrectWordCount(int ms)
        {
            return MsArray.Any((int _) => _ == ms);
        }

        //
        // Summary:
        //     Derives the mnemonic seed.
        //
        // Parameters:
        //   passphrase:
        //     The passphrase.
        //
        // Returns:
        //     The seed.
        public byte[] DeriveSeed(string passphrase = null)
        {
            if (passphrase == null)
            {
                passphrase = "";
            }

            byte[] salt = Concat(_noBomutf8.GetBytes("mnemonic"), Normalize(passphrase));
            return GenerateSeed(Normalize(_mnemonic), salt);
        }

        //
        // Summary:
        //     Generate the seed using pbkdf with sha 512.
        //
        // Parameters:
        //   password:
        //     The password to derive the key from.
        //
        //   salt:
        //     The salt to use for key derivation.
        //
        // Returns:
        //     The derived key.
        private static byte[] GenerateSeed(byte[] password, byte[] salt)
        {
            Pkcs5S2ParametersGenerator pkcs5S2ParametersGenerator = new Pkcs5S2ParametersGenerator(new Sha512Digest());
            pkcs5S2ParametersGenerator.Init(password, salt, 2048);
            return ((KeyParameter)pkcs5S2ParametersGenerator.GenerateDerivedParameters(512)).GetKey();
        }

        //
        // Summary:
        //     Get the normalized the string as a byte array.
        //
        // Parameters:
        //   str:
        //     The string to normalize.
        //
        // Returns:
        //     The byte array.
        private static byte[] Normalize(string str)
        {
            return _noBomutf8.GetBytes(NormalizeString(str));
        }

        //
        // Summary:
        //     Normalize the string.
        //
        // Parameters:
        //   word:
        //     The string to normalize.
        //
        // Returns:
        //     The normalized string.
        internal static string NormalizeString(string word)
        {
            if (SupportOsNormalization())
            {
                return word.Normalize(NormalizationForm.FormKD);
            }

            return KdTable.NormalizeKd(word);
        }

        //
        // Summary:
        //     Checks for OS normalization support.
        //
        // Returns:
        //     True if available, otherwise false.
        private static bool SupportOsNormalization()
        {
            if (_supportOsNormalization.HasValue)
            {
                return _supportOsNormalization.Value;
            }

            if ("あおぞら".Equals("あおそ\u3099ら", StringComparison.Ordinal))
            {
                _supportOsNormalization = false;
            }
            else
            {
                try
                {
                    _supportOsNormalization = "あおぞら".Normalize(NormalizationForm.FormKD).Equals("あおそ\u3099ら", StringComparison.Ordinal);
                }
                catch
                {
                    _supportOsNormalization = false;
                }
            }

            return _supportOsNormalization.Value;
        }

        //
        // Summary:
        //     Concatenate an array of bytes.
        //
        // Parameters:
        //   source1:
        //     The first array.
        //
        //   source2:
        //     The second array.
        //
        // Returns:
        //     The concatenated array of bytes.
        private static byte[] Concat(byte[] source1, byte[] source2)
        {
            byte[] array = new byte[source1.Length + source2.Length];
            Buffer.BlockCopy(source1, 0, array, 0, source1.Length);
            Buffer.BlockCopy(source2, 0, array, source1.Length, source2.Length);
            return array;
        }

        //
        // Summary:
        //     Gets the mnemonic string.
        //
        // Returns:
        //     The string.
        public override string ToString()
        {
            return _mnemonic;
        }
    }
}
