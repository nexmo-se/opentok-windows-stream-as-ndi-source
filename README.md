NDI Source for Vonage Client
=====================

This prohject shows how to use Custom Audio drivers and Video renderer to creata an NDI source from
Opentok Streams

NDIRenderer.cs
----------------------

The NDI Render class implements IVideoRenderer to get Video streams and send send it to NDI.

NDIAudioDevice.cs
----------------------
This renderer sends the audio stream from the audiobus (passed as an audio bus parameter) to NDI


MainWindow.xaml.cs
------------------

In order to use the new renderer, we pass it in as the `renderer` parameter of the
`Publisher()` constructor:

```csharp
Publisher = new Publisher(Context.Instance,
  renderer: PublisherVideo,
  capturer: Capturer);
```

