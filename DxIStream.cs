using System;
using System.IO;
using TerraFX.Interop;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.E;
using static TerraFX.Interop.Windows.S;

namespace test {

  public unsafe struct IStreamImpl : IStream.Interface {
    
    private Stream _stream;
    
    public IStreamImpl(Stream stream) {
      _stream = stream;
    }

    #region Core APIs (the bare minimum we need to wrap System.IO.Stream as IStream)

    public HRESULT Read(void* pv, uint cb, uint* pcbRead) {
      if (pv == null) {
        return E_INVALIDARG;
      }
      
      int bytesRead = _stream.Read(new Span<byte>(pv, (int)cb));
      if (pcbRead != null) {
        *pcbRead = (uint)bytesRead;
      }

      return S_OK;
    }

    public HRESULT Seek(LARGE_INTEGER dlibMove, uint dwOrigin, ULARGE_INTEGER* plibNewPosition) {
      long newPosition;

      switch ((STREAM_SEEK)dwOrigin) {
        case STREAM_SEEK.STREAM_SEEK_SET:
          newPosition = dlibMove.QuadPart;
          break;

        case STREAM_SEEK.STREAM_SEEK_CUR:
          newPosition = _stream.Position + dlibMove.QuadPart;
          break;

        case STREAM_SEEK.STREAM_SEEK_END:
          newPosition = _stream.Length + dlibMove.QuadPart;
          break;

        default:
          return E_INVALIDARG;
      }

      if (newPosition < 0 || newPosition > _stream.Length) {
        return E_INVALIDARG; 
      }

      try {
        _stream.Seek(newPosition, SeekOrigin.Begin);

        if (plibNewPosition != null) {
          plibNewPosition->QuadPart = (ulong)newPosition;
        }

        return S_OK;
      }
      catch (Exception) {
        return E_FAIL; 
      }
    }

    public HRESULT Write(void* pv, uint cb, uint* pcbWritten) => E_NOTIMPL;

    #endregion

    #region Helper APIs

    public HRESULT QueryInterface(Guid* riid, void** ppvObject) => E_NOTIMPL;

    public uint AddRef() => 0; 

    public uint Release() => 0; 

    public HRESULT SetSize(ULARGE_INTEGER libNewSize) => E_NOTIMPL;

    public HRESULT CopyTo(IStream* pstm, ULARGE_INTEGER cb, ULARGE_INTEGER* pcbRead, ULARGE_INTEGER* pcbWritten) => E_NOTIMPL;

    public HRESULT Commit(uint grfCommitFlags) => E_NOTIMPL;

    public HRESULT Revert() => E_NOTIMPL;

    public HRESULT LockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, uint dwLockType) => E_NOTIMPL;

    public HRESULT UnlockRegion(ULARGE_INTEGER libOffset, ULARGE_INTEGER cb, uint dwLockType) => E_NOTIMPL;

    public HRESULT Stat(STATSTG* pstatstg, uint grfStatFlag) => E_NOTIMPL;

    public HRESULT Clone(IStream** ppstm) => E_NOTIMPL;

    #endregion

    static Guid* INativeGuid.NativeGuid => throw new NotImplementedException();

  }
}