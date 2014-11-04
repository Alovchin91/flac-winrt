# FLAC for Windows Runtime

**flac-winrt** is a simple Windows Runtime component for decoding (so far) [Free Lossless Audio Codec](http://xiph.org/flac/) (FLAC) files. It is written completely in C++/CX, and is based on an original C _libFLAC_ library and _libFLAC++_ version 1.3.0 source code. It is available for C# and C++/CX developers writing Windows Store apps for Windows (Phone) 8.1 and higher.

Currently, FLAC for Windows Runtime exposes only a decoder as this is what most developers would need in a Windows Store app. But as far as Windows Runtime matures and becomes a technology that is used in desktop development as well, FLAC for Windows Runtime will also update and add necessary functionality.

## Sample usage

To decode a FLAC file stream, use `FLAC.WindowsRuntime.Decoder.StreamDecoder` class. Instantiate the class and then call `StreamDecoder.Init` to initialize the decoder. Use `Init()` overload to provide your own FLAC decoder callbacks implementation, or use `Init(IRandomAccessStream)` overload to rely on internal FLAC for Windows Runtime implementation.

FLAC decoder callbacks are implemented as `StreamDecoder` class events. To get samples that the decoder returns, you will need to subscribe to `StreamDecoder.WriteCallback` event. The write callback contains an object of type `FLAC.WindowsRuntime.Decoder.Callbacks.StreamDecoderWriteBuffer`. This class wraps the multidimensional array of 32-bit integers [provided](http://xiph.org/flac/api/group__flac__stream__decoder.html#ga13) by the stream decoder, and since Windows Runtime doesn't support exposing of multidimensional arrays you can get access to contents of the array using `StreamDecoderWriteBuffer.GetData` method that accepts first dimension index as a parameter. For convenience, `StreamDecoderWriteBuffer.GetBuffer` method is provided that packs the array to a `Windows.Storage.Streams.IBuffer`. This `IBuffer` is accepted by many Windows Runtime APIs. The `StreamDecoder.WriteCallback` event handler must return one of the `FLAC.WindowsRuntime.Decoder.Callbacks.StreamDecoderWriteStatus` enum values.

**Coming soon**: Example solution containing sample Windows Phone 8.1 background audio project that uses FLAC for Windows Runtime.

Here's an example of how `FLAC.WindowsRuntime.Decoder.StreamDecoder` should be used:

```cs
class FlacDecoder
{
    private readonly FLAC.WindowsRuntime.Decoder.StreamDecoder _streamDecoder;
    private Windows.Storage.Streams.IBuffer _currentSample;

    public FlacDecoder()
    {
        _streamDecoder = new FLAC.WindowsRuntime.Decoder.StreamDecoder();
        _streamDecoder.WriteCallback += WriteCallback;
    }
    
    public void Initialize(Windows.Storage.Streams.IRandomAccessStream fileStream)
    {
        if (!_streamDecoder.IsValid)
            throw new InvalidOperationException("Decoder is not valid.");
        
        FLAC.WindowsRuntime.Decoder.StreamDecoderInitStatus decoderInitStatus = _streamDecoder.Init(fileStream);
        if (decoderInitStatus != StreamDecoderInitStatus.OK)
        {
            Finish();
            throw new InvalidOperationException("Failed to initialize decoder.");
        }
    }
    
    public IBuffer GetSample()
    {
        // Note: FLAC API is synchronous
        return _streamDecoder.ProcessSingle() ? _currentSample : (_currentSample = null);
    }
    
    private FLAC.WindowsRuntime.Decoder.Callbacks.StreamDecoderWriteStatus WriteCallback(
        FLAC.WindowsRuntime.Format.Frame frame, FLAC.WindowsRuntime.Decoder.Callbacks.StreamDecoderWriteBuffer buffer)
    {
        _currentSample = buffer.GetBuffer();
        return StreamDecoderWriteStatus.Continue;
    }
    
    public void Finish()
    {
        _streamDecoder.Finish();
        _streamDecoder.WriteCallback -= WriteCallback;
        _streamDecoder.Dispose();
    }
}
```

## How to build

**flac-winrt** includes all the necessary source code to build the libraries. FLAC for Windows Runtime solution includes original libFLAC and its dependencies, including [libogg](http://downloads.xiph.org/releases/ogg/), and contains libFLAC_winrt project that is the main output of the solution.

You will need Visual Studio 2013 or higher to build the library for Windows (Phone) 8.1 or higher. All the projects currently are set up to build for Windows Phone 8.1, but it's easy to retarget them for Windows 8.1 or higher.

## How to contribute

Please refer to [Contribution guidelines](./CONTRIBUTING.md). Thank you :)
