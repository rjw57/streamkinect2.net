# Kinect 2 streaming server for .NET

[![Build status](https://ci.appveyor.com/api/projects/status/xp7etsw7cdss8f8j/branch/master)](https://ci.appveyor.com/project/rjw57/streamkinect2-net/branch/master)

This is a minimal implementation of the
[streamkinect2](https://github.com/rjw57/streamkinect2) server protocol for
.NET. It's intended to make installation of a streamkinect2 server on Windows
easier by not requiring a working Python environment.

## Minimum requirements

The minimum requirements for this software are those of the Kinect for Windows
SDK 2.0. In practice this means:

* Windows 8.1
* USB 3.0

## Downloading

A simple GUI server tool is automatically built by the continuous integration
system and may be found in the [list of
artifacts](https://ci.appveyor.com/project/rjw57/streamkinect2-net/build/artifacts)
for the latest build. The GUI application is available in the
``StreamKinect2GUI.zip`` file. Should the latest build be failing, you can
doanload artifacts from an earlier one.

## Building

You will need Visual Studio 2013 to build this software. The free (as in beer)
Express Edition will suffice. In addition, you must have the [Apple Bonjour SDK
for Windows](https://developer.apple.com/bonjour/index.html) and [Kinect for
Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/) version 2
installed.

Dependencies which may be re-distributed are located in the
[streamkinect2.net-depends](https://github.com/rjw57/streamkinect2.net-depends)
repository. Please read the
[README](https://github.com/rjw57/streamkinect2.net-depends/blob/master/README.md)
in that repository for licensing information.

Additional dependencies are managed via [NuGet](https://www.nuget.org/).

## Licence

Copyright 2014, Rich Wareham. This software is licensed under a BSD 2-clause
licence. See the [LICENSE](LICENSE.txt) file included with the source
distribution.
