using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Optional;

namespace LibCyStd.IO
{
    /// <summary>
    /// Base class for blacklist kinds
    /// </summary>
    public abstract class BlacklistKind { }

    /// <summary>
    /// Use a instance of this class to specify to <see cref="Blacklist"/> that this blacklist should only exist in memory.
    /// </summary>
    public class MemoryBlacklist : BlacklistKind { }

    /// <summary>
    /// Use a instance of this class to specify to <see cref="Blacklist"/> that this blacklist should load <see cref="FileBlacklist.PathToFile"/> from file and write any additional items added to it.
    /// </summary>
    public class FileBlacklist : BlacklistKind
    {
        public string PathToFile { get; }

        public FileBlacklist(string pathToFile)
        {
            PathToFile = pathToFile;
        }
    }

    public class Blacklist : IDisposable
    {
        private readonly BlacklistKind _kind;
        private readonly Option<FileStream> _fileStream;
        private readonly Option<StreamWriter> _writer;
        private readonly HashSet<string> _set;

        private bool _disposed;
        private bool _loaded;

        /// <summary>
        /// Creates a new instance of a <see cref="Blacklist"/>. Use an instance of <see cref="MemoryBlacklist"/> to keep this <see cref="Blacklist"/> only in memory. Use an instance of <see cref="FileBlacklist"/> to be able to load from file and write the contents of the blacklist to file.
        /// </summary>
        /// <param name="kind"></param>
        /// <exception cref="IOException"/>
        /// <exception cref="System.Security.SecurityException" />
        /// <exception cref="UnauthorizedAccessException" />
        /// <exception cref="PathTooLongException" />
        public Blacklist(in BlacklistKind kind)
        {
            if (kind is FileBlacklist fileBlacklist)
            {
                var fileStream = new FileStream(fileBlacklist.PathToFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                _fileStream = Option.Some(fileStream);
                var streamWriter = new StreamWriter(fileStream) { AutoFlush = true };
                _writer = Option.Some(streamWriter);
            }
            else
            {
                _writer = Option.None<StreamWriter>();
                _fileStream = Option.None<FileStream>();
                _loaded = true;
            }

            _kind = kind;
            _set = new HashSet<string>();
        }

        private void CheckIfNotLoaded()
        {
            if (!_loaded) ExnUtils.InvalidOp("Must call Load or LoadAsync if blacklist kid is of type FileBlacklist");
        }

        private void CheckLoaded()
        {
            if (_loaded) ExnUtils.InvalidOp("Already called Load on this Blacklist");
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }

        private void Check()
        {
            CheckDisposed();
            CheckIfNotLoaded();
        }

        private void NotAFileBlacklist() => ExnUtils.InvalidOp("Blacklist kind 'MemoryBlacklist' cannot write/read to file.");

        private void Write(string item) =>
            _writer.Match(
                some: writer => writer.WriteLine(item),
                none: NotAFileBlacklist
            );

        private void Write(IEnumerable<string> items) =>
            _writer.Match(
                some: writer => { foreach (var item in items) writer.WriteLine(item); },
                none: NotAFileBlacklist
            );

        /// <summary>
        /// Loads the contents of the <see cref="FileBlacklist.PathToFile"/> property. Call this function first or <see cref="Load"/> if <see cref="BlacklistKind"/> is <see cref="FileBlacklist"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public async Task LoadAsync()
        {
            CheckDisposed();
            CheckLoaded();

            if (_kind is FileBlacklist f)
            {
                using (var fs = new FileStream(f.PathToFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = await sr.ReadLineAsync().ConfigureAwait(false);
                        _set.Add(line);
                    }
                }
            }
            else
            {
                NotAFileBlacklist();
            }

            _loaded = true;
        }

        /// <summary>
        /// Invoke this function or <see cref="LoadAsync"/> first if <see cref="BlacklistKind"/> is <see cref="FileBlacklist"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        /// <exception cref="IOException"/>
        /// <exception cref="ArgumentOutOfRangeException"/>
        public void Load()
        {
            CheckDisposed();
            CheckLoaded();

            if (_kind is FileBlacklist f)
            {
                using (var fs = new FileStream(f.PathToFile, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    while (!sr.EndOfStream)
                    {
                        var line = sr.ReadLine();
                        _set.Add(line);
                    }
                }
            }
            else
            {
                NotAFileBlacklist();
            }

            _loaded = true;
        }

        /// <summary>
        /// Adds item to <see cref="Blacklist"/> using lock.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        public void ThreadSafeAdd(in string item)
        {
            Check();
            lock (_set) if (_set.Add(item)) Write(item);
        }

        /// <summary>
        /// Adds items to <see cref="Blacklist"/> using <see cref="lock"/>
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        public void ThreadSafeAdd(in IEnumerable<string> items)
        {
            Check();
            lock (_set) Write(items.Where(_set.Add));
        }

        /// <summary>
        /// Checks if the <see cref="Blacklist"/> contains item using <see cref="lock"/>
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        public bool ThreadSafeContains(in string item)
        {
            Check();
            lock (_set) return _set.Contains(item);
        }

        /// <summary>
        /// Checks if the <see cref="Blacklist"/> contains item. Not thread safe.
        /// </summary>
        /// <param name="item"></param>
        /// <exception cref="ObjectDisposedException"/>
        /// <exception cref="InvalidOperationException"/>
        public bool Contains(in string item)
        {
            Check();
            return _set.Contains(item);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _fileStream.MatchSome(file => file.Dispose());
            _writer.MatchSome(writer => writer.Dispose());
            _set.Clear();
            _disposed = true;
        }
    }

    /// <summary>
    /// Class that runs a loop in the background, checking a collection of cached items every 10 seconds. If the collection has over > 1000 items, the items are written to the specified file and the cache is cleared.
    /// </summary>
    public class WriteWorker : IDisposable
    {
        private readonly string _pathToFile;
        private readonly CancellationTokenSource _cts;
        private readonly HashSet<string> _set;

        private bool _disposed;
        private bool _started;

        public int Count => _set.Count;

        public int ThreadSafeCount
        {
            get { lock (_set) return _set.Count; }
        }

        public WriteWorker(in string pathToFile)
        {
            _pathToFile = pathToFile;
            _cts = new CancellationTokenSource();
            _set = new HashSet<string>();
        }

        private void CheckIfNotStarted()
        {
            if (!_started) ExnUtils.InvalidOp("WriteWorker must call Start.");
        }

        private void CheckDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
        }

        private void Check()
        {
            CheckDisposed();
            CheckIfNotStarted();
        }

        private async Task WriteLoop()
        {
            try
            {
                while (true)
                {
                    if (_cts.IsCancellationRequested) return;
                    await Task.Delay(10000, _cts.Token).ConfigureAwait(false);
                    if (_set.Count > 1000) Write();
                }
            }
            catch (OperationCanceledException) { }
            finally { Write(); }
        }

        /// <summary>
        /// Writes the cached contents of the write worker.
        /// </summary>
        public void Write()
        {
            Check();

            lock (_set)
            {
                if (_set.Count == 0) return;
                using (var sw = new StreamWriter(_pathToFile, true))
                {
                    foreach (var item in _set)
                        sw.WriteLine(item);
                }

                _set.Clear();
            }
        }

        public void Start()
        {
            if (_started) ExnUtils.InvalidOp("Already started this WriteWorker.");
            _ = Task.Run(WriteLoop);
            _started = true;
        }

        /// <summary>
        /// Adds the items to the cache to be written.
        /// </summary>
        /// <param name="items"></param>
        public void Add(in IEnumerable<string> items)
        {
            Check();
            lock (_set)
                foreach (var item in items) _set.Add(item);
        }

        public void Add(in string item)
        {
            Check();
            lock (_set)
                _set.Add(item);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _cts.Cancel();
            _cts.Dispose();
            _disposed = true;
        }
    }
}