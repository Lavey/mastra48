using System;
using System.Collections.Generic;
using System.Text;

namespace Mastra48.Tokenization
{
    /// <summary>
    /// Identifies a BPE encoding scheme.
    /// Mirrors the encoding names supported by tiktoken.
    /// </summary>
    public enum BpeEncoding
    {
        /// <summary>cl100k_base – used by GPT-4 and GPT-3.5-Turbo.</summary>
        Cl100kBase,

        /// <summary>p50k_base – used by GPT-3 text-davinci models.</summary>
        P50kBase,

        /// <summary>r50k_base – used by older GPT-3 models (same vocabulary as GPT-2).</summary>
        R50kBase,
    }

    /// <summary>
    /// Represents a sequence of BPE (Byte Pair Encoding) token IDs.
    ///
    /// BPE is the sub-word tokenisation algorithm used by the GPT family of models.
    /// A tokeniser maps a string into a sequence of integer token IDs; this class
    /// holds such a sequence together with the encoding that produced it.
    ///
    /// Decoding (converting token IDs back to a string) requires the full BPE
    /// vocabulary and is performed by the companion Python helper:
    ///   scripts/decode_bpe_tokens.py
    /// </summary>
    public class BpeTokenSequence
    {
        /// <summary>The integer token IDs, in order.</summary>
        public IReadOnlyList<int> TokenIds { get; }

        /// <summary>The BPE encoding that was used to produce the token IDs.</summary>
        public BpeEncoding Encoding { get; }

        /// <summary>
        /// Initialises a new <see cref="BpeTokenSequence"/>.
        /// </summary>
        /// <param name="tokenIds">The token IDs to wrap.</param>
        /// <param name="encoding">The BPE encoding that produced them.</param>
        /// <exception cref="ArgumentNullException">
        ///   Thrown when <paramref name="tokenIds"/> is <c>null</c>.
        /// </exception>
        public BpeTokenSequence(IReadOnlyList<int> tokenIds, BpeEncoding encoding = BpeEncoding.R50kBase)
        {
            if (tokenIds == null) throw new ArgumentNullException(nameof(tokenIds));
            TokenIds = tokenIds;
            Encoding = encoding;
        }

        /// <summary>Returns the number of tokens in the sequence.</summary>
        public int Count { get { return TokenIds.Count; } }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < TokenIds.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(TokenIds[i]);
            }
            sb.Append(']');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Provides helper methods for working with BPE token sequences in the
    /// context of the Mastra AI framework.
    ///
    /// Full decoding (token IDs → text) is handled offline by the tiktoken
    /// Python library.  Run <c>scripts/decode_bpe_tokens.py</c> to reveal
    /// the text encoded in <see cref="CtfTokenSequence"/>.
    /// </summary>
    public static class BpeTokenDecoder
    {
        /// <summary>
        /// The CTF reactor-log BPE token sequence whose plaintext is the challenge flag.
        /// Encoding: r50k_base (GPT-2 / GPT-3 legacy).
        ///
        /// To decode, run:
        ///   pip install tiktoken
        ///   python scripts/decode_bpe_tokens.py
        /// </summary>
        public static readonly BpeTokenSequence CtfTokenSequence =
            new BpeTokenSequence(
                new int[] { 942, 940, 960, 970, 949 },
                BpeEncoding.R50kBase);

        /// <summary>
        /// Formats a list of token IDs as a comma-separated string, e.g.
        /// <c>"942, 940, 960, 970, 949"</c>.
        /// </summary>
        public static string FormatTokenIds(IReadOnlyList<int> tokenIds)
        {
            if (tokenIds == null) throw new ArgumentNullException(nameof(tokenIds));
            return string.Join(", ", tokenIds);
        }

        /// <summary>
        /// Returns the tiktoken encoding-name string that corresponds to
        /// the given <see cref="BpeEncoding"/> value.
        /// </summary>
        public static string GetEncodingName(BpeEncoding encoding)
        {
            switch (encoding)
            {
                case BpeEncoding.Cl100kBase: return "cl100k_base";
                case BpeEncoding.P50kBase:   return "p50k_base";
                case BpeEncoding.R50kBase:   return "r50k_base";
                default:
                    throw new ArgumentOutOfRangeException(nameof(encoding));
            }
        }
    }
}
