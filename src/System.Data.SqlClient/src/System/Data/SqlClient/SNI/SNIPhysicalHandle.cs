// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Collections.Concurrent;

namespace System.Data.SqlClient.SNI
{
    internal abstract class SNIPhysicalHandle : SNIHandle
    {
        protected const int DefaultPoolSize = 4;

        private ConcurrentQueueSegment<SNIPacket> _pool;

        protected SNIPhysicalHandle(int poolSize = DefaultPoolSize)
        {
            _pool = new ConcurrentQueueSegment<SNIPacket>(poolSize);
        }

        public override SNIPacket RentPacket(int headerSize, int dataSize)
        {
            SNIPacket packet;
            if (!_pool.TryDequeue(out packet))
            {
                packet = new SNIPacket(this);
            }
            packet.Allocate(headerSize, dataSize);
            return packet;
        }

        public override void ReturnPacket(SNIPacket packet)
        {
            Debug.Assert(packet != null, "releasing null packet");
            Debug.Assert(ReferenceEquals(packet._owner, this), "releasing packet that belongs to another physical handle");
            packet.Release();
            _pool.TryEnqueue(packet);
        }
    }
}
