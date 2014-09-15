# Kinect 2 streaming server for .NET

This is a minimal implementation of the
[streamkinect2](https://github.com/rjw57/streamkinect2) server protocol for
.NET. It's intended to make installation of a streamkinect2 server on Windows
easier by not requiring a working Python environment.

## Building

You will need Visual Studio 2013 to build this software. The free (as in beer)
Express Edition will suffice. In addition, you must have the [Apple Bonjour SDK
for Windows](https://developer.apple.com/bonjour/index.html) and [Kinect for
Windows SDK](http://www.microsoft.com/en-us/kinectforwindows/) version 2
installed.

Additional dependencies are managed via [NuGet](https://www.nuget.org/).

## Licence

Copyright 2014, Rich Wareham. This software is licensed under a BSD 2-clause
licence. See the [LICENSE](LICENSE.txt) file included with the source
distribution.
