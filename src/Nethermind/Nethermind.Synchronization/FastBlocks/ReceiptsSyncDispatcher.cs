//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nethermind.Blockchain.Synchronization;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Logging;
using Nethermind.Synchronization.ParallelSync;
using Nethermind.Synchronization.Peers;

namespace Nethermind.Synchronization.FastBlocks
{
    public class ReceiptsSyncDispatcher : SyncDispatcher<ReceiptsSyncBatch>
    {
        public ReceiptsSyncDispatcher(
            ISyncFeed<ReceiptsSyncBatch> syncFeed,
            ISyncPeerPool syncPeerPool,
            IPeerAllocationStrategyFactory<ReceiptsSyncBatch> peerAllocationStrategy,
            ILogManager logManager)
            : base(syncFeed, syncPeerPool, peerAllocationStrategy, logManager)
        {
        }

        protected override async Task Dispatch(PeerInfo peerInfo, ReceiptsSyncBatch batch, CancellationToken cancellationToken)
        {
            ISyncPeer peer = peerInfo.SyncPeer;
            batch.ResponseSourcePeer = peerInfo;
            batch.MarkSent();

            Keccak[]? hashes = batch.Infos.Where(i => i is not null).Select(i => i!.BlockHash).ToArray();
            if (hashes.Length == 0)
            {
                if (Logger.IsDebug) Logger.Debug($"{batch} - attempted send a request with no hash.");
                return;
            }

            try
            {
                batch.Response = await peer.GetReceipts(hashes, cancellationToken);
            }
            catch (TimeoutException)
            {
                if (Logger.IsDebug) Logger.Debug($"{batch} - request receipts timeout {batch.RequestTime:F2}");
                return;
            }

            if (batch.RequestTime > 1000)
            {
                if (Logger.IsDebug) Logger.Debug($"{batch} - peer is slow {batch.RequestTime:F2}");
            }
        }
    }
}
