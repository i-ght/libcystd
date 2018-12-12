﻿using LibCyStd.Seq;
using Optional;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;

namespace LibCyStd
{
    public class IniCfg : IDisposable
    {
        private readonly IDictionary<string, string> _values;
        private readonly Subject<IReadOnlyDictionary<string, string>> _valuesUpdated;
        private readonly Subject<(string name, IConvertible value)> _valueUpdated;

        private bool _disposed;

        public FileInfo FileInfo { get; }
        public IReadOnlyDictionary<string, string> Values { get; }
        public IObservable<IReadOnlyDictionary<string, string>> ValuesUpdated { get; }
        public IObservable<(string name, IConvertible value)> ValueUpdated { get; }

        private FileInfo MakeFile(in string path)
        {
            using (_ = File.Create(path)) { }
            return new FileInfo(path);
        }

        private FileInfo LoadFileInfo(in string path)
        {
            var pathTmp = path;
            if (!pathTmp.InvariantEndsWith(".ini")) pathTmp = $"{path}.ini";
            if (!File.Exists(pathTmp)) return MakeFile(pathTmp);
            else return new FileInfo(pathTmp);
        }

        private IDictionary<string, string> Parse(in IEnumerable<string> lines)
        {
            Option<(string key, string val)> ParseLine(string line)
            {
                var sp = line.Split('=');
                if (sp.Length != 2 || StringUtils.AnyEmptyOrWhiteSpace(sp)) return Option.None<(string, string)>();
                else return Option.Some((sp[0], sp[1]));
            }
            return DictUtils.OfSeq(lines.Choose(ParseLine));
        }

        public void Save()
        {
            using (var sw = new StreamWriter(FileInfo.FullName))
            {
                foreach (var kvp in _values)
                    sw.WriteLine($"{kvp.Key}={kvp.Value}");
            }
        }

        public Option<T> TryGetValue<T>(in string key) where T : IConvertible
        {
            if (_values.ContainsKey(key))
            {
                try { return Option.Some((T)Convert.ChangeType(_values[key], typeof(T))); }
                catch (Exception e) when
                    (e is InvalidCastException
                    || e is FormatException
                    || e is OverflowException
                    || e is ArgumentNullException)
                {
                    return Option.None<T>();
                }
            }
            else
            {
                return Option.None<T>();
            }
        }

        public void AddOrUpdate<T>(in string key, in T value) where T : IConvertible
        {
            var (k, v) = (key, value);
            void OnAddedOrUpdated()
            {
                _valuesUpdated.OnNext(Values);
                _valueUpdated.OnNext((k, v));
                Save();
            }

            var str = value.ToString();
            if (_values.ContainsKey(key) && _values[key] != str)
            {
                _values[key] = str;
                OnAddedOrUpdated();
            }
            else if (!_values.ContainsKey(key))
            {
                _values.Add(key, value.ToString());
                OnAddedOrUpdated();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _valuesUpdated.Dispose();
            _valueUpdated.Dispose();
            _disposed = true;
        }

        public IniCfg(in string path)
        {
            FileInfo = LoadFileInfo(path);
            _values = Parse(File.ReadAllLines(FileInfo.FullName));
            Values = ReadOnlyDictUtils.OfDict(_values);
            _valuesUpdated = new Subject<IReadOnlyDictionary<string, string>>();
            _valueUpdated = new Subject<(string name, IConvertible value)>();
        }
    }
}