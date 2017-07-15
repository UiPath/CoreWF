// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace CoreWf.Statements
{
    [DataContract]
    /// <summary>
    /// StateMachineEventManager is used to manage triggered events globally.
    /// </summary>
    //[SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses",
    //Justification = "This type is actually used in LINQ expression and FxCop didn't detect that.")]
    internal class StateMachineEventManager
    {
        // To avoid out of memory, set a fixed length of event queue.
        private const int MaxQueueLength = 1 << 25;

        // queue is used to store triggered events
        private Queue<TriggerCompletedEvent> _queue;

        // If a state is running, its condition evaluation bookmark will be added in to activityBookmarks.
        // If a state is completed, its bookmark will be removed.
        private Collection<Bookmark> _activeBookmarks;

        /// <summary>
        /// Constructor to do initialization.
        /// </summary>
        public StateMachineEventManager()
        {
            _queue = new Queue<TriggerCompletedEvent>();
            _activeBookmarks = new Collection<Bookmark>();
        }

        /// <summary>
        /// Gets or sets the trigger index of current being processed event.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public TriggerCompletedEvent CurrentBeingProcessedEvent
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the CurrentConditionIndex denotes the index of condition is being evaluated.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public int CurrentConditionIndex
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether StateMachine is on the way of transition.
        /// </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool OnTransition
        {
            get;
            set;
        }

        /// <summary>
        /// Gets the EventManager queue.
        /// </summary>
        public IEnumerable<TriggerCompletedEvent> Queue
        {
            get
            {
                return _queue;
            }
        }

        /// <summary>
        /// Gets a value indicating whether StateMachineManger is ready to process an event immediately.
        /// </summary>
        private bool CanProcessEventImmediately
        {
            get
            {
                return this.CurrentBeingProcessedEvent == null && !this.OnTransition && _queue.Count == 0;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "queue")]
        internal Queue<TriggerCompletedEvent> SerializedQueue
        {
            get { return _queue; }
            set { _queue = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "activeBookmarks")]
        internal Collection<Bookmark> SerializedActiveBookmarks
        {
            get { return _activeBookmarks; }
            set { _activeBookmarks = value; }
        }

        /// <summary>
        /// When StateMachine enters a state, condition evaluation bookmark of that state would be added to activeBookmarks collection.
        /// </summary>
        /// <param name="bookmark">Bookmark reference.</param>
        public void AddActiveBookmark(Bookmark bookmark)
        {
            _activeBookmarks.Add(bookmark);
        }

        /// <summary>
        /// Gets next completed events queue.
        /// </summary>
        /// <returns>Top TriggerCompletedEvent item in the queue.</returns>
        public TriggerCompletedEvent GetNextCompletedEvent()
        {
            while (_queue.Any())
            {
                TriggerCompletedEvent completedEvent = _queue.Dequeue();
                if (_activeBookmarks.Contains(completedEvent.Bookmark))
                {
                    this.CurrentBeingProcessedEvent = completedEvent;
                    return completedEvent;
                }
            }

            return null;
        }

        /// <summary>
        /// This method is used to denote whether a given bookmark is referred by currently processed event.
        /// </summary>
        /// <param name="bookmark">Bookmark reference.</param>
        /// <returns>True is the bookmark references to the event being processed.</returns>
        public bool IsReferredByBeingProcessedEvent(Bookmark bookmark)
        {
            return this.CurrentBeingProcessedEvent != null && this.CurrentBeingProcessedEvent.Bookmark == bookmark;
        }

        /// <summary>
        /// Register a completed event and returns whether the event could be processed immediately.
        /// </summary>
        /// <param name="completedEvent">TriggerCompletedEvent reference.</param>
        /// <param name="canBeProcessedImmediately">True if the Condition can be evaluated.</param>
        public void RegisterCompletedEvent(TriggerCompletedEvent completedEvent, out bool canBeProcessedImmediately)
        {
            canBeProcessedImmediately = this.CanProcessEventImmediately;
            _queue.Enqueue(completedEvent);
            return;
        }

        /// <summary>
        /// When StateMachine leaves a state, condition evaluation bookmark of that state would be removed from activeBookmarks collection.
        /// </summary>
        /// <param name="bookmark">Bookmark reference.</param>
        public void RemoveActiveBookmark(Bookmark bookmark)
        {
            _activeBookmarks.Remove(bookmark);
        }
    }
}
