# NebOsc
Simple .NET OSC library. Look at the Test project for how to use it.

Requires VS2019 and .NET6.


Reference from [OSC Spec](http://opensoundcontrol.org/spec-1_0):
- Message contains an address, a comma followed by one or more data type identifiers, then the data itself follows in binary encoding.
- Data types - minimal:
  - i = int32 = 32-bit big-endian two's complement integer
  - f = float32 = 32-bit big-endian IEEE 754 floating point number
  - s = OSC-string = A sequence of non-null ASCII characters followed by a null, followed by 0-3 additional null characters
    to make the total number of bits a multiple of 32.
  - b = OSC-blob = An int32 size count, followed by that many 8-bit bytes of arbitrary binary data, followed by 0-3 additional
    zero bytes to make the total number of bits a multiple of 32.
- Bundle consists of the OSC-string "#bundle" followed by an OSC Time Tag, followed by zero or more OSC Message or Bundle Elements.
  An OSC Bundle Element consists of its size and its contents. The size is an int32 representing the number of 8-bit bytes in the
  contents, and will always be a multiple of 4. The contents are either an OSC Message or an OSC Bundle.
- Time tags are represented by a 64 bit fixed point number. The first 32 bits specify the number of seconds since midnight
  on January 1, 1900, and the last 32 bits specify fractional parts of a second to a precision of about 200 picoseconds.
  This is the representation used by Internet NTP timestamps.

Notes/modifications:
- Supports Messages and Bundles but not nested Bundles.
- Only the default data types are supported.

Uses:
- [NBagOfTricks](https://github.com/cepthomas/NBagOfTricks/blob/main/README.md)
