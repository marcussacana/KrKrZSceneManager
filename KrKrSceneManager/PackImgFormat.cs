using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KrKrSceneManager
{
    //WARNING - THIS IS A UNSTABLE RESOURCE 
    public class PackImgFormat
    {
        private byte[] packget;
        public TlgFile[] Import(byte[] pimg)
        {
            SCENE DataTools = new SCENE();
            if (DataTools.getRange(pimg, 0, 4) != "50534200")
                throw new Exception("Bad File Format");
            packget = pimg;
            byte[] Header1 = Encoding.ASCII.GetBytes("TLG5.0");
            byte[] Header2 = Encoding.ASCII.GetBytes("TLG6.0");
            byte[] Header3 = Encoding.ASCII.GetBytes("TLG0.0");
            int StartPos = DataTools.GetOffset(pimg, 0x20, 4, false);
            int OffsetPos = DataTools.GetOffset(pimg, 0x18, 4, false);
            int[] Offsets = new int[0];
            for (int i = StartPos; i < pimg.Length; i++)
            {
                bool Found = false;
                if (DataTools.EqualsAt(pimg, Header1, i))
                    Found = true;
                else if (DataTools.EqualsAt(pimg, Header2, i))
                    Found = true;
                else if (DataTools.EqualsAt(pimg, Header3, i))
                    Found = true;
                if (Found)
                {
                    int[] tmp = new int[Offsets.Length + 1];
                    Offsets.CopyTo(tmp, 0);
                    tmp[Offsets.Length] = i - StartPos;
                    Offsets = tmp;
                }
            }
            int TotalTlgs = Offsets.Length;
            int minimalsize = 1;
            //while (Offsets[TotalTlgs-1] > DataTools.elevate(0xFF, minimalsize)) //don't working?? why??
            //    minimalsize++;
            for (int i = minimalsize; ; i++)
            {
                byte[] OffTable = DataTools.genOffsetTable(Offsets, Offsets.Length, i);
                for (int pos = OffsetPos; pos < StartPos; pos++)
                    if (DataTools.EqualsAt(pimg, OffTable, pos))
                    {
                        OffsetPos = pos;
                        goto exitloop;
                    }
            }
        exitloop:;
            TlgFile[] Tlgs = new TlgFile[TotalTlgs];
            for (int i = 0; i < TotalTlgs; i++)
            {
                int EndPos = 0;
                if (i == TotalTlgs - 1)
                    EndPos = pimg.Length - StartPos;
                else
                    EndPos = Offsets[i + 1];
                byte[] data = new byte[EndPos - Offsets[i]];
                for (int ind = Offsets[i]; ind < EndPos; ind++)
                    data[ind - Offsets[i]] = pimg[ind + StartPos];
                Tlgs[i] = new TlgFile() { Data = data };
            }
            return Tlgs;
        }

    }
    public class TlgFile
    {
        public byte[] Data { get; internal set; }

    }
}
