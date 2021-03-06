﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AemulusModManager
{
    public class sprUtils
    {
        private int Search(byte[] src, byte[] pattern)
        {
            int c = src.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < c; i++)
            {
                if (src[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && src[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }

        private byte[] SliceArray(byte[] source, int start, int end)
        {
            int length = end - start;
            byte[] dest = new byte[length];
            Array.Copy(source, start, dest, 0, length);
            return dest;
        }

        private string getTmxName(byte[] tmx)
        {
            int end = Search(tmx, new byte[] { 0x00 });
            byte[] name = tmx.Take(end).ToArray();
            // hardcode for ◆noiz.tmx
            if (BitConverter.ToString(new byte[] { name[0] }).Replace("-", "") == "81")
                return $"◆{Encoding.ASCII.GetString(SliceArray(name, 2, name.Length))}";
            return Encoding.ASCII.GetString(name);
        }

        public Dictionary<string, int> getTmxNames(string spr)
        {
            Dictionary<string, int> tmxNames = new Dictionary<string, int>();
            byte[] sprBytes = File.ReadAllBytes(spr);
            byte[] pattern = Encoding.ASCII.GetBytes("TMX0");
            int offset = 0;
            int found = 0;
            while (found != -1)
            {
                // Start search after "TMX0"
                found = Search(SliceArray(sprBytes, offset, sprBytes.Length), pattern);
                offset =  found + offset + 4;
                if (found != -1)
                {
                    string tmxName = getTmxName(SliceArray(sprBytes,(offset + 24),sprBytes.Length));
                    tmxNames.Add(tmxName, offset - 12);
                }
            }
            return tmxNames;
        }

        private List<int> getTmxOffsets(string spr)
        {
            List<int> tmxOffsets = new List<int>();
            byte[] sprBytes = File.ReadAllBytes(spr);
            byte[] pattern = Encoding.ASCII.GetBytes("TMX0");
            int offset = 0;
            int found = 0;
            while (found != -1)
            {
                // Start search after "TMX0"
                found = Search(SliceArray(sprBytes, offset, sprBytes.Length), pattern);
                offset = found + offset + 4;
                if (found != -1)
                {
                    tmxOffsets.Add(offset - 12);
                }
            }
            return tmxOffsets;
        }

        private int findTmx(string spr, string tmxName)
        {
            // Get all tmx names instead to prevent replacing similar names
            if (File.Exists(spr))
            {
                Dictionary<string, int> tmxNames = getTmxNames(spr);
                if (tmxNames.ContainsKey(tmxName))
                    return tmxNames[tmxName];
            }
            return -1;
        }

        public void replaceTmx(string spr, string tmx)
        {
            string tmxPattern = Path.GetFileNameWithoutExtension(tmx);
            int offset = findTmx(spr, tmxPattern);
            if (offset > -1)
            {
                byte[] tmxBytes = File.ReadAllBytes(tmx);
                int repTmxLen = tmxBytes.Length;
                int ogTmxLen = BitConverter.ToInt32(File.ReadAllBytes(spr), (offset + 4));

                if (repTmxLen == ogTmxLen)
                {
                    using (Stream stream = File.Open(spr, FileMode.Open))
                    {
                        stream.Position = offset;
                        stream.Write(tmxBytes, 0, repTmxLen);
                    }
                }
                else // Insert and update offsets
                {
                    byte[] sprBytes = File.ReadAllBytes(spr);
                    byte[] newSpr = new byte[sprBytes.Length + (repTmxLen - ogTmxLen)];
                    SliceArray(sprBytes, 0, offset).CopyTo(newSpr, 0);
                    SliceArray(sprBytes, offset + ogTmxLen, sprBytes.Length).CopyTo(newSpr, offset + repTmxLen);
                    tmxBytes.CopyTo(newSpr, offset);
                    File.WriteAllBytes(spr, newSpr);
                    updateOffsets(spr, getTmxOffsets(spr));
                }
            }
        }

        private void updateOffsets(string spr, List<int> offsets)
        {
            // Start of tmx offsets
            int pos = 36;
            using (Stream stream = File.Open(spr, FileMode.Open))
            {
                foreach (int offset in offsets)
                {
                    byte[] offsetBytes = BitConverter.GetBytes(offset);
                    stream.Position = pos;
                    stream.Write(offsetBytes, 0, 4);
                    pos += 8;
                }
            }
        }

        public byte[] extractTmx(string spr, string tmx)
        {
            string tmxPattern = Path.GetFileNameWithoutExtension(tmx);
            int offset = findTmx(spr, tmxPattern);
            if (offset > -1)
            {
                byte[] sprBytes = File.ReadAllBytes(spr);
                int tmxLen = BitConverter.ToInt32(sprBytes, (offset + 4));
                return SliceArray(sprBytes, offset, offset + tmxLen);
            }
            return null;
        }
    }
}
