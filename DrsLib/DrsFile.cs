using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace YTY.DrsLib
{
  public sealed class DrsFile : Dictionary<DrsTableClass, SortedDictionary<int, byte[]>>
  {
    private byte[] signature;
    private static readonly Stack<int> stackPos = new Stack<int>();

    private DrsFile() { }

    public static DrsFile Load(string fileName)
    {
      var ret = new DrsFile();
      using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
      {
        using (var br = new BinaryReader(fs, Encoding.ASCII))
        {
          stackPos.Clear();
          ret.signature = br.ReadBytes(56);
          var numTables = br.ReadUInt32();
          br.ReadUInt32();
          for (var i = 0; i < numTables; i++)
          {
            var entries = new SortedDictionary<int, byte[]>();
            ret.Add((DrsTableClass)Array.IndexOf(TableClassSignatures, new string(br.ReadChars(4))), entries);
            var offset = br.ReadInt32();
            var numEntries = br.ReadInt32();
            SavePositionAndSeek(fs, offset);
            for (var j = 0; j < numEntries; j++)
            {
              var id = br.ReadInt32();
              var dataOffset = br.ReadInt32();
              var size = br.ReadInt32();
              SavePositionAndSeek(fs, dataOffset);
              entries.Add(id, br.ReadBytes(size));
              RestorePosition(fs);
            }
            RestorePosition(fs);
          }
        }
      }
      return ret;
    }

    public void Save(string fileName)
    {
      using (var fs = new FileStream(fileName, FileMode.Create))
      {
        using (var sw = new BinaryWriter(fs, Encoding.ASCII))
        {
          int pos;
          var qTable = new Queue<int>(Count);
          var qEntry = new Queue<int>(this.Sum(kv => kv.Value.Count));
          sw.Write(signature);
          sw.Write(Count);
          foreach (var kvTable in this)
          {
            sw.Write(TableClassSignatures[(int)kvTable.Key].ToCharArray());
            qTable.Enqueue((int)fs.Position);
            sw.Write(kvTable.Value.Count);
          }
          foreach (var kvTable in this)
          {
            pos = (int)fs.Position;
            sw.Seek(qTable.Dequeue(), SeekOrigin.Begin);
            sw.Write(pos);
            sw.Seek(pos, SeekOrigin.Begin);
            foreach (var kvEntry in kvTable.Value)
            {
              sw.Write(kvEntry.Key);
              qEntry.Enqueue((int)fs.Position);
              sw.Write(kvEntry.Value.Length);
            }
          }
          pos = (int)fs.Position;
          sw.Seek(60, SeekOrigin.Begin);
          sw.Write(pos);
          sw.Seek(pos, SeekOrigin.Begin);
          foreach (var kvTable in this)
            foreach (var kvEntry in kvTable.Value)
            {
              pos = (int)fs.Position;
              sw.Seek(qEntry.Dequeue(), SeekOrigin.Begin);
              sw.Write(pos);
              sw.Seek(pos, SeekOrigin.Begin);
              sw.Write(kvEntry.Value);
            }
        }
      }
    }

    private static void SavePositionAndSeek(Stream stream,int position)
    {
      stackPos.Push((int) stream.Position);
      stream.Seek(position, SeekOrigin.Begin);
    }

    private static int RestorePosition(Stream stream)
    {
      var ret= stackPos.Pop();
      stream.Seek(ret, SeekOrigin.Begin);
      return ret;
    }

    private static readonly string[] TableClassSignatures = { "anib", " phs", " pls", " vaw" };
  }

  public enum DrsTableClass
  {
    Bina,
    Shp,
    Slp,
    Wav
  }
}
