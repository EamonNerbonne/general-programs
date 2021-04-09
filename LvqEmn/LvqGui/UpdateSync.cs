namespace LvqGui
{
    class UpdateSync
    {
        readonly object syncUpdates = new object();
        bool busy, updateQueued;

        public bool UpdateEnqueue_IsMyTurn()
        {
            lock (syncUpdates) {
                updateQueued = busy;
                busy = true;
                return !updateQueued;
            }
        }

        public bool UpdateDone_IsQueueEmpty()
        {
            lock (syncUpdates) {
                busy = false;
                return !updateQueued;
            }
        }

        public object SyncObject => syncUpdates;
    }
}
