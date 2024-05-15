// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

using System.Activities.Runtime;

namespace System.Activities.Statements;

[DataContract]
// This class won't be thread safe, it relies on the callers to synchronize addTimer and removeTimer
internal class TimerTable : IDisposable
{
    private SortedTimerList _sortedTimerList;

    private bool _isImmutable;
    private DurableTimerExtension _timerExtension;

    private HybridCollection<Bookmark> _pendingRemoveBookmark;
    private HybridCollection<Bookmark> _pendingRetryBookmark;

    public TimerTable(DurableTimerExtension timerExtension)
    {
        _sortedTimerList = new SortedTimerList();
        _timerExtension = timerExtension;
    }

    public int Count => _sortedTimerList.Count;

    [DataMember(Name = "sortedTimerList")]
    internal SortedTimerList SerializedSortedTimerList
    {
        get => _sortedTimerList;
        set => _sortedTimerList = value;
    }

    public void AddTimer(TimeSpan timeout, Bookmark bookmark)
    {
        // Add timer is only called on the workflow thread, 
        // It can't be racing with the persistence thread. 
        // So the table MUST be mutable when this method is called
        Fx.Assert(!_isImmutable, "Add timer is called when table is immutable");
        DateTime dueTime = TimeoutHelper.Add(DateTime.UtcNow, timeout);
        TimerData timerData = new(bookmark, dueTime)
        {
            //timerData.IOThreadTimer = new IOThreadTimer(this.timerExtension.OnTimerFiredCallback, bookmark, false, 0);
            //timerData.IOThreadTimer.Set(timeout);
            DelayTimer = new DelayTimer(_timerExtension.OnTimerFiredCallback, bookmark, timeout)
        };
        _sortedTimerList.Add(timerData);
    }

    public void RemoveTimer(Bookmark bookmark)
    {
        // When IOThread Timer calls back, it will call remove timer
        // In another thread, we may be in the middle of persistence. 
        // During persisting, we will mark the table as immutable
        // After we are done writing to the database, we will buffer the remove request
        // Meanwhile, since we are not scheduling any IOThreadTimers, 
        // we can only have at most one pending Remove request
        // We don't want to remove 
        if (!_isImmutable)
        {
            if (_sortedTimerList.TryGetValue(bookmark, out TimerData expirationTimeData))
            {
                _sortedTimerList.Remove(bookmark);
                //expirationTimeData.IOThreadTimer.Cancel();
                expirationTimeData.DelayTimer?.Cancel();
            }
        }
        else
        {
            if (_pendingRemoveBookmark == null)
            {
                _pendingRemoveBookmark = new HybridCollection<Bookmark>(bookmark);
            }
            else
            {
                _pendingRemoveBookmark.Add(bookmark);
            }
        }
    }

    // Remove the timer from the table, and set expiration date to a new value.
    public void RetryTimer(Bookmark bookmark)
    {
        // This value controls how many seconds do we retry
        const int retryDuration = 10;

        // When IOThread Timer calls back, it might call RetryTimer timer if ResumeBookmark returned notReady
        // In another thread, we may be in the middle of persistence. 
        // During persisting, we will mark the table as immutable
        // After we are done writing to the database, we will buffer the remove request
        // Meanwhile, since we are not scheduling any IOThreadTimers, 
        // we can only have at most one pending Remove request
        // We don't want to remove 
        if (!_isImmutable)
        {
            // We only retry the timer IFF no one has removed it from the table
            // Otherwise, we are just retrying a timer that doesn't exist
            if (_sortedTimerList.ContainsKey(bookmark))
            {
                RemoveTimer(bookmark);

                // Update it to the retry time and put it back to the timer list
                AddTimer(TimeSpan.FromSeconds(retryDuration), bookmark);
            }
        }
        else
        {
            if (_pendingRetryBookmark == null)
            {
                _pendingRetryBookmark = new HybridCollection<Bookmark>(bookmark);
            }
            else
            {
                _pendingRetryBookmark.Add(bookmark);
            }
        }
    }

    public DateTime GetNextDueTime()
    {
        if (_sortedTimerList.Count > 0)
        {
            return _sortedTimerList.Timers[0].ExpirationTime;
        }
        else
        {
            return DateTime.MaxValue;
        }
    }

    public void OnLoad(DurableTimerExtension timerExtension)
    {
        _timerExtension = timerExtension;
        _sortedTimerList.OnLoad();

        foreach (TimerData timerData in _sortedTimerList.Timers)
        {
            //timerData.IOThreadTimer = new IOThreadTimer(this.timerExtension.OnTimerFiredCallback, timerData.Bookmark, false, 0);
            if (timerData.ExpirationTime <= DateTime.UtcNow)
            {
                // If the timer expired, we want to fire it immediately to win the race against UnloadOnIdle policy
                timerExtension.OnTimerFiredCallback(timerData.Bookmark);
            }
            else
            {
                //timerData.IOThreadTimer.Set(timerData.ExpirationTime - DateTime.UtcNow);
                timerData.DelayTimer = new DelayTimer(_timerExtension.OnTimerFiredCallback, timerData.Bookmark, timerData.ExpirationTime - DateTime.UtcNow);
            }
        }
    }

    public void MarkAsImmutable() => _isImmutable = true;

    public void MarkAsMutable()
    {
        if (_isImmutable)
        {
            _isImmutable = false;

            int index;
            if (_pendingRemoveBookmark != null)
            {
                for (index = 0; index < _pendingRemoveBookmark.Count; index++)
                {
                    RemoveTimer(_pendingRemoveBookmark[index]);
                }
                _pendingRemoveBookmark = null;
            }

            if (_pendingRetryBookmark != null)
            {
                for (index = 0; index < _pendingRemoveBookmark.Count; index++)
                {
                    RetryTimer(_pendingRetryBookmark[index]);
                }
                _pendingRetryBookmark = null;
            }
        }
    }

    public void Dispose()
    {
        // Cancel the active timer so we stop retrying
        foreach (TimerData timerData in _sortedTimerList.Timers)
        {
            //timerData.IOThreadTimer.Cancel();
            timerData.DelayTimer?.Cancel();
        }

        // And we clear the table and other member variables that might cause the retry logic
        _sortedTimerList.Clear();
        _pendingRemoveBookmark = null;
        _pendingRetryBookmark = null;
    }

    [DataContract]
    internal class TimerData
    {
        private Bookmark _bookmark;
        private DateTime _expirationTime;

        public TimerData(Bookmark timerBookmark, DateTime expirationTime)
        {
            Bookmark = timerBookmark;
            ExpirationTime = expirationTime;
        }

        public Bookmark Bookmark
        {
            get => _bookmark;
            private set => _bookmark = value;
        }

        public DateTime ExpirationTime
        {
            get => _expirationTime;
            private set => _expirationTime = value;
        }

        //public IOThreadTimer IOThreadTimer
        //{
        //    get;
        //    set;
        //}

        public DelayTimer DelayTimer { get; set; }

        [DataMember(Name = "Bookmark")]
        internal Bookmark SerializedBookmark
        {
            get => Bookmark;
            set => Bookmark = value;
        }

        [DataMember(Name = "ExpirationTime")]
        internal DateTime SerializedExpirationTime
        {
            get => ExpirationTime;
            set => ExpirationTime = value;
        }
    }

    [DataContract]
    internal class SortedTimerList
    {
        private List<TimerData> _list;

        private Dictionary<Bookmark, TimerData> _dictionary;

        public SortedTimerList()
        {
            _list = new List<TimerData>();
            _dictionary = new Dictionary<Bookmark, TimerData>();
        }

        public List<TimerData> Timers => _list;

        public int Count => _list.Count;

        [DataMember(Name = "list")]
        internal List<TimerData> SerializedList
        {
            get => _list;
            set => _list = value;
        }

        [DataMember(Name = "dictionary")]
        internal Dictionary<Bookmark, TimerData> SerializedDictionary
        {
            get => _dictionary;
            set => _dictionary = value;
        }

        public void Add(TimerData timerData)
        {
            int index = _list.BinarySearch(timerData, TimerComparer.Instance);
            if (index < 0)
            {
                _list.Insert(~index, timerData);
                _dictionary.Add(timerData.Bookmark, timerData);
            }
        }

        public bool ContainsKey(Bookmark bookmark) => _dictionary.ContainsKey(bookmark);

        public void OnLoad()
        {
            // If upgrading from 4.0, the dictionary will be empty, so we need to create it
            if (_dictionary == null)
            {
                _dictionary = new Dictionary<Bookmark, TimerData>();
                for (int i = 0; i < _list.Count; i++)
                {
                    _dictionary.Add(_list[i].Bookmark, _list[i]);
                }
            }
        }

        public void Remove(Bookmark bookmark)
        {
            if (_dictionary.TryGetValue(bookmark, out TimerData timerData))
            {
                int index = _list.BinarySearch(timerData, TimerComparer.Instance);
                _list.RemoveAt(index);
                _dictionary.Remove(bookmark);
            }
        }

        public bool TryGetValue(Bookmark bookmark, out TimerData timerData) => _dictionary.TryGetValue(bookmark, out timerData);

        public void Clear()
        {
            _list.Clear();
            _dictionary.Clear();
        }
    }

    private class TimerComparer : IComparer<TimerData>
    {
        internal static readonly TimerComparer Instance = new();

        public int Compare(TimerData x, TimerData y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }
            if (x.ExpirationTime != y.ExpirationTime)
            {
                return x.ExpirationTime.CompareTo(y.ExpirationTime);
            }
            if (x.Bookmark.IsNamed)
            {
                return !y.Bookmark.IsNamed ? 1 : string.Compare(x.Bookmark.Name, y.Bookmark.Name, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return y.Bookmark.IsNamed ? -1 : x.Bookmark.Id.CompareTo(y.Bookmark.Id);
            }
        }
    }
}
