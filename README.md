# Kinect 2 streaming server for .NET

[![Build status](https://ci.appveyor.com/api/projects/status/t4m3d2ollhbjqklc/branch/master)](https://ci.appveyor.com/project/rjw57/streamkinect2-net/branch/master)

This is a minimal implementation of the
[streamkinect2](https://github.com/rjw57/streamkinect2) server protocol for
.NET. It's intended to make installation of a streamkinect2 server on Windows
easier by not requiring a working Python environment.

## Minimum requirements

The minimum requirements for this software are those of the Kinect for Windows
SDK 2.0. In practice this means:

* Windows 8.1
* USB 3.0

## Building

You will need Visual Studio 2013 to build this software. The free (as in beer)
Express Edition will suffice. In addition, you must have the [Apple Bonjour SDK
for Windows](https://developer.apple.com/bonjour/index.html) and [Kinect for
Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/) version 2
installed.

Dependencies which may be re-distributed are located in the
[dependencies](dependencies/) folder. Please read the
[README](dependencies/README.md) for licensing information.

Additional dependencies are managed via [NuGet](https://www.nuget.org/).

## Licence

Copyright 2014, Rich Wareham. This software is licensed under a BSD 2-clause
licence. See the [LICENSE](LICENSE.txt) file included with the source
distribution.
