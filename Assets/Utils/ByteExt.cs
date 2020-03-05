using System;
using System.Collections.Generic;
using System.Net;

public static class ByteExt
{
    public static void AddNet(this List<byte> self, short aValue)
    {
        self.AddRange(hton(aValue));
    }

    public static void AddNet(this List<byte> self, int aValue)
    {
        self.AddRange(hton(aValue));
    }

    public static void AddNet(this List<byte> self, long aValue)
    {
        self.AddRange(hton(aValue));
    }

    public static byte[] hton(short aValue)
    {
        return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(aValue));
    }

    public static byte[] hton(int aValue)
    {
        return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(aValue));
    }

    public static byte[] hton(long aValue)
    {
        return BitConverter.GetBytes(IPAddress.HostToNetworkOrder(aValue));
    }
}
