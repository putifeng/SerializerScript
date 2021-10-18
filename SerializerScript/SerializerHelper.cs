using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SerializerScript.Internal;

namespace SerializerScript
{
    public static class SerializerHelper
    {
        public static byte[] Serilaizer<T>(T arg)
        {
            System.IO.MemoryStream writeStream = new System.IO.MemoryStream();
            WriterHelper writer = new WriterHelper(writeStream);
            CostomSerilzaerModel.Write(arg, writer);
            writer.Close();
            return writeStream.ToArray();
        }

        public static T Deserilaizer<T>(byte[] bytes)
        {

            System.IO.MemoryStream readStream = new System.IO.MemoryStream(bytes);
            ReaderHelper reader = new ReaderHelper(readStream);
            return CostomSerilzaerModel.Read<T>(reader);
        }
    }


}
