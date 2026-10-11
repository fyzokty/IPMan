using System.Runtime.InteropServices;
using IPMan.Infrastructure.Networking;

namespace IPMan.Tests.Fakes;

internal sealed class FakeGetIfTable2NativeApi : IGetIfTable2NativeApi, IDisposable
{
    private IntPtr _table;

    public FakeGetIfTable2NativeApi(params (Guid InterfaceGuid, uint Medium)[] rows)
    {
        // Native x64 offsets are deliberately independent of the production declarations.
        int size = 8 + (1352 * rows.Length);
        _table = Marshal.AllocHGlobal(size);
        Marshal.Copy(new byte[size], 0, _table, size);
        Marshal.WriteInt32(_table, rows.Length);
        for (int index = 0; index < rows.Length; index++)
        {
            IntPtr row = IntPtr.Add(_table, 8 + (1352 * index));
            Marshal.StructureToPtr(rows[index].InterfaceGuid, IntPtr.Add(row, 12), false);
            Marshal.WriteInt32(row, 1140, unchecked((int)rows[index].Medium));
        }
    }

    public uint Result { get; set; }

    public Exception? GetException { get; set; }

    public Exception? FreeException { get; set; }

    public bool ReturnNull { get; set; }

    public int FreeCalls { get; private set; }

    public void SetCount(uint count) => Marshal.WriteInt32(_table, unchecked((int)count));

    public uint Get(out IntPtr table)
    {
        table = ReturnNull ? IntPtr.Zero : _table;
        if (GetException is not null)
        {
            throw GetException;
        }

        return Result;
    }

    public void Free(IntPtr table)
    {
        if (table != _table || table == IntPtr.Zero)
        {
            throw new ArgumentException("Unexpected table pointer.", nameof(table));
        }

        FreeCalls++;
        Dispose();
        if (FreeException is not null)
        {
            throw FreeException;
        }
    }

    public void Dispose()
    {
        if (_table != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_table);
            _table = IntPtr.Zero;
        }
    }
}
