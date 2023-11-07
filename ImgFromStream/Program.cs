// See https://aka.ms/new-console-template for more information
using Claron.WIF.Dx;
using System.Reflection;
using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;
using static TerraFX.Interop.Windows.Windows;

Console.WriteLine("Hello, World!");

unsafe
{
    var FactoryImage = CreateWicImageFactory();

    // Reading from file directly is working fine
    // It reads from embedded resource (make sure the embedded resource is good), save into a .png then load from file.
    // This runs to the end. In our application it successfully returns the bitmap handle.
    // CreateBitmapFromFile(FactoryImage);

    // Reading from stream is failing...
    // It reads from embedded resource as a stream, but this always crashes at CreateDecoderFromStream()
    CreateBitmapFromStream(FactoryImage);
}

unsafe IWICImagingFactory* CreateWicImageFactory()
{
    IWICImagingFactory* wicImageFactory = null;

    Guid clsidWICImagingFactory = CLSID.CLSID_WICImagingFactory;
    ThrowIfFailed(CoCreateInstance(
      &clsidWICImagingFactory, null,
      (uint)CLSCTX.CLSCTX_INPROC_SERVER,
      __uuidof<IWICImagingFactory>(),
      (void**)&wicImageFactory));

    return wicImageFactory;
}

unsafe void CreateBitmapFromFile(IWICImagingFactory* FactoryImage)
{
    //ID2D1Bitmap* dBitmap;

    using ComPtr<IWICBitmapDecoder> wicDecoder = null;
    using ComPtr<IWICBitmapFrameDecode> wicFrame = null;
    using ComPtr<IWICBitmapSource> wicFrameBitmapSource = null;
    using ComPtr<IWICFormatConverter> wicConverter = null;
    using ComPtr<IWICBitmapSource> wicConverterBitmapSource = null;

    var pngName = "ToothSolid";
    var assembly = Assembly.GetExecutingAssembly();
    var pngResourceName = assembly.GetManifestResourceNames().FirstOrDefault(str => str.Contains(pngName));

    if (string.IsNullOrEmpty(pngResourceName))
    {
        throw new Exception($"Resource {pngName} not found.");
    }

    // Temporarily save a local copy
    var assemblyPath = assembly.Location;
    var directory = Path.GetDirectoryName(assemblyPath);
    string filePathToSave = @$"{directory}\{pngName}.png";
    if (!File.Exists(filePathToSave))
    {
        using (var imgStream = assembly.GetManifestResourceStream(pngResourceName))
        {
            if (imgStream != null)
            {
                using (var fileStream = File.Create(filePathToSave))
                {
                    imgStream.CopyTo(fileStream);
                }
            }
        }
    }

    IntPtr filePathPtr = Marshal.StringToHGlobalUni(filePathToSave);
    ThrowIfFailed(FactoryImage->CreateDecoderFromFilename(
      (ushort*)filePathPtr,
      null,
      GENERIC_READ,
      WICDecodeOptions.WICDecodeMetadataCacheOnLoad,
      wicDecoder.GetAddressOf()));

    ThrowIfFailed(wicDecoder.Get()->GetFrame(0, wicFrame.GetAddressOf()));
    ThrowIfFailed(wicFrame.Get()->QueryInterface(__uuidof<IWICBitmapSource>(), (void**)&wicFrameBitmapSource));

    FactoryImage->CreateFormatConverter(wicConverter.GetAddressOf());
    Guid clsidWICPixelFormat = GUID.GUID_WICPixelFormat32bppPBGRA;
    wicConverter.Get()->Initialize(
      wicFrameBitmapSource,
      &clsidWICPixelFormat,
      WICBitmapDitherType.WICBitmapDitherTypeNone,
      null, 0.0,
      WICBitmapPaletteType.WICBitmapPaletteTypeCustom);
    ThrowIfFailed(wicConverter.Get()->QueryInterface(__uuidof<IWICBitmapSource>(), (void**)&wicConverterBitmapSource));

    //ThrowIfFailed(DeviceContext2D->CreateBitmapFromWicBitmap(wicConverterBitmapSource, null, &dBitmap));

    if (filePathPtr != IntPtr.Zero)
    {
        Marshal.FreeHGlobal(filePathPtr);
    }

    //return dBitmap;
}

unsafe void CreateBitmapFromStream(IWICImagingFactory* FactoryImage)
{
    //ID2D1Bitmap* dBitmap = null;

    using ComPtr<IWICBitmapDecoder> wicDecoder = null;
    using ComPtr<IWICBitmapFrameDecode> wicFrame = null;
    using ComPtr<IWICBitmapSource> wicFrameBitmapSource = null;
    using ComPtr<IWICFormatConverter> wicConverter = null;
    using ComPtr<IWICBitmapSource> wicConverterBitmapSource = null;

    var pngName = "ToothSolid";
    var assembly = Assembly.GetExecutingAssembly();
    var pngResourceName = assembly.GetManifestResourceNames().FirstOrDefault(str => str.Contains(pngName));

    if (string.IsNullOrEmpty(pngResourceName))
    {
        throw new Exception($"Resource {pngName} not found.");
    }

    using (var imgStream = assembly.GetManifestResourceStream(pngResourceName))
    {
        if (imgStream == null)
        {
            throw new Exception("Image resource not found.");
        }

        var ptrStream = IStreamImpl.Create(imgStream);

        try
        {
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
            //ThrowIfFailed(DeviceContext2D->CreateBitmapFromWicBitmap(wicConverterBitmapSource, null, &dBitmap));
        }
        finally
        {
            if (ptrStream != null)
                ptrStream->Release();
        }
    }

    //return dBitmap;
}


