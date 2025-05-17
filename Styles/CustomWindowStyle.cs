using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
// WindowChrome 사용을 위해 Animation 네임스페이스가 직접적으로 필요하지 않다면 제거 가능
// using System.Windows.Media.Animation; 
using System.Windows.Shell; // WindowChrome 사용을 위해 추가

// WindowType 열거형을 네임스페이스 외부 또는 동일 네임스페이스 내부로 이동하고 public으로 선언
public enum WindowType {
    MainWindow, // 모든 버튼 활성화
    GameInfoDialog,
    ScanResultDialog,
    ExecutableSelectionDialog,
}

namespace DLGameViewer.Styles {
    public static class CustomWindowStyle {

        private const double OriginalTitleBarHeight = 30.0;
        // 이 값은 이제 전체 창의 최대화 시 여백으로 사용됩니다.
        private const double WindowMaximizeMargin = 7.0; 

        #region CustomTitleBarTemplate

        public static readonly DependencyProperty UseCustomTitleBarProperty =
            DependencyProperty.RegisterAttached("UseCustomTitleBar", typeof(bool), typeof(CustomWindowStyle),
                new PropertyMetadata(false, OnUseCustomTitleBarChanged));

        public static bool GetUseCustomTitleBar(Window window) {
            return (bool)window.GetValue(UseCustomTitleBarProperty);
        }

        public static void SetUseCustomTitleBar(Window window, bool value) {
            window.SetValue(UseCustomTitleBarProperty, value);
        }

        private static void OnUseCustomTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (d is Window window && (bool)e.NewValue == true) {
                // 기존 WindowStyle.None 및 AllowsTransparency 제거
                // window.WindowStyle = WindowStyle.None;
                // window.AllowsTransparency = true;

                var chrome = new WindowChrome {
                    CaptionHeight = OriginalTitleBarHeight, // 논리적 캡션 높이는 XAML 정의대로 유지
                    ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness, // 시스템 기본 크기 조정 테두리 두께 사용
                    GlassFrameThickness = new Thickness(0), // 유리 효과 사용 안 함 또는 필요에 따라 조정
                    UseAeroCaptionButtons = false // 사용자 정의 버튼 사용
                };
                WindowChrome.SetWindowChrome(window, chrome);

                window.Loaded += Window_Loaded;
                window.StateChanged += Window_StateChanged; // 상태 변경 이벤트 구독
            }
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e) {
            if (sender is Window window) {
                WindowType windowType = GetWindowType(window);
                bool canMinimize = false;
                bool canMaximize = false;
                bool canClose = true; // 기본적으로 닫기 버튼은 활성화

                switch (windowType) {
                    case WindowType.MainWindow:
                        canMinimize = true;
                        canMaximize = true;
                        break;
                    case WindowType.GameInfoDialog:
                        canMinimize = false;
                        canMaximize = true;
                        break;
                    case WindowType.ScanResultDialog:
                        canMinimize = false;
                        canMaximize = true;
                        break;
                    case WindowType.ExecutableSelectionDialog:
                        canMinimize = false;
                        canMaximize = false;
                        break;
                }

                if (FindChildByName(window, "PART_CloseButton") is Button closeButton) {
                    if (!canClose) {
                        closeButton.Visibility = Visibility.Collapsed;
                    } else {
                        closeButton.Click += (s, args) => window.Close();
                    }
                }
                if (FindChildByName(window, "PART_MinimizeButton") is Button minimizeButton) {
                    if (!canMinimize) {
                        minimizeButton.Visibility = Visibility.Collapsed;
                    } else {
                        minimizeButton.Click += (s, args) => {
                            // WindowChrome 사용 시 시스템 애니메이션에 맡기므로 Opacity 애니메이션 제거
                            window.WindowState = WindowState.Minimized;
                        };
                    }
                }
                if (FindChildByName(window, "PART_MaximizeButton") is Button maximizeButton) {
                    if (!canMaximize) {
                        maximizeButton.Visibility = Visibility.Collapsed;
                    } else {
                        maximizeButton.Click += (s, args) => {
                            window.WindowState = window.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                        };
                    }
                }
                if (FindChildByName(window, "PART_TitleBar") is UIElement titleBar) {
                    titleBar.MouseLeftButtonDown += (s, args) => {
                        if (args.LeftButton == MouseButtonState.Pressed && window.WindowState != WindowState.Maximized) {
                            window.DragMove();
                        }
                    };
                }
                AdjustLayoutForWindowState(window);
            }
        }

        private static void Window_StateChanged(object? sender, EventArgs e) {
            if (sender is Window window) {
                AdjustLayoutForWindowState(window);
            }
        }

        private static void AdjustLayoutForWindowState(Window window) {
            // 전체 창 콘텐츠를 감싸는 Border의 마진을 조정
            if (FindChildByName(window, "RootChromeBorder") is Border rootBorder) {
                if (window.WindowState == WindowState.Maximized) {
                    rootBorder.Margin = new Thickness(WindowMaximizeMargin);
                } else {
                    rootBorder.Margin = new Thickness(0);
                }
            }

            // PART_TitleBar Grid 자체의 마진은 0으로 유지하고, RowDefinition 높이도 원래대로 유지
            if (FindChildByName(window, "PART_TitleBar") is FrameworkElement titleBarGrid) {
                // 사용자가 로컬에서 추가했을 수 있는 titleBarGrid의 직접적인 Margin을 제거
                titleBarGrid.Margin = new Thickness(0);

                if (titleBarGrid.Parent is Grid parentGrid && parentGrid.RowDefinitions.Count > 0) {
                    var titleBarRowDefinition = parentGrid.RowDefinitions[0];
                    // 제목 표시줄 RowDefinition의 높이를 원래 XAML 값(OriginalTitleBarHeight)으로 고정
                    titleBarRowDefinition.Height = new GridLength(OriginalTitleBarHeight);
                }
            }
        }

        /// <summary>
        /// 창에서 특정 이름의 자식 요소를 찾음
        /// </summary>
        private static UIElement? FindChildByName(DependencyObject parent, string childName) {
            // 현재 부모의 자식 요소 개수를 가져옴
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++) {
                var child = VisualTreeHelper.GetChild(parent, i);

                // 현재 자식의 이름 확인
                if (child is FrameworkElement element && element.Name == childName) {
                    return element;
                }

                // 재귀적으로 자식의 자식을 탐색
                var result = FindChildByName(child, childName);
                if (result != null) {
                    return result;
                }
            }

            return null;
        }

        #endregion

        #region WindowType Attached Property

        public static readonly DependencyProperty WindowTypeProperty =
            DependencyProperty.RegisterAttached("WindowType", typeof(WindowType), typeof(CustomWindowStyle),
                new PropertyMetadata(WindowType.MainWindow));

        public static WindowType GetWindowType(Window window) {
            return (WindowType)window.GetValue(WindowTypeProperty);
        }

        public static void SetWindowType(Window window, WindowType value) {
            window.SetValue(WindowTypeProperty, value);
        }
        #endregion
    }
}