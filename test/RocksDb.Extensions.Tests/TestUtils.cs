using System.Text;
using Google.Protobuf;

namespace RocksDb.Extensions.Tests;

public static class TestUtils
{
    public static ByteString CreateByteStringOf(int bytes)
    {
        var payload = new byte[bytes];
        Array.Fill(payload, (byte)1);
        return ByteString.CopyFrom(payload);
    }

    public static string CreateStringOf(int bytes)
    {
        var payload = new byte[bytes];
        Array.Fill(payload, (byte)1);
        return Encoding.UTF8.GetString(payload);
    }
}
