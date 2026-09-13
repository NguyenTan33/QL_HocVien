using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using QL_HocVien.Services.Interfaces;
using QL_HocVien.ViewModels;

namespace QL_HocVien.Views.Components
{
    public partial class UnitTreeComboBox : UserControl
    {
        private IUnitHierarchyService? _hierarchyService;
        private List<UnitTreeNode> _treeNodes = new();
        private bool _isLoaded;
        private bool _isInternalSelection;

        public static readonly DependencyProperty SelectedUnitProperty =
            DependencyProperty.Register(
                nameof(SelectedUnit),
                typeof(string),
                typeof(UnitTreeComboBox),
                new FrameworkPropertyMetadata(
                    string.Empty,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedUnitChanged));

        public static readonly DependencyProperty IsFilterModeProperty =
            DependencyProperty.Register(
                nameof(IsFilterMode),
                typeof(bool),
                typeof(UnitTreeComboBox),
                new PropertyMetadata(false, OnIsFilterModeChanged));

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(
                nameof(Placeholder),
                typeof(string),
                typeof(UnitTreeComboBox),
                new PropertyMetadata("Chọn đơn vị...", OnPlaceholderChanged));

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(
                nameof(IsDropDownOpen),
                typeof(bool),
                typeof(UnitTreeComboBox),
                new PropertyMetadata(false, OnIsDropDownOpenChanged));

        public string SelectedUnit
        {
            get => (string)GetValue(SelectedUnitProperty);
            set => SetValue(SelectedUnitProperty, value);
        }

        public bool IsFilterMode
        {
            get => (bool)GetValue(IsFilterModeProperty);
            set => SetValue(IsFilterModeProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        public UnitTreeComboBox()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded) return;
            _isLoaded = true;

            try
            {
                if (App.ServiceProvider != null)
                {
                    _hierarchyService = App.ServiceProvider.GetService<IUnitHierarchyService>();
                    if (_hierarchyService != null)
                    {
                        _hierarchyService.OnHierarchyChanged += OnHierarchyServiceChanged;
                    }
                }
            }
            catch { }

            AllFilterOption.Visibility = IsFilterMode ? Visibility.Visible : Visibility.Collapsed;
            HookParentWindow();
            _ = LoadTreeAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnhookParentWindow();
            if (_hierarchyService != null)
            {
                _hierarchyService.OnHierarchyChanged -= OnHierarchyServiceChanged;
            }
        }

        private void OnHierarchyServiceChanged()
        {
            Dispatcher.InvokeAsync(async () =>
            {
                await LoadTreeAsync();
            });
        }

        private async Task LoadTreeAsync()
        {
            try
            {
                if (_hierarchyService != null)
                {
                    _treeNodes = await _hierarchyService.GetUnitTreeAsync(isFilterMode: false);
                }
                else
                {
                    _treeNodes = new List<UnitTreeNode>();
                }

                MainTreeView.ItemsSource = _treeNodes;
                if (EmptyNoticeBorder != null)
                {
                    EmptyNoticeBorder.Visibility = (_treeNodes == null || _treeNodes.Count == 0) && !IsFilterMode 
                        ? Visibility.Visible 
                        : Visibility.Collapsed;
                }
                UpdateDisplayFromSelectedUnit(SelectedUnit);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UnitTreeComboBox] LoadTreeAsync error: {ex.Message}");
                _treeNodes = new List<UnitTreeNode>();
                MainTreeView.ItemsSource = _treeNodes;
                if (EmptyNoticeBorder != null)
                {
                    EmptyNoticeBorder.Visibility = !IsFilterMode ? Visibility.Visible : Visibility.Collapsed;
                }
                UpdateDisplayFromSelectedUnit(SelectedUnit);
            }
        }

        private static void OnSelectedUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UnitTreeComboBox control)
            {
                if (control._isInternalSelection) return;
                control.UpdateDisplayFromSelectedUnit(e.NewValue as string);
            }
        }

        private static void OnIsFilterModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UnitTreeComboBox control)
            {
                bool isFilter = (bool)e.NewValue;
                control.AllFilterOption.Visibility = isFilter ? Visibility.Visible : Visibility.Collapsed;
                control.UpdateDisplayFromSelectedUnit(control.SelectedUnit);
            }
        }

        private static void OnPlaceholderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UnitTreeComboBox control)
            {
                control.UpdateDisplayFromSelectedUnit(control.SelectedUnit);
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // Popup opened/closed lifecycle handled by events
        }

        private void UpdateDisplayFromSelectedUnit(string? unit)
        {
            if (string.IsNullOrWhiteSpace(unit) || unit.Equals("Tất cả", StringComparison.OrdinalIgnoreCase))
            {
                DeselectAllNodes(_treeNodes);
                if (IsFilterMode)
                {
                    SelectedIconText.Text = "🌐";
                    SelectedDisplayText.Text = "[ Tất cả đơn vị ]";
                    BtnClear.Visibility = Visibility.Collapsed;
                }
                else
                {
                    SelectedIconText.Text = "🏛️";
                    SelectedDisplayText.Text = !string.IsNullOrWhiteSpace(Placeholder) ? Placeholder : "Chọn đơn vị...";
                    BtnClear.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // Tìm node phù hợp trong cây
            var matchedNode = FindNodeMatching(unit, _treeNodes);
            if (matchedNode != null)
            {
                DeselectAllNodes(_treeNodes);
                matchedNode.IsSelected = true;
                matchedNode.ExpandParents();

                SelectedIconText.Text = matchedNode.Icon;
                SelectedDisplayText.Text = matchedNode.DisplayText;
                BtnClear.Visibility = Visibility.Visible;
            }
            else
            {
                // Nếu chưa tìm thấy (hoặc là mã tự do b3 chưa nạp xong), hiển thị trực tiếp giá trị
                SelectedIconText.Text = "🚩";
                SelectedDisplayText.Text = unit;
                BtnClear.Visibility = Visibility.Visible;
            }
        }

        private UnitTreeNode? FindNodeMatching(string unit, IEnumerable<UnitTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.MatchesValue(unit))
                {
                    return node;
                }
                var childMatch = FindNodeMatching(unit, node.Children);
                if (childMatch != null)
                {
                    return childMatch;
                }
            }
            return null;
        }

        private void DeselectAllNodes(IEnumerable<UnitTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsSelected = false;
                if (node.Children.Count > 0)
                {
                    DeselectAllNodes(node.Children);
                }
            }
        }

        private Window? _parentWindow;

        private void HookParentWindow()
        {
            var window = Window.GetWindow(this);
            if (window == _parentWindow && _parentWindow != null) return;

            UnhookParentWindow();

            _parentWindow = window;
            if (_parentWindow != null)
            {
                _parentWindow.PreviewMouseDown += OnWindowPreviewMouseDown;
                _parentWindow.PreviewMouseWheel += OnWindowPreviewMouseWheel;
                _parentWindow.Deactivated += OnWindowDeactivated;
                _parentWindow.LocationChanged += OnWindowLocationOrSizeChanged;
                _parentWindow.SizeChanged += OnWindowLocationOrSizeChanged;
            }
        }

        private void UnhookParentWindow()
        {
            if (_parentWindow != null)
            {
                _parentWindow.PreviewMouseDown -= OnWindowPreviewMouseDown;
                _parentWindow.PreviewMouseWheel -= OnWindowPreviewMouseWheel;
                _parentWindow.Deactivated -= OnWindowDeactivated;
                _parentWindow.LocationChanged -= OnWindowLocationOrSizeChanged;
                _parentWindow.SizeChanged -= OnWindowLocationOrSizeChanged;
                _parentWindow = null;
            }
        }

        private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsDropDownOpen) return;

            // Nếu click nằm trên chính control UnitTreeComboBox (TriggerBox), bỏ qua để TriggerBox tự toggle
            if (this.IsMouseOver) return;

            // Nếu click nằm bên trong Popup, cho phép tương tác với SearchBox, TreeView, Expander
            if (TreePopup.IsMouseOver) return;

            if (e.OriginalSource is DependencyObject dep)
            {
                if (IsDescendantOf(dep, this) || IsDescendantOf(dep, TreePopup))
                {
                    return;
                }
            }

            // Click ở bất kỳ đâu khác trong cửa sổ: Đóng dropdown
            IsDropDownOpen = false;
        }

        private void OnWindowPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsDropDownOpen) return;

            // Nếu cuộn chuột bên trong Popup, cho phép cuộn bình thường
            if (TreePopup.IsMouseOver) return;

            if (e.OriginalSource is DependencyObject dep && IsDescendantOf(dep, TreePopup))
            {
                return;
            }

            // Cuộn bên ngoài: Đóng dropdown để không bị trôi lơ lửng
            IsDropDownOpen = false;
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }
        }

        private void OnWindowLocationOrSizeChanged(object? sender, EventArgs e)
        {
            if (IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }
        }

        private static bool IsDescendantOf(DependencyObject? node, DependencyObject? parent)
        {
            if (node == null || parent == null) return false;
            while (node != null)
            {
                if (node == parent) return true;
                node = System.Windows.Media.VisualTreeHelper.GetParent(node) ?? System.Windows.LogicalTreeHelper.GetParent(node);
            }
            return false;
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            Dispatcher.InvokeAsync(() =>
            {
                SearchBox.Text = string.Empty;
                if (IsDropDownOpen && TreePopup.IsOpen)
                {
                    SearchBox.Focus();
                }
            }, DispatcherPriority.Background);
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            if (IsDropDownOpen)
            {
                IsDropDownOpen = false;
            }
        }

        private void OnTriggerBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsClickInsideButton(e.OriginalSource)) return; // Nút Clear được xử lý riêng

            // Đánh dấu Handled để sự kiện chuột không truyền lên ScrollViewer hoặc container ngoài
            e.Handled = true;
            HookParentWindow();
            IsDropDownOpen = !IsDropDownOpen;
        }

        private void OnTriggerBoxPreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (IsClickInsideButton(e.OriginalSource)) return;
            e.Handled = true;
        }

        private static bool IsClickInsideButton(object? originalSource)
        {
            if (originalSource is DependencyObject dep)
            {
                while (dep != null)
                {
                    if (dep is System.Windows.Controls.Primitives.ButtonBase) return true;
                    dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
                }
            }
            return false;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape && IsDropDownOpen)
            {
                IsDropDownOpen = false;
                e.Handled = true;
            }
        }

        private void OnClearClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _isInternalSelection = true;
            try
            {
                SelectedUnit = IsFilterMode ? "Tất cả" : string.Empty;
            }
            finally
            {
                _isInternalSelection = false;
            }
            UpdateDisplayFromSelectedUnit(SelectedUnit);
        }

        private void OnAllUnitsClicked(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            _isInternalSelection = true;
            try
            {
                SelectedUnit = "Tất cả";
            }
            finally
            {
                _isInternalSelection = false;
            }
            UpdateDisplayFromSelectedUnit("Tất cả");
            IsDropDownOpen = false;
        }

        private void OnNodeRowClicked(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            var btn = sender as Button;
            var node = btn?.Tag as UnitTreeNode;
            if (node == null) return;

            DeselectAllNodes(_treeNodes);
            node.IsSelected = true;

            _isInternalSelection = true;
            try
            {
                SelectedUnit = !string.IsNullOrWhiteSpace(node.Value) ? node.Value : node.Name;
            }
            finally
            {
                _isInternalSelection = false;
            }

            UpdateDisplayFromSelectedUnit(SelectedUnit);
            IsDropDownOpen = false;
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                ResetSearchFilter(_treeNodes);
            }
            else
            {
                FilterNodesRecursive(_treeNodes, keyword);
            }
        }

        private bool FilterNodesRecursive(IEnumerable<UnitTreeNode> nodes, string keyword)
        {
            bool anyChildMatched = false;
            foreach (var node in nodes)
            {
                bool selfMatch = (node.Name ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                 (node.Code ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                 (node.DisplayText ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                                 (node.Description ?? "").Contains(keyword, StringComparison.OrdinalIgnoreCase);

                bool childrenMatched = FilterNodesRecursive(node.Children, keyword);

                node.IsVisible = selfMatch || childrenMatched;
                if (childrenMatched || selfMatch)
                {
                    node.IsExpanded = true;
                    anyChildMatched = true;
                }
            }
            return anyChildMatched;
        }

        private void ResetSearchFilter(IEnumerable<UnitTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.IsVisible = true;
                if (node.Children.Count > 0)
                {
                    ResetSearchFilter(node.Children);
                }
            }
        }

        private void OnClearSearchClicked(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
        }
    }
}
