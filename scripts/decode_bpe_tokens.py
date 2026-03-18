"""
Decode BPE (Byte Pair Encoding) tokens back to text using tiktoken.

CTF challenge: decode the reactor-log flag encoded as BPE token IDs.
Tokens to decode: 942, 940, 960, 970, 949
"""

import tiktoken

TOKENS = [942, 940, 960, 970, 949]

ENCODINGS = [
    "cl100k_base",  # GPT-4
    "p50k_base",    # GPT-3
    "r50k_base",    # Legacy models (GPT-2 / GPT-3)
]


def decode_tokens(encoding_name: str, tokens: list) -> str:
    enc = tiktoken.get_encoding(encoding_name)
    return enc.decode(tokens)


def main():
    print(f"Decoding BPE tokens: {TOKENS}\n")
    for enc_name in ENCODINGS:
        try:
            decoded = decode_tokens(enc_name, TOKENS)
            print(f"  [{enc_name}] => {decoded!r}")
        except Exception as exc:
            print(f"  [{enc_name}] => ERROR: {exc}")

    # Primary result using r50k_base (GPT-2 / legacy GPT-3 encoding)
    result = decode_tokens("r50k_base", TOKENS)
    print(f"\nDecoded word (r50k_base): {result}")


if __name__ == "__main__":
    main()
