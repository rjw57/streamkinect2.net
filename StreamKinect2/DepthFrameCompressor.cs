using Lz4Net;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamKinect2
{
    public delegate void CompressedDepthFrameHandler(DepthFrameCompressor compressor, byte[] data);

    public class DepthFrameCompressor : IDisposable
    {
        public static int MAX_IN_FLIGHT = 1;

        public event CompressedDepthFrameHandler CompressedDepthFrame;

        private long m_nextTaskId = 0;
        private IDictionary<long, Task> m_compressionTasks;

        public DepthFrameCompressor()
        {
            m_compressionTasks = new ConcurrentDictionary<long, Task>();
        }

        ~DepthFrameCompressor()
        {
            Dispose();
        }

        public void Dispose()
        {
            Task.WaitAll(m_compressionTasks.Values.ToArray<Task>(), 500);
        }

        public void NewDepthFrame(IDepthFrameSource source, DepthFrameHandlerArgs args)
        {
            if(m_compressionTasks.Count > MAX_IN_FLIGHT)
            {
                Debug.WriteLine("Dropping incoming depth frame.");
                return;
            }

            long thisTaskId = m_nextTaskId++;

            UInt16[] data = (UInt16[])args.FrameData.Clone();
            var task = Task.Factory.StartNew(() =>
            {
                var outputStream = new MemoryStream();
                var stream = new Lz4CompressionStream(outputStream);

                // Add highbits to stream
                foreach (var datum in data)
                {
                    stream.WriteByte((byte)(datum >> 4));
                }

                // Add lowbits
                for (var idx = 0; idx < data.Length; idx += 2)
                {
                    if (idx + 1 < data.Length)
                    {
                        stream.WriteByte((byte)(
                            ((data[idx] & 0xf) << 4) | (data[idx + 1] & 0xf)
                        ));
                    }
                    else
                    {
                        stream.WriteByte((byte)((data[idx] & 0xf) << 4));
                    }
                }

                // Send event
                var e = CompressedDepthFrame;
                if (e != null) { e(this, outputStream.GetBuffer()); }

                // Remove ourselves from the bad
                m_compressionTasks.Remove(thisTaskId);
            });

            // Add this task
            m_compressionTasks.Add(thisTaskId, task);
        }
    }
}
