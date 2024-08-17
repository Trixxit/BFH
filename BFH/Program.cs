#define Logging

using System.Runtime.CompilerServices;
using Microsoft.Win32.SafeHandles;
using System.Text;
using System.Diagnostics;

using static ANSI.Stylings;
using static ANSI.Cursor;
using static Logging.Logger;
using Metadata = FileReading.Metadata;
using System;
using ANSI;

/*
    General File Layout:
        Metadata, then entries
        Metadata:
	    Metadata Size: How long the metadata is to skip it for efficiency.
            Flags: A byte (#### ####) where each bit is a flag.
                Bit 1: If the file has the custom Encryption.
                Bit 2: If the file uses a schema or not.
                Bit 3: If the file uses a set size or not.
                Bit 4 - 8: Unused.
            Schema part:
                If the second bit is off, then this won't exist for the file.
		This is the part that defines the names and data types of entries in the file.
		Schema part breakdown by item:
			Item 1: The size of the written schema (for skipping) (uint).
			Item 2: How many fields in the schema (uint)
			Item x-y: The fields.
				Fields are broken down into string-byte pairs
				String: The name of the field
				Byte: The datatype of the field
         SetSize:
                A uint that tells how long each entry "should" be, used for speeding things up.
         Entry Amount:
              How many entries are in the file
         Entry Size:
              a list of uints that tell how long each entry is. Won't exist if set size is on

       Entries:
           Organised in either byte - object pairs or just a line of objects, depending on whether or not a schema is used.
 */

string uin = "";
Log("Started!");
while (!string.Equals(uin, "exit", StringComparison.InvariantCultureIgnoreCase))
{
    Console.Write("Enter a command: ");
    uin = (Console.ReadLine() ?? "").ToLower();
    Log($"User input: {uin}");
    Commands.HandleCommand(uin);
    Console.WriteLine();
}

Console.WriteLine(Red + Bold + Italic + "Exiting..." + Reset);

static class Commands
{
    static readonly Dictionary<string, (string, Action)> cmds = new()
    {
        { "create", ("Creates a new binary file", Create) },
        { "clear", ("Clears the console", Clear) },
        { "write entry", ("Writes an entry to a currently open file (in write mode)", Write)}
    };

    public static void HandleCommand(string input)
    {
        if (input == "exit")
            return;
        if (cmds.TryGetValue(input, out (string, Action) a))
            a.Item2();
        else
            Console.WriteLine(Red + Italic + "Command not found" + Reset);
    }

    static void Create()
    {
        Log("Creating file...");
        bool relative = H.Confirm("Use Relative Path? (Y/N): " + Reset, H.yn, "y", "n", out _);
        Log($"Relative path? {relative}");
        Console.Write("Input file name: ");
        string fp = (Console.ReadLine() ?? "").ToLower();
        fp = relative ? Path.Combine(H.exep, fp) : fp;
        fp = fp.EndsWith(".dat") ? fp : fp + ".dat";
        Log($"Filepath {fp}");
        if (File.Exists(fp))
        {
            Console.WriteLine(
                $"{GetFGColor(226, 80, 152)}File {White}{Italic}{fp}{Reset}{GetFGColor(226, 80, 152)} already exists.{Reset}"
                );
            bool overwrite = H.Confirm("Overwrite existing file? (Y/N): ", H.yn, "y", "n", out _);
            Log($"Overwriting? {overwrite}");
            if (!overwrite)
            {
                Console.WriteLine(Red + Italic + "Creation stopped." + Reset);
                return; 
            }
        }
        FileWriting.AddMetaData(File.Create(fp));
        Log($"File created.");
        Console.WriteLine(Green + Italic + "Created!" + Reset);
        bool open = H.Confirm("Would you like to open it for editing? (Y/N): ", H.yn, "y", "n", out _);
        Log($"Keeping Open? {open}");
        if (open)
        {
            Streams.fileOpen = true;
            Streams.ConstructStream(fp);
            Console.WriteLine(Green + Italic + $"File activated" + Reset);
        }
    }


    static void Clear()
        => Console.Clear();

    static void Write()
    {
        Log($"Writing to file...");
        if (Streams.CurrentMetadata.ifSchema)
            WriteSchema();
        else
            WriteNoSchema();
    }

    static void WriteSchema()
    {

    }

    static void WriteNoSchema()
    {
        List<TypeValueUnion> items = [];
        Console.WriteLine($"{Italic}How many entries?{Reset}");
        bool conf_amo = H.GetNumber<int>(out int amount, out int lns);
        if (!conf_amo)
        {
            Clearlines(lns + 1);
            Console.WriteLine($"{Red}{Italic}Failed to get amount of entries, cancelling.{Reset}");
            return;
        }
        Log($"Amount: {amount}");
        bool obo = H.Confirm("Insert One by One? (Y/N): ", H.yn, "y", "n", out int ln1);
        int lines = 2 + lns + ln1;
        if (obo)
        {
            string choice = "";
            Console.WriteLine($"Enter {Italic}{Inverse}cancel{Reset} to cancel, enter {Italic}{Inverse}undo{Reset} to step back to the previous entry.");
            for (int j = 0; j < amount; j++) { 
                choice = H.Confirm($"Insert Datatype #{amount}: ", "Please choose a datatype from the below: Boolean, Byte, Bytes, Char, Chars, Double, Half, Int16, Int32, Int64, SByte, Single, String, UInt16, UInt32, UInt64", ["Boolean", "Byte", "Bytes", "Char", "Chars", "Double", "Half", "Int16", "Int32", "Int64", "SByte", "Single", "String", "UInt16", "UInt32", "UInt64", "cancel"], out int l);
                lines += l;
                if (string.Equals(choice, "cancel", StringComparison.InvariantCultureIgnoreCase))
                    return;
                if (string.Equals(choice, "undo", StringComparison.InvariantCultureIgnoreCase))
                {
                    j -= 2;
                    continue;
                }
                bool b = H.Str2Enu(choice, out object? dt);
                if (!b || dt is not Datatypes)
                {
                    Console.WriteLine($"{Red}{Italic}Failed to parse {White}{Bold}{choice}{Reset}{Red}{Italic}, this may be a logic error. Try again though.");
                    lines++;
                    j -= 1;
                    continue;
                }
                switch (dt)
                {
                    case Datatypes.Boolean:
                        bool a1 = H.GetBool(out bool c, out int l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Boolean, c));
                        break;
                    case Datatypes.Byte:
                        a1 = H.GetNumber<byte>(out byte b1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Byte, b1));
                        break;
                    case Datatypes.Bytes:
                        a1 = H.GetBytes(out byte[] byteArr, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Bytes, byteArr));
                        break;
                    case Datatypes.Char:
                        a1 = H.GetChar(out char ch, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Char, ch));
                        break;
                    case Datatypes.Chars:
                        a1 = H.GetChars(out char[] charArr, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Chars, charArr));
                        break;
                    case Datatypes.Double:
                        a1 = H.GetNumber<double>(out double d1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Double, d1));
                        break;
                    case Datatypes.Half:
                        a1 = H.GetNumber<Half>(out Half h1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Half, h1));
                        break;
                    case Datatypes.Int16:
                        a1 = H.GetNumber<short>(out short s1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Int16, s1));
                        break;
                    case Datatypes.Int32:
                        a1 = H.GetNumber<int>(out int i1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Int32, i1));
                        break;
                    case Datatypes.Int64:
                        a1 = H.GetNumber<long>(out long l2, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Int64, l2));
                        break;
                    case Datatypes.SByte:
                        a1 = H.GetNumber<sbyte>(out sbyte sb1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.SByte, sb1));
                        break;
                    case Datatypes.Single:
                        a1 = H.GetNumber<float>(out float f1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.Single, f1));
                        break;
                    case Datatypes.String:
                        Console.Write("Insert String: ");
                        string s = Console.ReadLine() ?? "";
                        lines += 1;
                        items.Add(new(Datatypes.String, s));
                        break;
                    case Datatypes.UInt16:
                        a1 = H.GetNumber<ushort>(out ushort us1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.UInt16, us1));
                        break;
                    case Datatypes.UInt32:
                        a1 = H.GetNumber<uint>(out uint ui1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.UInt32, ui1));
                        break;
                    case Datatypes.UInt64:
                        a1 = H.GetNumber<ulong>(out ulong ul1, out l1);
                        lines += l1;
                        if (!a1)
                        {
                            Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Item cancelled, continuing...{Reset}");
                            lines++;
                            continue;
                        }
                        items.Add(new(Datatypes.UInt64, ul1));
                        break;
                    default:
                        Console.WriteLine($"{GetFGColor(226, 80, 152)}{Italic}Unknown datatype, please try again...{Reset}");
                        j--;
                        break;
                }


            } while (!string.Equals(choice, "cancel", StringComparison.InvariantCultureIgnoreCase));
            Console.WriteLine("Please confirm the below data: ");
            for (int i = 0; i < items.Count; i++)
                Console.WriteLine($"Type: {items[i].Type}      Value: {items[i].Value}");
            bool z = H.Confirm("Is this Correct? (Y/N)", H.yn, "y", "n", out int ln);
            lines += ln;
            if (!z)
            {
                Cursor.Clearlines(lines);
                Console.WriteLine($"{Red}{Italic}Entry write cancelled.{Reset}");
                return;
            }
        }
        else
        {

        }

        Cursor.Clearlines(lines);
        Console.WriteLine($"Adding entry!");
        FileWriting.ClearMemoryStream();
        FileWriting.AddEntry(items.ToArray());
        Console.WriteLine("Entry added!");
    }
}

enum Datatypes : byte
{
    Boolean,
    Byte,
    Bytes,
    Char,
    Chars,
    Double,
    Half,
    Int16,
    Int32,
    Int64,
    SByte,
    Single,
    String,
    UInt16,
    UInt32,
    UInt64,
    SevenBitEncodedInt,
    SevenBitEncodedInt64
}

static class FileReading
{
    public readonly record struct Metadata(bool Encrypted, bool ifSchema, uint setsize, Schema schema, uint entry_amount, uint[] entry_lengths, long length)
    {
        public uint Size { get 
            {
                MemoryStream ms = new();
                Writer w = new(ms);
                byte b = 0b0;
                b.SFI(Encrypted, 0b1);
                b.SFI(ifSchema, 0b10);
                b.SFI(setsize > 0, 0b100);
                w.Write(b);
                if (ifSchema)
                {
                    w.Write(schema.bitsize)
                }
            } 
        }
    }

    public record struct Schema(ulong bitsize, uint fieldsize, SchemaField[] fields);

    public readonly record struct SchemaField(string name, Datatypes type);

    public static Metadata ReadMetaData()
    {
        var r = Streams.reader;
        Streams.stream.SeekStart();
        uint mdlen = r.ReadUInt();
        byte flags = r.ReadByte();
        bool hasSchema = flags.HF(0b10), hasSize = flags.HF(0b100);
        Schema schema = new();
        uint setSize = 0;
        if (hasSchema)
        {
            ulong size = r.ReadULong();
            uint fieldsize = r.ReadUInt();;
            SchemaField[] fields = new SchemaField[fieldsize];
            Log($"Metadata: {size}, {fieldsize}");
            for (int i = 0; i < fieldsize; i++)
            {
                fields[i] = new SchemaField(r.ReadString(), (Datatypes)r.ReadByte());
                Log(fields[i].ToString());
            }
            schema = new(size, fieldsize, fields);
        }
        if (hasSize)
            setSize = r.ReadUInt();;
        uint entry_amount = r.ReadUInt();;
        uint[] entries = new uint[entry_amount];
        for (int i = 0; i < entry_amount; i++)
            entries[i] = r.ReadUInt();;
        var md = new Metadata(flags.HF(0b1), hasSchema, setSize, schema, entry_amount, entries, Streams.stream.Position);
        return md;
    }

    public static long ReadEntryLenghts()
    {
        return 0;
    }

    public static long GetEntryLength(int entryNumber)
    {
        return 0;
    }

    public static long[] ReadLengths()
    {
        return [0];
    }

    public static object[][] ReadEntries()
    {
        return [];
    }

    public static object[] ReadEntry(long entry, Datatypes[] types)
    {
        return [0];
    }

    public static bool CheckMatch(long entry, Datatypes[] types)
    {
        return true;
    }

    public static string GetBinary()
    {
        return "";
    }

    public static string GetHex()
    {
        return "";
    }

    public static string GetRaw()
    {
        return "";
    }
}

static class FileWriting
{
    private static MemoryStream ms;
    private static bool CanWrite { get => Streams.fileOpen && Streams.stream != null && Streams.stream.CanWrite && ms != null; }

    public static void AddMetaData(FileStream fileStream)
        => fileStream.Write([0b0000,
            0b0000, 0b0000, 0b0000, 0b0000]);

    public static void RemoveLength(long length)
    {
        Stream fileStream = Streams.stream;
        long remainingBytes = fileStream.Length - fileStream.Position - length;
        if (remainingBytes > 0)
        {
            byte[] buffer = new byte[remainingBytes];
            fileStream.Seek(length, SeekOrigin.Current);
            fileStream.Read(buffer, 0, buffer.Length);
            fileStream.Seek(-remainingBytes, SeekOrigin.Current);
            fileStream.Write(buffer, 0, buffer.Length);
        }
        fileStream.SetLength(fileStream.Length - length);
    }

    public static void UpdateMetadata(long old_length, Metadata md)
    {
        Streams.stream.SeekStart();
        RemoveLength(old_length);
        Streams.writer.Write(md.length);
        byte flags = 0x0;
        if (md.ifSchema)
            flags.SF(0b10);
        if (md.setsize > 0)
            flags.SF(0b100);
        if (md.Encrypted)
            flags.SF(0b1);
        Streams.writer.Write(flags);
        if (md.ifSchema && md.schema.bitsize > 0 && md.schema.fieldsize > 0)
        {
            Streams.writer.Write(md.schema.bitsize);
            Streams.writer.Write(md.schema.fieldsize);
            for (int i = 0; i < md.schema.fieldsize; i++)
            {
                Streams.writer.Write(md.schema.fields[i].name);
                Streams.writer.Write((byte)md.schema.fields[i].type);
            }
        }
        if (md.setsize > 0)
            Streams.writer.Write(md.setsize);
        Streams.writer.Write(md.entry_amount);
        for (int i = 0; i < md.entry_amount; i++)
            Streams.writer.Write(md.entry_lengths[i]);
        Streams.CurrentMetadata = md;
    }

    public static void InitializeEntry()
    {
        if (Streams.fileOpen && Streams.stream != null && Streams.stream.CanWrite == true)
            ms = new MemoryStream();
        else
            throw new InvalidOperationException("No File is open, the stream is null or the stream is read only!");
        Log("Entry initialised!");
    }

    public static void FlushEntry()
    {
        new InvalidOperationException().ThrowIfFalse(CanWrite);
        Streams.stream.SeekEnd();
        Streams.writer.Write(ms.Length);
        Streams.writer.Write(ms.ToArray());
        Log($"Entry of length {ms.Length} flushed!");
        ClearMemoryStream();
    }

    public static void AddEntry(TypeValueUnion[] tvus)
    {
        for (int i = 0; i < tvus.Length; i++)
            AddField(tvus[i]);
        FileWriting.FlushEntry();

    }

    public static void ClearMemoryStream()
        => ms = new();

    public static void AddField(TypeValueUnion tvu)
    { 
        new InvalidOperationException().ThrowIfFalse(CanWrite);
        new Writer(ms).Write(tvu);
        Log($"TVU {tvu.Type}, {tvu.Value} flushed!");
    }

    public static void RemoveEntry(long id)
    {
        new InvalidOperationException().ThrowIfFalse(CanWrite);
    }

    public static void ClearEntries()
    {
        new InvalidOperationException().ThrowIfFalse(CanWrite);
    }

    public static void UpdateEntry()
    {
        new InvalidOperationException().ThrowIfFalse(CanWrite);
    }
}

readonly record struct TypeValueUnion(Datatypes Type, object Value);

static class Streams
{
    public static bool fileOpen = false;
    public static Stream stream;
    public static Reader reader;
    public static Writer writer;
    public static Metadata CurrentMetadata;

    public static void ConstructStream(string path)
    {
        SafeFileHandle sfh = File.OpenHandle(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        stream = new Stream(sfh, FileAccess.ReadWrite);
        writer = new Writer(stream);
        reader = new Reader(stream);
        CurrentMetadata = FileReading.ReadMetaData();
    }
}

unsafe class Reader(System.IO.Stream fs) : IDisposable
{
    public void Dispose()
    {
        fs.Dispose();
    }

    public bool ReadBoolean()
        => ReadByte() != 0;

    public byte ReadByte()
    {
        int value = fs.ReadByte();
        if (value == -1)
            throw new EndOfStreamException();
        return (byte)value;
    }

    [method:MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ReadBytes(int count)
    {
        byte[] buffer = new byte[count];
        if (fs.Read(buffer, 0, count) != count)
            throw new EndOfStreamException();
        return buffer;
    }

    public char ReadChar()
    {
        byte[] buffer = ReadBytes(2);
        return (char)(buffer[0] | (buffer[1] << 8));
    }

    public char[] ReadChars(int count)
    {
        char[] buffer = new char[count];
        for (int i = 0; i < count; i++)
            buffer[i] = ReadChar();
        return buffer;
    }

    public double ReadDouble()
    {
        byte[] buffer = ReadBytes(8);
        ulong value = ((ulong)buffer[0] |
                       ((ulong)buffer[1] << 8) |
                       ((ulong)buffer[2] << 16) |
                       ((ulong)buffer[3] << 24) |
                       ((ulong)buffer[4] << 32) |
                       ((ulong)buffer[5] << 40) |
                       ((ulong)buffer[6] << 48) |
                       ((ulong)buffer[7] << 56));
        return LongToDouble((long)value);
    }

    private static double LongToDouble(long value)
        => *(double*)&value;

    public short ReadShort()
    {
        byte[] buffer = ReadBytes(2);
        return (short)(buffer[0] | (buffer[1] << 8));
    }

    public int ReadInt()
    {
        byte[] buffer = ReadBytes(4);
        return buffer[0] |
               (buffer[1] << 8) |
               (buffer[2] << 16) |
               (buffer[3] << 24);
    }

    public long ReadLong()
    {
        byte[] buffer = ReadBytes(8);
        return (long)(buffer[0] |
                      ((long)buffer[1] << 8) |
                      ((long)buffer[2] << 16) |
                      ((long)buffer[3] << 24) |
                      ((long)buffer[4] << 32) |
                      ((long)buffer[5] << 40) |
                      ((long)buffer[6] << 48) |
                      ((long)buffer[7] << 56));
    }

    public sbyte ReadSignedByte()
    {
        int value = fs.ReadByte();
        if (value == -1)
            throw new EndOfStreamException();
        return (sbyte)value;
    }

    public float ReadFloat()
    {
        byte[] buffer = ReadBytes(4);
        int value = buffer[0] |
                    (buffer[1] << 8) |
                    (buffer[2] << 16) |
                    (buffer[3] << 24);
        return IntToFloat(value);
    }

    private static float IntToFloat(int value)
    {
        return *(float*)&value;
    }

    public string ReadString()
    {
        int length = Read7BitEncodedInt();
        return Encoding.UTF8.GetString(ReadBytes(length));
    }

    public ushort ReadUInt16()
    {
        byte[] buffer = ReadBytes(2);
        return (ushort)(buffer[0] | (buffer[1] << 8));
    }

    public uint ReadUInt()
    {
        byte[] buffer = ReadBytes(4);
        return (uint)(buffer[0] |
                      (buffer[1] << 8) |
                      (buffer[2] << 16) |
                      (buffer[3] << 24));
    }

    public ulong ReadULong()
    {
        byte[] buffer = ReadBytes(8);
        return (buffer[0] |
                ((ulong)buffer[1] << 8) |
                ((ulong)buffer[2] << 16) |
                ((ulong)buffer[3] << 24) |
                ((ulong)buffer[4] << 32) |
                ((ulong)buffer[5] << 40) |
                ((ulong)buffer[6] << 48) |
                ((ulong)buffer[7] << 56));
    }

    public int Read7BitEncodedInt()
    {
        int value = 0;
        int shift = 0;
        byte byteRead;

        do
        {
            if (shift == 35)
            {
                throw new FormatException("Invalid 7-bit encoded integer.");
            }

            byteRead = ReadByte();
            value |= (byteRead & 0x7F) << shift;
            shift += 7;
        } while ((byteRead & 0x80) != 0);

        return value;
    }


    public static ulong UnsignLong(long l)
        => *(ulong*)&l;

    public static uint UnsignInt(int i)
        => *(uint*)&i;

    public static ushort UnsignLong(short s)
        => *(ushort*)&s;

    public static sbyte SignByte(long b)
        => unchecked(*(sbyte*)&b);
}

unsafe class Writer(System.IO.Stream _fileStream) : IDisposable
{
    private readonly byte[] _buffer = new byte[16];

    public void WriteFlag<T>(T value)
    {
        if (value is bool bo)
        {
            Write((byte)Datatypes.Boolean);
            Write(bo);
        }
        else if (value is byte by)
        {
            Write((byte)Datatypes.Byte);
            Write(by);
        }
        else if (value is byte[] bytes)
        {
            Write((byte)Datatypes.Bytes);
            Write(bytes);
        }
        else if (value is char ch)
        {
            Write((byte)Datatypes.Char);
            Write(ch);
        }
        else if (value is char[] chars)
        {
            Write((byte)Datatypes.Chars);
            Write(chars);
        }
        else if (value is double d)
        {
            Write((byte)Datatypes.Double);
            Write(d);
        }
        else if (value is Half h)
        {
            Write((byte)Datatypes.Half);
            Write(h);
        }
        else if (value is short s)
        {
            Write((byte)Datatypes.Int16);
            Write(s);
        }
        else if (value is int i)
        {
            Write((byte)Datatypes.Int32);
            Write(i);
        }
        else if (value is long l)
        {
            Write((byte)Datatypes.Int64);
            Write(l);
        }
        else if (value is sbyte sb)
        {
            Write((byte)Datatypes.SByte);
            Write(sb);
        }
        else if (value is float f)
        {
            Write((byte)Datatypes.Single);
            Write(f);
        }
        else if (value is string str)
        {
            Write((byte)Datatypes.String);
            Write(str);
        }
        else if (value is ushort us)
        {
            Write((byte)Datatypes.UInt16);
            Write(us);
        }
        else if (value is uint ui)
        {
            Write((byte)Datatypes.UInt32);
            Write(ui);
        }
        else if (value is ulong ul)
        {
            Write((byte)Datatypes.UInt64);
            Write(ul);
        }
        else
            throw new ArgumentException("Unsupported data type.");
    }

    public void Write(TypeValueUnion TVU)
    {
        Datatypes type = TVU.Type;
        if (type == Datatypes.Boolean)
        {
            Write((byte)Datatypes.Boolean);
            Write((bool)TVU.Value);
        }
        else if (type == Datatypes.Byte)
        {
            Write((byte)Datatypes.Byte);
            Write((byte)TVU.Value);
        }
        else if (type == Datatypes.Bytes)
        {
            Write((byte)Datatypes.Bytes);
            Write((byte[])TVU.Value);
        }
        else if (type == Datatypes.Char)
        {
            Write((byte)Datatypes.Char);
            Write((char)TVU.Value);
        }
        else if (type == Datatypes.Chars)
        {
            Write((byte)Datatypes.Chars);
            Write((char[])TVU.Value);
        }
        else if (type == Datatypes.Double)
        {
            Write((byte)Datatypes.Double);
            Write((double)TVU.Value);
        }
        else if (type == Datatypes.Half)
        {
            Write((byte)Datatypes.Half);
            Write((Half)TVU.Value);
        }
        else if (type == Datatypes.Int16)
        {
            Write((byte)Datatypes.Int16);
            Write((short)TVU.Value);
        }
        else if (type == Datatypes.Int32)
        {
            Write((byte)Datatypes.Int32);
            Write((int)TVU.Value);
        }
        else if (type == Datatypes.Int64)
        {
            Write((byte)Datatypes.Int64);
            Write((long)TVU.Value);
        }
        else if (type == Datatypes.SByte)
        {
            Write((byte)Datatypes.SByte);
            Write((sbyte)TVU.Value);
        }
        else if (type == Datatypes.Single)
        {
            Write((byte)Datatypes.Single);
            Write((float)TVU.Value);
        }
        else if (type == Datatypes.String)
        {
            Write((byte)Datatypes.String);
            Write((string)TVU.Value);
        }
        else if (type == Datatypes.UInt16)
        {
            Write((byte)Datatypes.UInt16);
            Write((ushort)TVU.Value);
        }
        else if (type == Datatypes.UInt32)
        {
            Write((byte)Datatypes.UInt32);
            Write((uint)TVU.Value);
        }
        else if (type == Datatypes.UInt64)
        {
            Write((byte)Datatypes.UInt64);
            Write((ulong)TVU.Value);
        }
        else
            throw new ArgumentException("Unsupported data type.");
    }

    public void Write(bool value)
    {
        _buffer[0] = (byte)(value ? 1 : 0);
        _fileStream.Write(_buffer, 0, 1);
    }

    public void Write(byte value)
        => _fileStream.WriteByte(value);

    public void Write(byte[] value)
        => _fileStream.Write(value, 0, value.Length);

    public void Write(char value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _fileStream.Write(_buffer, 0, 2);
    }

    public void Write(char[] value)
    {
        foreach (char c in value)
            Write(c);
    }

    public void Write(double value)
    {
        fixed (byte* b = _buffer)
        {
            byte* p = (byte*)&value;
            for (int i = 0; i < sizeof(double); i++)
                b[i] = p[i];
        }
        _fileStream.Write(_buffer, 0, 8);
    }

    public void Write(Half value)
    {
        fixed (byte* b = _buffer)
        {
            byte* p = (byte*)&value;
            b[0] = p[0];
            b[1] = p[1];
        }
        _fileStream.Write(_buffer, 0, 2);
    }

    public void Write(short value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _fileStream.Write(_buffer, 0, 2);
    }

    public void Write(int value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _buffer[2] = (byte)(value >> 16);
        _buffer[3] = (byte)(value >> 24);
        _fileStream.Write(_buffer, 0, 4);
    }

    public void Write(long value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _buffer[2] = (byte)(value >> 16);
        _buffer[3] = (byte)(value >> 24);
        _buffer[4] = (byte)(value >> 32);
        _buffer[5] = (byte)(value >> 40);
        _buffer[6] = (byte)(value >> 48);
        _buffer[7] = (byte)(value >> 56);
        _fileStream.Write(_buffer, 0, 8);
    }

    public void Write(sbyte value)
    {
        _buffer[0] = (byte)value;
        _fileStream.Write(_buffer, 0, 1);
    }

    public void Write(float value)
    {
        fixed (byte* b = _buffer)
        {
            byte* p = (byte*)&value;
            for (int i = 0; i < sizeof(float); i++)
            {
                b[i] = p[i];
            }
        }
        _fileStream.Write(_buffer, 0, 4);
    }

    public void Write(string value)
    {
        byte[] stringBytes = Encoding.UTF8.GetBytes(value);
        Write7BitEncodedInt(stringBytes.Length);
        _fileStream.Write(stringBytes, 0, stringBytes.Length);
    }

    public void Write(ushort value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _fileStream.Write(_buffer, 0, 2);
    }

    public void Write(uint value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _buffer[2] = (byte)(value >> 16);
        _buffer[3] = (byte)(value >> 24);
        _fileStream.Write(_buffer, 0, 4);
    }

    public void Write(ulong value)
    {
        _buffer[0] = (byte)value;
        _buffer[1] = (byte)(value >> 8);
        _buffer[2] = (byte)(value >> 16);
        _buffer[3] = (byte)(value >> 24);
        _buffer[4] = (byte)(value >> 32);
        _buffer[5] = (byte)(value >> 40);
        _buffer[6] = (byte)(value >> 48);
        _buffer[7] = (byte)(value >> 56);
        _fileStream.Write(_buffer, 0, 8);
    }

    public void Write7BitEncodedInt(int value)
    {
        while (value >= 0x80)
        {
            _fileStream.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        _fileStream.WriteByte((byte)value);
    }

    public void Write7BitEncodedInt64(long value)
    {
        while (value >= 0x80)
        {
            _fileStream.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        _fileStream.WriteByte((byte)value);
    }

    public void Dispose()
       => _fileStream?.Dispose();
}

class Stream : FileStream
{
    public Stream(SafeFileHandle sfh, FileAccess fa) : base(sfh, fa)
        => _ = "Lorum Ipsum";

    public void SeekEnd()
        => Seek(0, SeekOrigin.End);

    public void SeekEnd(long n)
        => Seek(n, SeekOrigin.End);

    public void SeekStart()
        => Seek(0, SeekOrigin.Begin);

    public void SeekStart(long n)
        => Seek(n, SeekOrigin.Begin);
}

namespace ANSI
{
    public static class Stylings
    {
        public const string Reset = "\u001b[0m";
        public const string Bold = "\u001b[1m";
        public const string Italic = "\u001b[3m";
        public const string Underline = "\u001b[4m";
        public const string Blink = "\u001b[5m";
        public const string FastBlink = "\u001b[6m";
        public const string Inverse = "\u001b[7m";
        public const string Hidden = "\u001b[8m";
        public const string Strikethrough = "\u001b[9m";

        public const string Black = "\u001b[30m";
        public const string Red = "\u001b[31m";
        public const string Green = "\u001b[32m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string Cyan = "\u001b[36m";
        public const string White = "\u001b[37m";

        public const string BrightBlack = "\u001b[30;1m";
        public const string BrightRed = "\u001b[31;1m";
        public const string BrightGreen = "\u001b[32;1m";
        public const string BrightYellow = "\u001b[33;1m";
        public const string BrightBlue = "\u001b[34;1m";
        public const string BrightMagenta = "\u001b[35;1m";
        public const string BrightCyan = "\u001b[36;1m";
        public const string BrightWhite = "\u001b[37;1m";

        public const string BgBlack = "\u001b[40m";
        public const string BgRed = "\u001b[41m";
        public const string BgGreen = "\u001b[42m";
        public const string BgYellow = "\u001b[43m";
        public const string BgBlue = "\u001b[44m";
        public const string BgMagenta = "\u001b[45m";
        public const string BgCyan = "\u001b[46m";
        public const string BgWhite = "\u001b[47m";

        public const string BgBrightBlack = "\u001b[40;1m";
        public const string BgBrightRed = "\u001b[41;1m";
        public const string BgBrightGreen = "\u001b[42;1m";
        public const string BgBrightYellow = "\u001b[43;1m";
        public const string BgBrightBlue = "\u001b[44;1m";
        public const string BgBrightMagenta = "\u001b[45;1m";
        public const string BgBrightCyan = "\u001b[46;1m";
        public const string BgBrightWhite = "\u001b[47;1m";

        public const string Framed = "\u001b[51;";
        public const string Encircled = "\u001b[52;";
        public const string Overline = "\u001b[53;";

        public static string GetFGColor(int r, int g, int b)
            => $"\u001b[38;5;{16 + (36 * (r / 51)) + (6 * (g / 51)) + (b / 51)}m";

    }

    public static class Cursor
    {
        public const string MoveUp = "\u001b[{0}A";
        public const string MoveDown = "\u001b[{0}B";
        public const string MoveForward = "\u001b[{0}C";
        public const string MoveBackward = "\u001b[{0}D";
        public const string MoveToNextLine = "\u001b[{0}E";
        public const string MoveToPreviousLine = "\u001b[{0}F";
        public const string MoveToColumn = "\u001b[{0}G";
        public const string MoveToPosition = "\u001b[{0};{1}H";
        public const string ClearScreen = "\u001b[2J";
        public const string ClearLine = "\u001b[2K";
        public const string SavePosition = "\u001b[s";
        public const string RestorePosition = "\u001b[u";
        public const string Return = "\r";

        public static string F(string a, string b)
            => string.Format(a, b);

        public static string MoveTo(string a, string b)
            => string.Format(MoveToPosition, a, b);

        public static void Clearlines(int a)
        {
            Console.Write("\r\u001b[2K");
            for (int i = 1; i < a; i++)
            {
                Console.Write(Return + F(MoveUp, "2"));
                Console.Write(ClearLine);
            } 
        }
    }
}

static class H
{
    public const string yn = Red + Italic + Bold + "Please Input Y or N" + Reset;
    public static string exep => System.Environment.CurrentDirectory;

    public static bool Confirm(string question, string error, string ca, string cb, out int lines)
    {
        string i = null;
        lines = 1;
        bool showError = false;
        do
        {
            if (showError)
            {
                Console.Write(Return + ClearLine);
                Console.Write(Return + F(MoveUp, "2") + ClearLine);
                Console.WriteLine(error);
                Console.Beep(200, 200);
                Thread.Sleep(50);
                Console.Beep(190, 300);
                Console.Write(Return + ClearLine + new string(' ', Console.WindowWidth - 1) + Return);
                lines = 2;
            }
            Console.Write(question);
            i = (Console.ReadLine() ?? "").ToLower();
            showError = i.Contains(ca, StringComparison.InvariantCultureIgnoreCase) || i.Contains(cb, StringComparison.InvariantCultureIgnoreCase);
        } while (showError);
        return i.Contains(ca, StringComparison.InvariantCultureIgnoreCase);
    }

    public static string Confirm(string question, string error, List<string> choices, out int lines)
    {
        lines = 1;
        string i = null;
        bool showError = false;
        do
        {
            if (showError)
            {
                Console.Write(Return + ClearLine);
                Console.Write(Return + F(MoveUp, "2") + ClearLine);
                Console.WriteLine(error);
                lines = 2;
                Console.Beep(200, 200);
                Thread.Sleep(50);
                Console.Beep(190, 300);
                Console.Write(Return + ClearLine + new string(' ', Console.WindowWidth - 1) + Return);
            }
            Console.Write(question);
            i = (Console.ReadLine() ?? "").ToLower();
            showError = choices.Any(k => i.Contains(k, StringComparison.InvariantCultureIgnoreCase));
        } while (showError);
        return choices.Any(k => string.Equals(i, k, StringComparison.InvariantCultureIgnoreCase)) ? choices.First(k => string.Equals(i, k, StringComparison.InvariantCultureIgnoreCase)) : choices.First(k => i.Contains(k, StringComparison.InvariantCultureIgnoreCase));
    }

    public static bool Confirm(string question, string error, List<string> choices, out int lines, out object? result)
    {
        lines = 1;
        result = null;
        string i = null;
        bool showError = false;
        do
        {
            if (showError)
            {
                Console.Write(Return + ClearLine);
                Console.Write(Return + F(MoveUp, "2") + ClearLine);
                Console.WriteLine(error);
                lines = 2;
                Console.Beep(200, 200);
                Thread.Sleep(50);
                Console.Beep(190, 300);
                Console.Write(Return + ClearLine + new string(' ', Console.WindowWidth - 1) + Return);
            }
            Console.Write($"[Enter {Inverse}cancel{Reset} to exit] " + question);
            i = (Console.ReadLine() ?? "").ToLower();
            if (i.Contains("cancel", StringComparison.InvariantCultureIgnoreCase))
                return false;
            showError = choices.Any(k => i.Contains(k, StringComparison.InvariantCultureIgnoreCase));
        } while (showError);
        result = choices.Any(k => string.Equals(i, k, StringComparison.InvariantCultureIgnoreCase)) ? choices.First(k => string.Equals(i, k, StringComparison.InvariantCultureIgnoreCase)) : choices.First(k => i.Contains(k, StringComparison.InvariantCultureIgnoreCase));
        return true;
    }

    public static bool Str2Enu(string v, out object? ev)
        => Enum.TryParse(typeof(Datatypes), v, true, out ev);

    public static bool GetBool(out bool b, out int lines)
    {
        b = false;
        bool o = Confirm($"Enter {Inverse}True{Reset} or {Inverse}False{Reset}", $"{Red}{Italic}Try Again.{Reset}", ["true", "false"], out lines, out object? obj);
        return o && bool.TryParse(obj.ToString(), out b);
    }

    public static bool GetNumber<T>(out T val, out int lines) where T : struct
    {
        val = default(T);
        lines = 1;
        string i = null;
        bool showError = false;
        do
        {
            if (showError)
            {
                Console.Write(Return + ClearLine);
                Console.Write(Return + F(MoveUp, "2") + ClearLine);
                Console.WriteLine($"{Red}{Italic}Try Again.{Reset}");
                lines = 2;
                Console.Beep(200, 200);
                Thread.Sleep(50);
                Console.Beep(190, 300);
                Console.Write(Return + ClearLine + new string(' ', Console.WindowWidth - 1) + Return);
            }
            Console.Write($"[Enter {Inverse}cancel{Reset} to exit] " + $"Enter a {(typeof(T) == typeof(float) || typeof(T) == typeof(double) ? "real" : "whole")} number: ");
            i = (Console.ReadLine() ?? "").ToLower();
            if (i.Contains("cancel", StringComparison.InvariantCultureIgnoreCase))
                return false;

            showError = i.TryParseAnonNumber(typeof(T), out dynamic val2);
            val = val2;
        } while (showError);
        return true;
    }

    public static bool GetBytes(out byte[] bytes, out int lines)
    {
        lines = 1;
        bytes = [];
        Console.WriteLine("How many bytes?");
        bool a = GetNumber(out int amount, out int ln);
        lines += ln;
        if (!a)
            return false;
        bytes = new byte[amount];
        for (int i = 0; i < amount; i++)
        {
            a = GetNumber(out byte b, out ln);
            lines += ln;
            if (!a)
            {
                Console.WriteLine("Failed to get byte");
                bool c = H.Confirm("Cancel operation or redo item? (Cancel/Redo): ", $"{Red}Please enter {White}{Italic}Cancel{Reset}{Red} or {White}{Italic}Redo{Reset}.", "cancel", "redo", out ln);
                lines += ln;
                if (c)
                    return false;
                i--;
            }
            bytes[i] = b;
        }
        return true;
    }

    public static bool GetChar(out char @char, out int lines)
    {
        @char = ' ';
        lines = 1;
        Console.Write("Please enter a character, if multiple are entered then the first will be used: ");
        try
        {
            @char = (char)Console.Read();
            return true;
        }
        catch 
        { 
            return false;
        }
    }

    public static bool GetChars(out char[] chars, out int lines)
    {
        chars = [];
        lines = 1;
        Console.Write("Please enter one or more characters, spaces WILL be included: ");
        try
        {
            chars = (Console.ReadLine() ?? " ").ToCharArray();
            return true;
        }
        catch
        {
            return false;
        }
    }
}

static class Extensions
{
    public static bool ThrowIfFalse<T>(this T ioe, bool flag, string message = "") where T : Exception, new()
    {
        if (!flag)
            throw (T)Activator.CreateInstance(typeof(T), message);
        return true;
    }

    public static bool HF(this byte b, byte flag)
        => (b & flag) == flag;

    public static byte TF(this ref byte b, byte flag)
        => b = (byte)(b ^ flag);

    public static byte SF(this ref byte b, byte flag)
        => b = (byte)(b | flag);

    public static byte RF(this ref byte b, byte flag)
        => b = (byte)(b & ~flag);

    public static byte SFI(this ref byte b, bool condition, byte flag)
        => condition ? b = (byte)(b | flag) : b;

    public static bool TryParseAnonNumber(this string input, Type t, out dynamic val)
    {
        val = null;

        if (t == typeof(byte))
        {
            bool success = input.TryParseNumber(out byte result);
            val = result;
            return success;
        }

        if (t == typeof(sbyte))
        {
            bool success = input.TryParseNumber(out sbyte result);
            val = result;
            return success;
        }

        if (t == typeof(double))
        {
            bool success = input.TryParseNumber(out double result);
            val = result;
            return success;
        }

        if (t == typeof(float))
        {
            bool success = input.TryParseNumber(out float result);
            val = result;
            return success;
        }

        if (t == typeof(short))
        {
            bool success = input.TryParseNumber(out short result);
            val = result;
            return success;
        }

        if (t == typeof(ushort))
        {
            bool success = input.TryParseNumber(out ushort result);
            val = result;
            return success;
        }

        if (t == typeof(int))
        {
            bool success = input.TryParseNumber(out int result);
            val = result;
            return success;
        }

        if (t == typeof(uint))
        {
            bool success = input.TryParseNumber(out uint result);
            val = result;
            return success;
        }

        if (t == typeof(long))
        {
            bool success = input.TryParseNumber(out long result);
            val = result;
            return success;
        }

        if (t == typeof(ulong))
        {
            bool success = input.TryParseNumber(out ulong result);
            val = result;
            return success;
        }

        return false;
    }

    public static bool TryParseNumber(this string input, out byte result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out sbyte result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out double result)
        => double.TryParse(input, out result);

    public static bool TryParseNumber(this string input, out float result)
        => float.TryParse(input, out result);

    public static bool TryParseNumber(this string input, out short result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out ushort result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out int result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out uint result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out long result)
        => TryParse(input, out result);

    public static bool TryParseNumber(this string input, out ulong result)
        => TryParse(input, out result);

    private static bool TryParse<T>(string input, out T result) where T : struct, IConvertible
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();

        try
        {
            int numberBase = 10;
            if (input.StartsWith("0b", StringComparison.OrdinalIgnoreCase))
            {
                numberBase = 2;
                input = input.Substring(2);
            }
            else if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                numberBase = 16;
                input = input.Substring(2);
            }
            else if (input.StartsWith("0o", StringComparison.OrdinalIgnoreCase))
            {
                numberBase = 8;
                input = input.Substring(2);
            }

            result = (T)Convert.ChangeType(Convert.ToUInt64(input, numberBase), typeof(T));
            return true;
        }
        catch
        {
            return false;
        }
    }
}

namespace Logging
{
    public static class Logger
    {
        [Conditional("Logging")]
        public static void Log(string message)
        {
            int width = Console.WindowWidth - (Console.WindowWidth / 5);
            if (message.Length < width)
            {
                Console.WriteLine(Inverse + new string(' ', Console.WindowWidth - message.Length) + message + Reset);
            }
            else
            {
                for (int i = 0; i < message.Length; i += width)
                {
                    string part = message.Substring(i, Math.Min(width, message.Length - i));
                    Console.WriteLine(Inverse + new string(' ', Console.WindowWidth - part.Length) + part + Reset);
                }
            }
        }

    }
}