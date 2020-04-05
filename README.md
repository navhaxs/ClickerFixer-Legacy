# ClickerFixer

#### Force redirect any presentation clicker input to the active presentation window

---

⚠ **BIG HACK. DO NOT USE FOR LIVE PRODUCTION** ⚠

ClickerFixer requires the installation of [Interception](https://github.com/oblitum/Interception). Unfortunately, the free version of this driver will cause keyboard and mouse input to stop working altogether for any subsequent input device connected once the (accumulative) input device connect count reaches past 10. This will happen if re-plugging the keyboard/mouse/clicker USB, or having too many input devices in total, etc. This count resets only after a reboot, but obviously this is **an extremely bad scenario for live production, so be warned!**

[More info here](https://github.com/oblitum/Interception/issues/25), in short not much can be done besides the paid version of the driver which is... a bit steep...

Avoid on laptops for which you constantly change docks.

Read on if this sounds managable to you, maybe for a fixed AV desk desktop PC set up which is regularly shut down and rebooted...

---

### Why?
*Someone is giving what is possibly the most riveting, super-engaging presentation you've ever seen. Way better than those Uni group presentations (*shudder*). They have the clicker in their hands. Now you, sitting in front of the computer, would like to ALT+TAB to do something on the computer (perhaps it's Audacity to start an audio recording of this amazing presentation) and now suddenly the clicker can no longer change the slides! Ooops.*

Sounds familiar?

ClickerFixer is a simple program I hacked-together to solve this problem by **["fixing"](https://en.wikipedia.org/wiki/Match_fixing) the buttons on the clicker so that it always controls the active presentation slides** rather sending arrow keys to the active window.

### Configuration

devices.txt ‐ *VID string of HID devices to match, one per line. ClickerFixer will test each input device if its VID string starts with any of these strings. ClickerFixer will only do its magic for devices which succeed this test.*

e.g.
```
HID\\VID_046D&PID_C540
HID\\VID_046D&PID_C538
HID\\VID_046D&PID_C52D
```

**If you have ProPresenter:** enable the network remote in its preferences (this is how ClickerFixer sends its commands, websockets turns out to be quite reliable for slides control)

### How it works
ClickerFixer will intercept the input from the configured input device. The input redirection is determined by the following order of logic:

1. If a **ProPresenter** output window is enabled, send the NEXT/PREVIOUS slide command there

2. Else if there is an active **PowerPoint** slide show, send the NEXT/PREVIOUS slide command there

3. Else just passthrough the button press to the active window *(default clicker behaviour)*

### Download
License: MIT

- [Windows x64](https://github.com/navhaxs/ClickerFixer/releases)

ClickerFixer requires the Interception driver to be installed, which can be downloaded from [here](https://github.com/oblitum/Interception) and the `interception.dll` file copied to ClickerFixer's working directory.

### Acknowledgements

- [Interception](https://github.com/oblitum/Interception) (Windows driver) - Francisco Lopes

- [Interceptor](https://github.com/jasonpang/Interceptor) (C# interface to Interception) - Jason Pang
