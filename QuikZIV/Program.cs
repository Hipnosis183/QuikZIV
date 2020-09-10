using System;
using System.Collections.Generic;
using System.IO;

namespace QuikZIV
{
    class Program
    {
        static bool AltLZW = true;
        static bool Retry = false;

        static byte[] Output;
        static int OutputSize;

        static FileStream StreamRead;

        const string Usage = "Usage: drag and drop or: quikziv.exe 'input.ziv|sqz' ...";

        static void Main(string[] args)
        {
            string FileName;

            if (args.Length != 0)
            {
                foreach (string arg in args)
                {
                    FileName = arg;

                    if (File.Exists(FileName))
                    {
                        StreamRead = new FileStream(FileName, FileMode.Open, FileAccess.Read);

                        Uncompress();

                        StreamRead.Close();
                    }

                    else
                    {
                        Console.WriteLine("\nFile doesn't exist. " + Usage);
                        Console.ReadKey();
                    }
                }
            }

            else
            {
                Console.WriteLine("\nFile not specified. " + Usage);
                Console.ReadKey();
            }
        }

        static void Uncompress()
        {
            StreamRead.Position = 0;

            // Buffer first 4 bytes.
            byte[] BufferHead = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                BufferHead[i] = (byte)StreamRead.ReadByte();
            }

            // Calculate uncompressed file size.
            OutputSize = (BufferHead[0] << 16) + ((BufferHead[3] << 8) + BufferHead[2]);

            // Check compression method.
            if (BufferHead[1] == 0x10)
            {
                Output = DecodeLZW();
                WriteLZW();
            }

            // Other values might be possible, but for safety just 0x00 or 0x01.

            else if (BufferHead[1] == 0x00) // Huffman+RLE
            {
                Output = DecodeHRLE();
                WriteHRLE();
            }

            else
            {
                Console.WriteLine("\nError: invalid or unsupported file. " + Usage);
                Console.ReadKey();
            }
        }

        static void WriteLZW()
        {
            // Write data stream to file.
            if (OutputSize == Output.Length)
            {
                string FileName = Path.GetFileNameWithoutExtension(StreamRead.Name);

                // Add extension if a DOS MZ file header is detected.
                FileName += Output[0] == 0x4D && Output[1] == 0x5A ? ".EXE" : ".OUT";

                FileStream StreamWrite = new FileStream(FileName, FileMode.Create, FileAccess.Write);
                StreamWrite.Write(Output, 0, Output.Length);
                StreamWrite.Close();

                Console.WriteLine("\nSuccess: output file '" + FileName + "'");
                Console.ReadKey();
            }

            else
            {
                AltLZW = false;

                // Retry without the AltLZW flag.
                if (Retry == false)
                {
                    Retry = true;
                    Uncompress();
                }

                else // Something went wrong.
                {
                    Console.WriteLine("\nError: invalid or unsupported file. " + Usage);
                    Console.ReadKey();
                }
            }
        }

        static void WriteHRLE()
        {
            // Write data stream to file.
            if (OutputSize == Output.Length)
            {
                string FileName = Path.GetFileNameWithoutExtension(StreamRead.Name);

                // Add extension if a DOS MZ file header is detected.
                FileName += Output[0] == 0x4D && Output[1] == 0x5A ? ".EXE" : ".OUT";

                FileStream StreamWrite = new FileStream(FileName, FileMode.Create, FileAccess.Write);
                StreamWrite.Write(Output, 0, Output.Length);
                StreamWrite.Close();

                Console.WriteLine("\nSuccess: output file '" + FileName + "'");
                Console.ReadKey();
            }

            else // Something went wrong.
            {
                Console.WriteLine("\nError: invalid or unsupported file. " + Usage);
                Console.ReadKey();
            }
        }

        static byte[] DecodeLZW()
        {
            // Constants
            int CLEAR_CODE = AltLZW ? 0x101 : 0x100;
            int END_CODE = AltLZW ? 0x100 : 0x101;

            int FIRST = 0x102;
            int MAX_TABLE = 0x1000;

            // Output stream.
            List<byte> Output = new List<byte>();

            // Dictionary [Pointer, Postfix, Prefix]
            Dictionary<int, int[]> Dictionary = new Dictionary<int, int[]>();
            int DictionarySize = FIRST;

            // Variables to extract next codeword.
            int nBit = 0; // Current word size.
            int Position = 0;
            int Previous = CLEAR_CODE; // Previous codeword.
            int Buffer = ((StreamRead.ReadByte() << 16) + (StreamRead.ReadByte() << 8) + StreamRead.ReadByte());

            while (Previous != END_CODE)
            {
                if (Previous == CLEAR_CODE)
                {
                    nBit = 9;
                    DictionarySize = FIRST;
                }

                // Get the next codeword.
                Position += nBit;
                int Codeword = (Buffer >> (24 - Position)) & Convert.ToInt32(Math.Pow(2, nBit) - 1);
                Buffer = (Buffer << 8) + NextByte();

                if (Position >= 16)
                {
                    Buffer = (Buffer << 8) + NextByte();
                }

                Buffer &= 0xFFFFFF;
                Position &= 7;

                // Process the current codeword.
                if ((Codeword != CLEAR_CODE) && (Codeword != END_CODE))
                {
                    int NewByte;

                    if (Codeword < DictionarySize)
                    {
                        NewByte = Codeword < FIRST ? Codeword : Dictionary[Codeword - FIRST][2];
                    }

                    else
                    {
                        NewByte = Previous < FIRST ? Previous : Dictionary[Previous - FIRST][2];
                    }

                    if ((Previous != CLEAR_CODE) && (DictionarySize < MAX_TABLE))
                    {
                        if (Dictionary.ContainsKey(DictionarySize - FIRST))
                        {
                            Dictionary[DictionarySize - FIRST] = new int[] { Previous, NewByte, Previous < FIRST ? Previous : Dictionary[Previous - FIRST][2] };
                        }

                        else
                        {
                            Dictionary.Add(DictionarySize - FIRST, new int[] { Previous, NewByte, Previous < FIRST ? Previous : Dictionary[Previous - FIRST][2] });
                        }

                        DictionarySize++;

                        if ((DictionarySize == Convert.ToInt32(Math.Pow(2, nBit))) && (nBit < 12))
                        {
                            nBit++;
                        }
                    }

                    // Format output.
                    int OutputCodeword = Codeword;
                    string OutputTemp = "";

                    while (OutputCodeword >= FIRST)
                    {
                        int Prefix = Dictionary[OutputCodeword - FIRST][0];
                        int Postfix = Dictionary[OutputCodeword - FIRST][1];

                        OutputCodeword = Prefix;
                        OutputTemp += Convert.ToChar(Postfix);
                    }

                    OutputTemp += Convert.ToChar(OutputCodeword);

                    // Convert output from char to byte.
                    char[] OutputRev = OutputTemp.ToCharArray();
                    Array.Reverse(OutputRev);

                    foreach (char Chara in OutputRev)
                    {
                        byte[] OutputOk = BitConverter.GetBytes(Chara);
                        Output.Add(OutputOk[0]);
                    }
                }

                Previous = Codeword;
            }

            return Output.ToArray();
        }

        static int NextByte()
        {
            return (StreamRead.Position == StreamRead.Length) ? 0 : StreamRead.ReadByte();
        }

        static byte[] DecodeHRLE()
        {
            // Set Huffman tree size.
            int TreeSize = StreamRead.ReadByte() + (StreamRead.ReadByte() << 8);

            // Output stream.
            List<byte> Output = new List<byte>();

            // Create Huffman byte tree.
            byte[] TreeByte = new byte[TreeSize];

            // Load data stream to byte tree.
            for (int i = 0; i < TreeSize; i++)
            {
                TreeByte[i] = (byte)StreamRead.ReadByte();
            }

            // Create Huffman unsigned short tree.
            ushort[] TreeShort = new ushort[TreeSize / 2];

            // Load data stream to unsigned short tree.
            for (int i = 0; i < TreeSize / 2; i++)
            {
                TreeShort[i] = (ushort)((TreeByte[i * 2 + 1] << 8) | TreeByte[i * 2]);
            }

            // Huffman variables.
            ushort Node = 0;

            // RLE variables.
            int Count = 0;
            int Last = 0;
            int State = 0;

            while (StreamRead.Position != StreamRead.Length)
            {
                int Word = (byte)StreamRead.ReadByte();

                for (int Bit = 128; Bit >= 1; Bit >>= 1)
                {
                    if ((Word & Bit) != 0)
                    {
                        Node++;
                    }

                    if ((TreeShort[Node] & 0x8000) != 0)
                    {
                        int Codeword = TreeShort[Node] & 0x7FFF;
                        int L = Codeword & 255;
                        int H = Codeword >> 8;

                        switch (State)
                        {
                            case 0:

                                if (H == 0)
                                {
                                    Last = L;
                                    Output.Add((byte)Last);
                                }

                                else if (L == 0)
                                {
                                    State = 1;
                                }

                                else if (L == 1)
                                {
                                    State = 2;
                                }

                                else
                                {
                                    for (int i = 0; i < L; i++)
                                    {
                                        Output.Add((byte)Last);
                                    }
                                }

                                break;

                            case 1:

                                for (int i = 0; i < Codeword; i++)
                                {
                                    Output.Add((byte)Last);
                                }

                                State = 0;
                                break;

                            case 2:

                                Count = L * 256;
                                State = 3;
                                break;

                            case 3:

                                Count += L;

                                for (int i = 0; i < Count; i++)
                                {
                                    Output.Add((byte)Last);
                                }

                                State = 0;
                                break;
                        }

                        Node = 0;
                    }

                    else
                    {
                        Node = (ushort)(TreeShort[Node] / 2);
                    }
                }
            }

            return Output.ToArray();
        }
    }
}
