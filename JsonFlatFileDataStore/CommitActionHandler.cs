using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace JsonFlatFileDataStore;

internal static class CommitActionHandler
{
    private const int CommitBatchMaxSize = 50;

    internal static void HandleStoreCommitActions(CancellationToken token, BlockingCollection<DataStore.CommitAction> updates,
        Action<bool> setExecutingState, Func<string, bool> updateState, Func<string> getLatestJson)
    {
        var batch = new Queue<DataStore.CommitAction>();
        var callbacks = new Queue<(DataStore.CommitAction action, bool success)>();

        while (!token.IsCancellationRequested)
        {
            batch.Clear();
            callbacks.Clear();

            try
            {
                var updateAction = updates.Take(token);
                setExecutingState(true);
                batch.Enqueue(updateAction);

                while (updates.Count > 0 && batch.Count < CommitBatchMaxSize)
                {
                    batch.Enqueue(updates.Take(token));
                }
            }
            catch (OperationCanceledException)
            {
                // BlockingCollection will throw OperationCanceledException when token is cancelled
                // Ignore this and exit
                break;
            }

            var jsonText = getLatestJson();

            Exception actionException = null;

            foreach (var action in batch)
            {
                try
                {
                    var (actionSuccess, updatedJson) = action.HandleAction(JObject.Parse(jsonText));

                    callbacks.Enqueue((action, actionSuccess));

                    if (actionSuccess)
                        jsonText = updatedJson;
                }
                catch (Exception e)
                {
                    // Record the failure but keep draining the batch so every caller's Ready
                    // callback fires — otherwise InnerCommit's wait loop would hang forever.
                    actionException = e;
                    callbacks.Enqueue((action, false));
                }
            }

            var updateSuccess = false;

            if (actionException == null)
            {
                try
                {
                    updateSuccess = updateState(jsonText);
                }
                catch (Exception e)
                {
                    actionException = e;
                }
            }

            foreach (var (cbAction, cbSuccess) in callbacks)
            {
                cbAction.Ready(updateSuccess ? cbSuccess : false, actionException);
            }

            setExecutingState(false);
        }
    }
}