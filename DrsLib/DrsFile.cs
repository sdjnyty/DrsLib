using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace YTY.DrsLib
{
  public class DrsFile : Dictionary<DrsTableClass, SortedDictionary<uint, byte[]>>
  {
    private string fileName;
    private byte[] signature;

    public DrsFile() { }

    public DrsFile Load(string fileName)
    {
      var f = new DrsFile();
      f.fileName = fileName;
      try
      {
        using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
          using (var br = new BinaryReader(fs))
          {
            f.signature = br.ReadBytes(56);
            var numTables = br.ReadUInt32();
            var tables = new DrsTable[numTables];
            br.ReadUInt32();
            for (var i = 0; i < numTables; i++)
            {
              tables[i].DrsTableClass = GetDrsTableClass(br.ReadUInt32());
              if (tables[i].DrsTableClass == DrsTableClass.Invalid)
                throw new InvalidOperationException("bad drs table class");
              try
              {
                Add(tables[i].DrsTableClass, new SortedDictionary<uint, byte[]>());
              }
              catch (ArgumentException) { throw new InvalidOperationException("drs table class repeat"); }
              tables[i].Offset = br.ReadUInt32();
              tables[i].NumEntries = br.ReadUInt32();
            }
            for (var i = 0; i < numTables; i++)
            {
              var entries = new DrsEntry[tables[i].NumEntries];
              for (var j = 0; j < tables[i].NumEntries; j++)
              {
                entries[j].Id = br.ReadUInt32();
                try
                {
                  this[tables[i].DrsTableClass].Add(tables[i].Entries[j].Id, null);
                }
                catch (ArgumentException) { throw new InvalidOperationException("entry id repeat"); }
                entries[j].Offset = br.ReadUInt32();
                entries[j].Size = br.ReadUInt32();
              }
              tables[i].Entries = entries;
            }
            for (var i = 0; i < numTables; i++)
            {
              for (var j = 0; j < tables[i].NumEntries; j++)
              {
                this[tables[i].DrsTableClass][tables[i].Entries[j].Id] = br.ReadBytes((int)tables[i].Entries[j].Size);
              }
            }
          }
        }
      }
      catch { throw; }
      return f;
    }

    public void Save()
    {

    }

    private DrsTableClass GetDrsTableClass(uint input)
    {
      switch (input)
      {
        case 0x62696e61: // 'bina'
          return DrsTableClass.Bina;
        case 0x73687020: // 'shp '
          return DrsTableClass.Shp;
        case 0x736c7020: // 'slp '
          return DrsTableClass.Slp;
        case 0x77617620: // 'wav '
          return DrsTableClass.Wav;
      }
      return DrsTableClass.Invalid;
    }

    private struct DrsTable
    {
      public DrsTableClass DrsTableClass;
      public uint Offset;
      public uint NumEntries;
      public DrsEntry[] Entries;
    }

    private struct DrsEntry
    {
      public uint Id;
      public uint Offset;
      public uint Size;
    }
  }

  public enum DrsTableClass
  {
    Invalid,
    Bina,
    Shp,
    Slp,
    Wav
  }
}
