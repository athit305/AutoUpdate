using System;
using System.Runtime.InteropServices;
using System.Net;
using System.IO;

public class NetworkConnection : IDisposable
{
    private string _networkName;
    public NetworkConnection(string networkName, NetworkCredential credentials)
    {
        _networkName = networkName;

        dynamic netResource = new NetResource
        {
            Scope = ResourceScope.GlobalNetwork,
            ResourceType = ResourceType.Disk,
            DisplayType = ResourceDisplaytype.Share,
            RemoteName = networkName
        };

        dynamic result = WNetAddConnection2(netResource, credentials.Password, credentials.UserName, 0);

        if (result != 0)
        {
            throw new IOException("Error connecting to remote share", result);
        }
    }

    ~NetworkConnection()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        WNetCancelConnection2(_networkName, 0, true);
    }

    [DllImport("mpr.dll")]
    private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

    [DllImport("mpr.dll")]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);
}

[StructLayout(LayoutKind.Sequential)]
public class NetResource
{
    public ResourceScope Scope;
    public ResourceType ResourceType;
    public ResourceDisplaytype DisplayType;
    public int Usage;
    public string LocalName;
    public string RemoteName;
    public string Comment;
    public string Provider;
}

public enum ResourceScope : int
{
    Connected = 1,
    GlobalNetwork,
    Remembered,
    Recent,
    Context
}

public enum ResourceType : int
{
    Any = 0,
    Disk = 1,
    Print = 2,
    Reserved = 8
}

public enum ResourceDisplaytype : int
{
    Generic = 0x0,
    Domain = 0x1,
    Server = 0x2,
    Share = 0x3,
    File = 0x4,
    Group = 0x5,
    Network = 0x6,
    Root = 0x7,
    Shareadmin = 0x8,
    Directory = 0x9,
    Tree = 0xa,
    Ndscontainer = 0xb
}