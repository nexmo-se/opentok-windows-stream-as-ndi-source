# Vonage Render to NDI
This project shows how to uses Subscriber.AudioData event and Video renderer to creata an NDI source from
Opentok Streams. This sample will create different streams for different Subscribers (multi NDI Streams)

Stream names will be "NDIlib Send Example: <STREAMID>"

## Prerequisites

- NDI SDK for Windows (NDI 5) - https://www.ndi.tv/sdk/
- NDI Tools. I used the **NDI Studio Monitor** to view NDI Streams - https://ndi.tv/tools/
- Vonage Video Account
- Vonage Video SDK for Windows, OpenTok.Client v2.23.1 (Important, required for AudioData Event)

### NDI Setup

- Unpack the Windows SDK
- Build the NDI .NET Lib Project - C:\Program Files\NewTek\NDI 5 SDK\Examples\C#\NDILibDotNet2

## How to Run the sample

- Goto Vonage Video Playground and get Session ID, Token and ApiKey
- In MainWindow.xaml.cs provide the credentials you got in previous step
- Compile and run the application
- Click on Connect to connect to the Session
- Now start the **NDI Studio Monitor**
- You can now select this application as the NDI source

## Notes

NDIRenderer.cs
----------------------

The NDI Render class implements IVideoRenderer to get Video streams and send send it to NDI.

MainWindow.xaml.cs
------------------

In order to use the new renderer, we pass it in as the `renderer` parameter of the
`Publisher()` constructor:

```csharp
Publisher = new Publisher(Context.Instance,
  renderer: PublisherVideo,
  capturer: Capturer);
```

