    public ID2D1Bitmap* CreateBitmap(DxPngEnum pngEnum) {
      ID2D1Bitmap* dBitmap = null;

      using ComPtr<IWICBitmapDecoder> wicDecoder = null;
      using ComPtr<IWICBitmapFrameDecode> wicFrame = null;
      using ComPtr<IWICBitmapSource> wicFrameBitmapSource = null;
      using ComPtr<IWICFormatConverter> wicConverter = null;
      using ComPtr<IWICBitmapSource> wicConverterBitmapSource = null;

      var assembly = Assembly.GetExecutingAssembly();
      var pngName = Enum.GetName(pngEnum.GetType(), pngEnum) ?? "";
      var pngResourceName = assembly.GetManifestResourceNames().FirstOrDefault(str => str.Contains(pngName));

      if (string.IsNullOrEmpty(pngResourceName)) {
        throw new Exception($"Resource {pngName} not found.");
      }

      using (var imgStream = assembly.GetManifestResourceStream(pngResourceName)) {
        if (imgStream == null) {
          throw new Exception("Image resource not found.");
        }

        GCHandle hdlStream = default;
        var dxSstream = new IStreamImpl(imgStream);

        try {
          hdlStream = GCHandle.Alloc(dxSstream, GCHandleType.Pinned);
          IntPtr ptrStream = hdlStream.AddrOfPinnedObject();

          ThrowIfFailed(FactoryImage->CreateDecoderFromStream(
              (IStream*)ptrStream,
              null,
              WICDecodeOptions.WICDecodeMetadataCacheOnLoad,
              wicDecoder.GetAddressOf()));

          ThrowIfFailed(wicDecoder.Get()->GetFrame(0, wicFrame.GetAddressOf()));
          ThrowIfFailed(wicFrame.Get()->QueryInterface(__uuidof<IWICBitmapSource>(), (void**)&wicFrameBitmapSource));
          ThrowIfFailed(FactoryImage->CreateFormatConverter(wicConverter.GetAddressOf()));

          Guid clsidWICPixelFormat = GUID.GUID_WICPixelFormat32bppPBGRA;
          wicConverter.Get()->Initialize(
              wicFrameBitmapSource,
              &clsidWICPixelFormat,
              WICBitmapDitherType.WICBitmapDitherTypeNone,
              null, 0.0,
              WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
          ThrowIfFailed(wicConverter.Get()->QueryInterface(__uuidof<IWICBitmapSource>(), (void**)&wicConverterBitmapSource));
          ThrowIfFailed(DeviceContext2D->CreateBitmapFromWicBitmap(wicConverterBitmapSource, null, &dBitmap));
        }
        finally {
          if (hdlStream.IsAllocated) hdlStream.Free();
        }
      }

      return dBitmap;
    }