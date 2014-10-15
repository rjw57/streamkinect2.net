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

                // Write data to stream in big endian (aka network) order
                foreach (var datum in data)
                {
                    stream.WriteByte((byte)(datum >> 8));
                    stream.WriteByte((byte)(datum & 0xff));
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
