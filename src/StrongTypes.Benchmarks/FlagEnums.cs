using System;

namespace StrongTypes.Benchmarks;

[Flags]
public enum Flags5 : long
{
    None = 0,
    F0 = 1L << 0, F1 = 1L << 1, F2 = 1L << 2, F3 = 1L << 3, F4 = 1L << 4,
}

[Flags]
public enum Flags15 : long
{
    None = 0,
    F0 = 1L << 0,  F1 = 1L << 1,  F2 = 1L << 2,  F3 = 1L << 3,  F4 = 1L << 4,
    F5 = 1L << 5,  F6 = 1L << 6,  F7 = 1L << 7,  F8 = 1L << 8,  F9 = 1L << 9,
    F10 = 1L << 10, F11 = 1L << 11, F12 = 1L << 12, F13 = 1L << 13, F14 = 1L << 14,
}

[Flags]
public enum Flags30 : long
{
    None = 0,
    F0 = 1L << 0,   F1 = 1L << 1,   F2 = 1L << 2,   F3 = 1L << 3,   F4 = 1L << 4,
    F5 = 1L << 5,   F6 = 1L << 6,   F7 = 1L << 7,   F8 = 1L << 8,   F9 = 1L << 9,
    F10 = 1L << 10, F11 = 1L << 11, F12 = 1L << 12, F13 = 1L << 13, F14 = 1L << 14,
    F15 = 1L << 15, F16 = 1L << 16, F17 = 1L << 17, F18 = 1L << 18, F19 = 1L << 19,
    F20 = 1L << 20, F21 = 1L << 21, F22 = 1L << 22, F23 = 1L << 23, F24 = 1L << 24,
    F25 = 1L << 25, F26 = 1L << 26, F27 = 1L << 27, F28 = 1L << 28, F29 = 1L << 29,
}
