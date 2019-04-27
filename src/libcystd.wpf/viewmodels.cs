using LibCyStd.LibOneOf;
using LibCyStd.Seq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace LibCyStd.Wpf
{
    using ConfigValue = OneOf<int, bool, string, ConfigSequence>;

    public static class ConfigValueModule
    {
        public static string AsString(ConfigValue value)
        {
            return value.Match(
                i => i.ToString(),
                b => b.ToString(),
                s => s,
                f => f.FileInfo.Name
            );
        }
    }

    public class ConfigSequence
    {
        public FileInfo FileInfo { get; }
        public IEnumerable<string> Items { get; }

        public ConfigSequence(FileInfo fileInfo, IEnumerable<string> items)
        {
            FileInfo = fileInfo;
            Items = items;
        }

        public override string ToString()
        {
            return FileInfo.FullName;
        }

        public static ConfigSequence Empty { get; } = new ConfigSequence(new FileInfo(Path.GetRandomFileName()), SeqModule.Empty<string>());
    }

    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
        }
    }

    public class WpfInt64Label : ViewModelBase
    {

        private string _displayTxt;
        private long _value;

        public string Name { get; }
        public Label Label { get; }

        public string DisplayText
        {
            get => _displayTxt;
            private set
            {
                _displayTxt = value;
                OnPropertyChanged();
            }
        }

        public long Value
        {
            get => _value;
            set
            {
                _value = value;
                DisplayText = $"{Name}: [{_value.ToString("N0")}]";
                OnPropertyChanged();
            }
        }

        public WpfInt64Label(string name, Label label)
        {
            _displayTxt = "";
            _value = 0;
            Name = name;
            Label = label;
            var binding = new Binding("DisplayText")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Source = this
            };
            Label.SetBinding(ContentControl.ContentProperty, binding);
            Value = 0;
        }
    }

    public class ConfigDataGridItem : ViewModelBase
    {
        private Brush _borderBrush;
        private string _displayTxt;
        private ConfigValue _value;

        public string Name { get; }
        public string TypeLabel { get; }
        public bool HasInvalidValue => _borderBrush == Brushes.Red;
        internal Option<DataGridCell> Cell { get; set; }

        public ConfigValue Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged(nameof(Value));
            }
        }


        public string DisplayText
        {
            get => _displayTxt;
            set
            {
                Unit Set()
                {
                    _displayTxt = value;
                    OnPropertyChanged(nameof(DisplayText));
                    return Unit.Value;
                }

                Unit UpdateStateWhenUserInputValid()
                {
                    if (_borderBrush == Brushes.Black)
                        return Set();

                    Cell.Value.Dispatcher.Invoke(
                        () => Cell.Value.BorderBrush = Brushes.Black
                    );
                    _borderBrush = Brushes.Black;

                    return Set();
                }

                Unit UpdateStateWhenUserInputInvalid()
                {
                    if (_borderBrush == Brushes.Red)
                        return Set();

                    Cell.Value.Dispatcher.Invoke(
                        () => Cell.Value.BorderBrush = Brushes.Red
                    );
                    _borderBrush = Brushes.Red;

                    return Set();
                }

                Unit SetIntValue()
                {
                    if (!int.TryParse(value, out var result))
                        return UpdateStateWhenUserInputInvalid();
                    Value = result;
                    return UpdateStateWhenUserInputValid();
                }

                Unit SetBoolValue()
                {
                    if (!bool.TryParse(value, out var result))
                        return UpdateStateWhenUserInputInvalid();
                    Value = result;
                    return UpdateStateWhenUserInputValid();
                }

                Unit SetStrValue()
                {
                    Value = value;
                    return UpdateStateWhenUserInputValid();
                }

                Unit SetValue(int index)
                {
                    return index switch
                    {
                        0 => SetIntValue(),
                        1 => SetBoolValue(),
                        2 => SetStrValue(),
                        3 => Set(),
                        _ => throw new InvalidOperationException("invalid ConfigValue option.")
                    };
                }

                _ = _value switch
                {
                    (int index, _, _, _, _) => SetValue(index)
                };
            }
        }

        public ConfigDataGridItem(
            string name,
            string typeLbl,
            ConfigValue val)
        {
            _borderBrush = Brushes.Black;
            _displayTxt = "";
            _value = val;
            Name = name;
            TypeLabel = typeLbl;
            Cell = Option.None;
            DisplayText = val.Match(
                i => i.ToString(),
                b => b.ToString(),
                s => s,
                f => f.FileInfo.Exists ? f.FileInfo.FullName : ""
            );
        }
    }

    public class ConfigDataGrid
    {
        private readonly IniCfg _iniCfg;
        private readonly Subject<(string name, ConfigValue value)> _whenValueChanged;
        private readonly ObservableCollection<ConfigDataGridItem> _items;
        private readonly DataGrid _dataGrid;

        private Unit UpdateStateOnValueChanged(ConfigDataGridItem item)
        {
            _whenValueChanged.OnNext((item.Name, item.Value));
            var value = item.Value switch
            {
                (3, _, _, _, ConfigSequence f) => f.FileInfo.FullName,
                _ => item.DisplayText
            };
            _iniCfg.AddOrUpdate(item.Name, value);

            return Unit.Value;
        }

        private static ReadOnlyCollection<DataGridTextColumn> CreateCols()
        {
            var nameBinding = new Binding("Name")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            var style = new Style();
            var setter1 = new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
            var setter2 = new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left);
            style.Setters.Add(setter1);
            style.Setters.Add(setter2);
            var nameCol = new DataGridTextColumn
            {
                Header = "Name",
                Width = DataGridLength.SizeToCells,
                Binding = nameBinding,
                CellStyle = style,
                MinWidth = 50.0,
                IsReadOnly = true
            };

            var typeBinding = new Binding("TypeLabel")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            var typeCol = new DataGridTextColumn
            {
                Header = "Type",
                Width = DataGridLength.SizeToCells,
                Binding = typeBinding,
                CellStyle = style,
                MinWidth = 50.0,
                IsReadOnly = true
            };

            var valueBinding = new Binding("DisplayText")
            {
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            var valueCol = new DataGridTextColumn
            {
                Header = "Value",
                Width = new DataGridLength(0.8, DataGridLengthUnitType.Star),
                Binding = valueBinding,
                CellStyle = style
            };

            var cols = ReadOnlyCollectionModule.OfSeq(new[] {
                nameCol,
                typeCol,
                valueCol
            });
            return cols;
        }

        private DataGrid CreateConfigDataGrid()
        {
            var dataGrid = new DataGrid
            {
                Name = "ConfigDataGrid",
                AutoGenerateColumns = false,
                ItemsSource = _items
            };
            foreach (var col in CreateCols())
                dataGrid.Columns.Add(col);
            return dataGrid;
        }

        private Unit ClearLoadedFile(ConfigDataGridItem item)
        {
            item.DisplayText = "";
            item.Value = ConfigSequence.Empty;
            UpdateStateOnValueChanged(item);
            return Unit.Value;
        }

        private void SetDataGridReadOnly(bool value)
        {
            _dataGrid.Dispatcher.Invoke(
                () => _dataGrid.IsReadOnly = value
            );
        }

        private Unit LoadFile(FileInfo fileInfo, ConfigDataGridItem item)
        {
            SetDataGridReadOnly(value: true);

            var seq = SeqModule.OfFile(fileInfo.FullName);
            var cnt = seq.Count();

            _dataGrid.Dispatcher.Invoke(() =>
            {
                var cfgFile = new ConfigSequence(fileInfo, seq);
                var cntStr = cnt.ToString("N0");

                item.DisplayText = $"[{cntStr}] ~ {fileInfo.FullName}";
                item.Value = cfgFile;

                UpdateStateOnValueChanged(item);
            });

            SetDataGridReadOnly(value: false);
            return Unit.Value;
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var idx = _dataGrid.SelectedIndex;
            if (idx <= -1)
                return;
            var item = _items[idx];
            if (!item.Value.IsT3) // not a file, return
                return;

            _ = e.ChangedButton switch
            {
                MouseButton.Right => ClearLoadedFile(item),
                _ => Unit.Value
            };
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            var idx = _dataGrid.SelectedIndex;
            if (idx <= -1)
                return;
            var item = _items[idx];
            if (item.HasInvalidValue)
                return;
            var valOpt = item.Value switch
            {
                (0, int i, _, _, _) => Option.Some(i.ToString()),
                (1, _, bool b, _, _) => Option.Some(b.ToString()),
                (2, _, _, string s, _ ) => Option.Some(s),
                _ => Option.None
            };
            if (valOpt.IsNone)
                return;

            var newValue = valOpt.Value;

            _ = _iniCfg.TryGetValue<string>(item.Name) switch
            {
                (true, var prevValue) when prevValue != newValue => UpdateStateOnValueChanged(item),
                _ => UpdateStateOnValueChanged(item)
            };
            
        }

        private Unit LoadFileInThreadPool(FileInfo fileInfo, ConfigDataGridItem item)
        {
            _ = Task.Run(() => LoadFile(fileInfo, item));
            return Unit.Value;
        }


        private void DataGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            Option<FileInfo> SelectFile(ConfigDataGridItem item)
            {
                // cancel editing of value for config file
                e.Cancel = true;
                return WpfModule.SelectFile(item.Name);
            }

            var idx = _dataGrid.SelectedIndex;
            if (idx <= -1 || idx >= _items.Count)
                return;

            var item = _items[idx];

            if (item.Cell.IsNone)
            {
                var cellInfo = _dataGrid.CurrentCell;
                item.Cell = (DataGridCell)cellInfo.Column.GetCellContent(cellInfo.Item).Parent;
            }

            Unit EditConfigFile()
            {
                var result = SelectFile(item);
                if (result.IsNone)
                    return Unit.Value;

                return LoadFileInThreadPool(result.Value, item);
            }

            _ = item.Value switch // if discriminated union value is ConfigFile
            {
                (3, _, _, _, ConfigSequence _) => EditConfigFile(),
                _ => Unit.Value
            };
        }

        public ConfigDataGrid(
            IEnumerable<ConfigDataGridItem> items,
            Grid grid,
            IniCfg cfg)
        {
            _items = new ObservableCollection<ConfigDataGridItem>(items);
            _iniCfg = cfg;
            _whenValueChanged = new Subject<(string name, ConfigValue value)>();
            _dataGrid = CreateConfigDataGrid();
            _dataGrid.MouseDoubleClick += DataGrid_MouseDoubleClick;
            _dataGrid.BeginningEdit += DataGrid_BeginningEdit;
            _dataGrid.CellEditEnding += DataGrid_CellEditEnding;
            grid.Children.Add(_dataGrid);

            Unit HandlePrimitive(ConfigDataGridItem item, string displayTxt)
            {
                item.DisplayText = displayTxt;
                UpdateStateOnValueChanged(item);
                return Unit.Value;
            }

            Unit HandleFileSeq(ConfigDataGridItem item, FileInfo f)
            {
                if (f.Exists)
                    LoadFileInThreadPool(f, item);
                return Unit.Value;
            }

            foreach (var item in _items)
            {
                var valCol = _dataGrid.Columns[2];
                var result = _iniCfg.TryGetValue<string>(item.Name);
                _ = result switch
                {
                    (true, var value) => item.Value switch
                    {
                        (0, _, _, _, _) => HandlePrimitive(item, value),
                        (1, _, _, _, _) => HandlePrimitive(item, value),
                        (2, _, _, _, _) => HandlePrimitive(item, value),
                        (3, _, _, _, _) => HandleFileSeq(item, new FileInfo(value)),
                        _ => throw new InvalidOperationException("invalid ConfigValue option.")
                    },
                    _ => Unit.Value
                };
            }
        }

    }
}
